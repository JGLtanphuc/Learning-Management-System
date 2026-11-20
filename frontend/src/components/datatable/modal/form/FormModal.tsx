import {
  Sheet,
  SheetContent,
  SheetFooter,
  SheetHeader,
  SheetDescription,
  SheetTitle,
} from "@/components/ui/sheet";
import { Button } from "@/components/ui/button";
import { useState, useEffect, useRef } from "react";
import type { FieldSchema } from "@/types";
import { FormFieldRenderer } from "./FormFieldRenderer";
import { validateForm, validateFieldValue } from "@/utils/validation.utils";
import { extractResourceCode } from "@/utils/resource.utils";
import { toast } from "sonner";
import { cn } from "@/lib/utils";

type SectionField = {
  name: string;
  fullWidth?: boolean;
};

type FormSection = {
  title: string;
  description?: string;
  fields: SectionField[];
};

type FormLayout = {
  description: string;
  maxWidthClass: string;
  sections: FormSection[];
};

const sideMenuFormLayouts: Record<string, FormLayout> = {
  module: {
    description: "Cấu hình phân hệ menu, đường dẫn điều hướng URL và quyền truy cập.",
    maxWidthClass: "sm:max-w-2xl",
    sections: [
      {
        title: "1. Thông tin cơ bản",
        description: "Đặt tên phân hệ và chọn nhóm quản trị hiển thị.",
        fields: [
          { name: "title" },
          { name: "moduleGroupId" },
        ],
      },
      {
        title: "2. Điều hướng & Phân quyền",
        description: "Định nghĩa đường dẫn URL và quyền truy cập phân hệ.",
        fields: [
          { name: "url" },
          { name: "requiredPermission" },
        ],
      },
      {
        title: "3. Cấu hình hiển thị",
        description: "Cấu hình biểu tượng icon, thứ tự sắp xếp và trạng thái hoạt động.",
        fields: [
          { name: "icon" },
          { name: "displayOrder" },
          { name: "isActive", fullWidth: true },
        ],
      },
    ],
  },
  "module-group": {
    description: "Tổ chức các nhóm phân hệ với tên gọi và thứ tự hiển thị trực quan.",
    maxWidthClass: "sm:max-w-2xl",
    sections: [
      {
        title: "Thông tin nhóm (Group Information)",
        description: "Tên nhóm và mô tả chi tiết.",
        fields: [
          { name: "name" },
          { name: "displayOrder" },
          { name: "description", fullWidth: true },
        ],
      },
      {
        title: "Trạng thái hiển thị (Visibility)",
        description: "Bật hoặc tắt nhóm này trên thanh điều hướng.",
        fields: [{ name: "isActive", fullWidth: true }],
      },
    ],
  },
  role: {
    description: "Cấu hình vai trò và ma trận phân quyền CRUD theo từng Module trong hệ thống.",
    maxWidthClass: "w-full sm:max-w-4xl lg:max-w-5xl",
    sections: [
      {
        title: "Thông tin Vai trò (Role Details)",
        description: "Tên định danh và mô tả chức năng của vai trò trong hệ thống.",
        fields: [
          { name: "name" },
          { name: "isActive" },
          { name: "description", fullWidth: true },
        ],
      },
      {
        title: "Ma trận Phân quyền theo Module (Module Permissions)",
        description: "Chọn các quyền Xem (Read), Thêm (Create), Sửa (Update), Xóa (Delete) cho từng Phân hệ.",
        fields: [{ name: "permissionIds", fullWidth: true }],
      },
    ],
  },
};

interface FormModalProps {
  open: boolean;
  onClose: (open: boolean) => void;
  schema: any;
  onSubmit: (data: any) => void;
  initial?: any;
  title?: string;
  isSubmitting?: boolean;
  relationOptions?: Record<string, any[]>;
  fieldErrors?: Record<string, string[]>;
}

export function FormModal({
  open,
  onClose,
  schema,
  onSubmit,
  initial,
  title = "Form",
  isSubmitting = false,
  relationOptions = {},
  fieldErrors = {},
}: FormModalProps) {
  const [form, setForm] = useState<Record<string, any>>({});
  const [localErrors, setLocalErrors] = useState<Record<string, string[]>>({});
  const scrollContainerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (open && scrollContainerRef.current) {
      scrollContainerRef.current.scrollTop = 0;
    }
  }, [open]);

  const isEdit = !!initial && initial[schema.idField] !== undefined;

  const editableFields: FieldSchema[] = schema.fields.filter(
    (f: FieldSchema) =>
      f.editable !== false &&
      f.type !== "dynamic" &&
      !f.dynamicChild &&
      (isEdit || !f.hideOnCreate)
  );
  const activeLayout = sideMenuFormLayouts[schema.entityName as string];
  const editableFieldMap = new Map(
    editableFields.map((field) => [field.name, field])
  );

  useEffect(() => {
    setLocalErrors(fieldErrors || {});
  }, [fieldErrors, open]);

  useEffect(() => {
    if (initial) {
      const next: Record<string, any> = {};

      schema.fields.forEach((field: FieldSchema) => {
        if (initial[field.name] !== undefined) {
          next[field.name] = initial[field.name];
          return;
        }

        if (field.type === "boolean") {
          next[field.name] = (field.name === "isActive" || field.name === "is_active") ? true : false;
        }
        else if (field.type === "relation" && field.relation?.multiple)
          next[field.name] = [];
        else if (field.type === "number") next[field.name] = "";
        else next[field.name] = "";
      });

      if (schema.entityName === "interview-question") {
        if (initial.optionsJson !== undefined) next.optionsJson = initial.optionsJson;
        if (initial.correctOptionId !== undefined) next.correctOptionId = initial.correctOptionId;
      }

      setForm(next);
    } else {
      const defaults: Record<string, any> = {};
      schema.fields.forEach((f: FieldSchema) => {
        if (f.name === "status") {
          if (schema.entityName === "class" || schema.entityName === "training-class") defaults[f.name] = "PLANNING";
          else if (schema.entityName === "allocation") defaults[f.name] = "PROPOSED";
          else if (schema.entityName === "mock-project") defaults[f.name] = "PLANNING";
          else if (schema.entityName === "interview-assignment") defaults[f.name] = "PUBLISHED";
          else defaults[f.name] = "ACTIVE";
        }
        else if (f.type === "boolean") {
          defaults[f.name] = (f.name === "isActive" || f.name === "is_active") ? true : false;
        }
        else if (f.type === "relation" && f.relation?.multiple)
          defaults[f.name] = [];
        else if (f.type === "number") defaults[f.name] = "";
        else defaults[f.name] = "";
      });

      if (schema.entityName === "interview-question") {
        defaults.questionType = "WRITTEN";
        defaults.optionsJson = JSON.stringify([
          { id: "A", text: "" },
          { id: "B", text: "" },
          { id: "C", text: "" },
          { id: "D", text: "" },
        ]);
        defaults.correctOptionId = "A";
      }

      setForm(defaults);
    }
  }, [initial, open, isEdit]);

  const updateField = (name: string, value: any) => {
    setForm((prev) => {
      const nextForm = { ...prev, [name]: value };
      if (name === "optionsJson" || name === "correctOptionId") {
        setLocalErrors((prevErrors) => {
          const nextErrors = { ...prevErrors };
          delete nextErrors["mcqOptions"];
          return nextErrors;
        });
      }
      const field = editableFieldMap.get(name);
      if (field) {
        const errs = validateFieldValue(field, value, nextForm);
        setLocalErrors((prevErrors) => {
          const nextErrors = { ...prevErrors };
          if (errs.length > 0) {
            nextErrors[name] = errs;
          } else {
            delete nextErrors[name];
          }
          return nextErrors;
        });
      }
      return nextForm;
    });
  };

  const getFieldErrors = (fieldName: string): string[] => {
    const local = localErrors[fieldName] || [];
    const backendKey = Object.keys(fieldErrors || {}).find(
      (k) => k.toLowerCase() === fieldName.toLowerCase()
    );
    const backend = backendKey ? fieldErrors[backendKey] || [] : [];
    return [...local, ...backend];
  };

  const hasError = (fieldName: string) => getFieldErrors(fieldName).length > 0;

  const handleSubmit = () => {
    const { isValid, errors } = validateForm(editableFields, form);
    if (!isValid) {
      setLocalErrors(errors);
      toast.error("Vui lòng kiểm tra lại các trường thông tin bị lỗi.");
      return;
    }

    const sanitized: Record<string, any> = { ...form };
    editableFields.forEach((f) => {
      const val = sanitized[f.name];
      if (f.type === "number") {
        if (val === "" || val === null || val === undefined) {
          sanitized[f.name] = null;
        } else {
          sanitized[f.name] = Number(val);
        }
      } else if (f.type === "date") {
        if (val === "" || val === null || val === undefined) {
          sanitized[f.name] = null;
        }
      } else if (f.type === "relation" && !f.relation?.multiple) {
        if (val === "__NONE__" || val === "") {
          sanitized[f.name] = null;
        }
      } else if (f.type === "text" || f.type === "textarea") {
        if (typeof val === "string" && val.trim() === "") {
          sanitized[f.name] = null;
        }
      }
    });

    if (schema.entityName === "module" && !sanitized.requiredPermission) {
      const resCode = extractResourceCode(sanitized.url, sanitized.title);
      sanitized.requiredPermission = `${resCode}_READ`;
    }

    if (schema.entityName === "interview-question") {
      if (form.questionType === "MCQ") {
        let parsed: any[] = [];
        try {
          parsed = typeof form.optionsJson === "string" ? JSON.parse(form.optionsJson) : (form.optionsJson || []);
        } catch {
          parsed = [];
        }

        if (!Array.isArray(parsed) || parsed.length < 2) {
          setLocalErrors((prev) => ({ ...prev, mcqOptions: ["Câu hỏi trắc nghiệm phải có ít nhất 2 phương án đáp án."] }));
          toast.error("Câu hỏi trắc nghiệm phải có ít nhất 2 phương án đáp án.");
          return;
        }

        if (parsed.some((o: any) => !o.text || !o.text.trim())) {
          setLocalErrors((prev) => ({ ...prev, mcqOptions: ["Vui lòng nhập đầy đủ nội dung cho tất cả các đáp án."] }));
          toast.error("Vui lòng nhập đầy đủ nội dung cho tất cả các đáp án.");
          return;
        }

        if (!form.correctOptionId || !parsed.some((o: any) => o.id === form.correctOptionId)) {
          setLocalErrors((prev) => ({ ...prev, mcqOptions: ["Vui lòng chọn 1 phương án làm đáp án đúng."] }));
          toast.error("Vui lòng chọn 1 phương án làm đáp án đúng.");
          return;
        }

        sanitized.optionsJson = typeof form.optionsJson === "string" ? form.optionsJson : JSON.stringify(form.optionsJson);
        sanitized.correctOptionId = form.correctOptionId;
        sanitized.keywords = null;
      } else {
        sanitized.optionsJson = null;
        sanitized.correctOptionId = null;
      }
    }

    onSubmit(sanitized);
  };

  const renderField = (field: FieldSchema) => {
    if (schema.entityName === "interview-question") {
      const isMcq = form.questionType === "MCQ";
      if (field.name === "mcqOptions" && !isMcq) return null;
      if (field.name === "keywords" && isMcq) return null;
    }

    return (
      <FormFieldRenderer
        key={field.name}
        field={field}
        value={form[field.name]}
        errorClass={
          hasError(field.name)
            ? "border-destructive focus-visible:ring-destructive"
            : ""
        }
        fieldErrors={getFieldErrors(field.name)}
        relationOptions={relationOptions}
        onChange={updateField}
        formValues={form}
        currentId={initial?.id}
      />
    );
  };

  const configuredFieldNames = new Set(
    activeLayout?.sections.flatMap((section) =>
      section.fields.map((field) => field.name)
    ) ?? []
  );

  const remainingFields = editableFields.filter(
    (field) => !configuredFieldNames.has(field.name)
  );

  const getEntityDisplayName = (name: string) => {
    switch (name) {
      case "role":
        return "Vai trò (Role)";
      case "module":
        return "Phân hệ Menu";
      case "module-group":
      case "module_group":
        return "Nhóm phân hệ Menu";
      case "user":
        return "Người dùng (User)";
      case "permission":
        return "Quyền hạn (Permission)";
      case "interview-question":
        return "Câu hỏi phỏng vấn";
      case "interview-topic":
        return "Chủ đề phỏng vấn";
      case "teacher":
        return "Giảng viên (Teacher)";
      case "class":
        return "Lớp học (Class)";
      default:
        return name;
    }
  };

  const resolvedTitle =
    title === "Create" || title === "Edit"
      ? `${title === "Create" ? "Tạo mới" : "Chỉnh sửa"} ${getEntityDisplayName(schema.entityName)}`
      : title;

  return (
    <Sheet open={open} onOpenChange={onClose}>
      <SheetContent
        side="right"
        onOpenAutoFocus={(e) => e.preventDefault()}
        className={cn(
          "w-full max-w-full p-0 flex flex-col h-full bg-white dark:bg-slate-900 border-l border-slate-200 dark:border-slate-800 shadow-2xl overflow-hidden",
          activeLayout?.maxWidthClass ?? (schema.entityName === "interview-question" ? "sm:max-w-2xl" : "sm:max-w-xl")
        )}
      >
        {/* Header */}
        <SheetHeader className="p-4 sm:p-5 sm:px-6 border-b border-slate-100 dark:border-slate-800 bg-slate-50/50 dark:bg-slate-900/50 shrink-0">
          <SheetTitle className="text-base sm:text-lg font-bold text-slate-900 dark:text-slate-100">
            {resolvedTitle}
          </SheetTitle>
          {activeLayout?.description ? (
            <SheetDescription className="text-xs text-slate-500 mt-1">
              {activeLayout.description}
            </SheetDescription>
          ) : null}
        </SheetHeader>

        {/* Scrollable Content Body (Guaranteed no horizontal overflow) */}
        <div ref={scrollContainerRef} className="flex-1 overflow-y-auto overflow-x-hidden p-3.5 sm:p-5 sm:p-6 space-y-4 sm:space-y-6 min-w-0 w-full">
          {activeLayout ? (
            <>
              {activeLayout.sections.map((section) => {
                const sectionFields = section.fields
                  .map((item) => ({
                    ...item,
                    schema: editableFieldMap.get(item.name),
                  }))
                  .filter((item) => !!item.schema);

                if (!sectionFields.length) {
                  return null;
                }

                // Nếu section chỉ chứa duy nhất Ma trận phân quyền: hiển thị trực tiếp để giao diện thoáng đãng
                const isOnlyMatrix = sectionFields.length === 1 && sectionFields[0].name === "permissionIds";

                if (isOnlyMatrix) {
                  return (
                    <div key={section.title} className="space-y-3 min-w-0 w-full">
                      <div className="border-b border-slate-100 dark:border-slate-800 pb-2">
                        <h3 className="text-sm font-bold text-slate-800 dark:text-slate-200 flex items-center gap-2">
                          {section.title}
                        </h3>
                        {section.description ? (
                          <p className="text-xs text-slate-400 mt-0.5">
                            {section.description}
                          </p>
                        ) : null}
                      </div>
                      <div className="min-w-0 w-full">
                        {renderField(sectionFields[0].schema as FieldSchema)}
                      </div>
                    </div>
                  );
                }

                return (
                  <section
                    key={section.title}
                    className="rounded-2xl border border-slate-200/90 dark:border-slate-800 bg-white dark:bg-slate-900/60 p-4 sm:p-5 space-y-4 min-w-0 w-full shadow-2xs"
                  >
                    <div className="border-b border-slate-100 dark:border-slate-800 pb-2.5">
                      <h3 className="text-xs sm:text-sm font-bold text-slate-800 dark:text-slate-200">
                        {section.title}
                      </h3>
                      {section.description ? (
                        <p className="text-[11px] sm:text-xs text-slate-400 dark:text-slate-500 mt-0.5">
                          {section.description}
                        </p>
                      ) : null}
                    </div>

                    <div className="grid content-start auto-rows-min gap-4 md:grid-cols-2 min-w-0">
                      {sectionFields.map((item) => (
                        <div
                          key={item.name}
                          className={item.fullWidth ? "md:col-span-2 min-w-0" : "min-w-0"}
                        >
                          {renderField(item.schema as FieldSchema)}
                        </div>
                      ))}
                    </div>
                  </section>
                );
              })}

              {remainingFields.length ? (
                <section className="rounded-2xl border border-slate-200 dark:border-slate-800 p-5 space-y-4 min-w-0">
                  <div className="border-b border-slate-100 dark:border-slate-800 pb-2">
                    <h3 className="text-sm font-bold text-slate-800 dark:text-slate-200">
                      Cài đặt bổ sung
                    </h3>
                  </div>

                  <div className="grid content-start auto-rows-min gap-4 md:grid-cols-2 min-w-0">
                    {remainingFields.map((field) => (
                      <div key={field.name} className="min-w-0">{renderField(field)}</div>
                    ))}
                  </div>
                </section>
              ) : null}
            </>
          ) : (
            <div className="space-y-4 min-w-0">
              {editableFields.map((field) => (
                <div key={field.name} className="min-w-0">{renderField(field)}</div>
              ))}
            </div>
          )}
        </div>

        {/* Footer */}
        <SheetFooter className="p-3 sm:p-4 px-4 sm:px-6 border-t border-slate-100 dark:border-slate-800 bg-slate-50/80 dark:bg-slate-900/80 shrink-0 flex flex-row items-center justify-end gap-2.5">
          <Button
            type="button"
            variant="outline"
            className="flex-1 sm:flex-initial h-9 px-4 text-xs font-medium cursor-pointer"
            onClick={() => onClose(false)}
            disabled={isSubmitting}
          >
            Hủy bỏ
          </Button>
          <Button
            type="button"
            className="flex-1 sm:flex-initial h-9 px-5 text-xs font-semibold bg-indigo-600 hover:bg-indigo-700 text-white shadow-xs cursor-pointer"
            onClick={handleSubmit}
            disabled={isSubmitting}
          >
            {isSubmitting ? "Đang lưu..." : isEdit ? "Lưu thay đổi" : "Tạo mới"}
          </Button>
        </SheetFooter>
      </SheetContent>
    </Sheet>
  );
}

import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Switch } from "@/components/ui/switch";
import { Textarea } from "@/components/ui/textarea";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { iconMap } from "@/components/datatable/iconMap";
import type { FieldSchema } from "@/types";
import { FieldError } from "./FieldError";
import { MultiRelationSelect } from "./MultiRelationSelect";
import { RolePermissionMatrix } from "./RolePermissionMatrix";
import { McqOptionsEditor } from "./McqOptionsEditor";

interface FormFieldRendererProps {
  field: FieldSchema;
  value: any;
  errorClass: string;
  fieldErrors?: string[];
  relationOptions?: Record<string, any[]>;
  onChange: (name: string, value: any) => void;
  formValues?: Record<string, any>;
  currentId?: any;
}

/**
 * Render input phù hợp cho từng loại field trong form.
 */
export function FormFieldRenderer({
  field,
  value,
  errorClass,
  fieldErrors,
  relationOptions = {},
  onChange,
  formValues,
  currentId,
}: FormFieldRendererProps) {
  const isRequired = field.validation?.required ?? field.required;
  const renderLabel = (className?: string) => (
    <div className="flex items-center justify-between gap-2">
      <Label htmlFor={field.name} className={className}>
        {field.label}
        {isRequired && <span className="text-rose-500 ml-1 font-bold">*</span>}
      </Label>
    </div>
  );

  switch (field.type) {
    case "boolean":
      return (
        <div className="flex items-center justify-between gap-4 py-1">
          {renderLabel("flex-1")}
          <Switch
            id={field.name}
            checked={!!value}
            onCheckedChange={(checked) => onChange(field.name, checked)}
          />
        </div>
      );

    case "dynamic":
      return null;

    case "mcq-options":
      return (
        <McqOptionsEditor
          optionsJson={formValues?.optionsJson}
          correctOptionId={formValues?.correctOptionId}
          onChangeOptions={(json) => onChange("optionsJson", json)}
          onChangeCorrectOption={(id) => onChange("correctOptionId", id)}
          fieldErrors={fieldErrors}
        />
      );

    case "icon":
    case "select": {
      const isIconField = field.type === "icon" || field.name === "icon";
      const options = field.options || field.filterOptions || [];
      const selectedOption = options.find(
        (opt) => opt.value?.toString() === value?.toString()
      );

      const selectedIconKey = selectedOption?.value?.toString();
      const SelectedIcon =
        isIconField && selectedIconKey && selectedIconKey in iconMap
          ? iconMap[selectedIconKey as keyof typeof iconMap]
          : null;

      return (
        <div className="grid gap-2">
          {renderLabel()}
          <Select
            value={value?.toString() || ""}
            onValueChange={(val) => onChange(field.name, val)}
          >
            <SelectTrigger className={errorClass}>
              <SelectValue
                placeholder={`Select ${field.label.toLowerCase()}...`}
              >
                {selectedOption ? (
                  <div className="flex items-center gap-2">
                    {SelectedIcon ? <SelectedIcon className="h-4 w-4" /> : null}
                    <span>{selectedOption.label}</span>
                  </div>
                ) : null}
              </SelectValue>
            </SelectTrigger>
            <SelectContent>
              {options.map((opt) => {
                const optionIconKey = opt.value?.toString();
                const OptionIcon =
                  isIconField && optionIconKey && optionIconKey in iconMap
                    ? iconMap[optionIconKey as keyof typeof iconMap]
                    : null;

                return (
                  <SelectItem key={opt.value} value={opt.value?.toString()}>
                    <div className="flex items-center gap-2">
                      {OptionIcon ? <OptionIcon className="h-4 w-4" /> : null}
                      <span>{opt.label}</span>
                    </div>
                  </SelectItem>
                );
              })}
            </SelectContent>
          </Select>
          <FieldError errors={fieldErrors} />
        </div>
      );
    }

    case "relation": {
      const rel = field.relation;
      if (!rel) return null;
      const items: any[] = relationOptions[field.name] ?? [];

      // Special case: Role permissions matrix
      if (field.name === "permissionIds") {
        return (
          <div className="grid gap-2">
            <RolePermissionMatrix
              field={field}
              rel={rel}
              items={items}
              value={value}
              onChange={(val) => onChange(field.name, val)}
              fieldErrors={fieldErrors}
            />
            <FieldError errors={fieldErrors} />
          </div>
        );
      }

      // Multiple select with checkboxes
      if (rel.multiple) {
        return (
          <MultiRelationSelect
            field={field}
            rel={rel}
            items={items}
            value={value}
            errorClass={errorClass}
            onChange={(val) => onChange(field.name, val)}
            fieldErrors={fieldErrors}
            formValues={formValues}
            currentId={currentId}
          />
        );
      }

      // Single select
      return (
        <div className="grid gap-2">
          {renderLabel()}
          <Select
            value={value?.toString() || ""}
            onValueChange={(val) => {
              if (val === "__NONE__" || val === "") {
                onChange(field.name, null);
                return;
              }
              const num = Number(val);
              onChange(field.name, !isNaN(num) && /^\d+$/.test(val) ? num : val);
            }}
          >
            <SelectTrigger className={errorClass}>
              <SelectValue
                placeholder={field.placeholder || `Chọn ${field.label.toLowerCase()}...`}
              />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="__NONE__">
                <span className="text-muted-foreground italic">
                  -- Không chọn / Để trống --
                </span>
              </SelectItem>
              {items.map((item) => {
                const val = item[rel.valueField];
                const label = item[rel.labelField];
                const secondaryLabel =
                  item.className && item.classCode && item.className !== label
                    ? ` (${item.className})`
                    : "";
                return (
                  <SelectItem
                    key={val}
                    value={val?.toString()}
                  >
                    {label}{secondaryLabel}
                  </SelectItem>
                );
              })}
            </SelectContent>
          </Select>
          <FieldError errors={fieldErrors} />
        </div>
      );
    }

    case "number":
      return (
        <div className="grid gap-2">
          {renderLabel()}
          <Input
            id={field.name}
            type="number"
            className={errorClass}
            value={value ?? ""}
            onChange={(e) =>
              onChange(
                field.name,
                e.target.value === "" ? "" : Number(e.target.value)
              )
            }
            placeholder={field.placeholder || `Nhập ${field.label.toLowerCase()}...`}
          />
          <FieldError errors={fieldErrors} />
        </div>
      );

    case "date":
      return (
        <div className="grid gap-2">
          {renderLabel()}
          <Input
            id={field.name}
            type="date"
            className={errorClass}
            value={value || ""}
            onChange={(e) => onChange(field.name, e.target.value)}
          />
          <FieldError errors={fieldErrors} />
        </div>
      );

    case "password":
      return (
        <div className="grid gap-2">
          {renderLabel()}
          <Input
            id={field.name}
            type="password"
            className={errorClass}
            value={value || ""}
            onChange={(e) => onChange(field.name, e.target.value)}
            placeholder={field.placeholder || `Nhập ${field.label.toLowerCase()}...`}
            autoComplete="new-password"
          />
          <FieldError errors={fieldErrors} />
        </div>
      );

    case "textarea":
      return (
        <div className="grid gap-2">
          {renderLabel()}
          <Textarea
            id={field.name}
            className={errorClass}
            value={value || ""}
            onChange={(e) => onChange(field.name, e.target.value)}
            placeholder={field.placeholder || `Nhập ${field.label.toLowerCase()}...`}
            rows={4}
          />
          <FieldError errors={fieldErrors} />
        </div>
      );

    case "text":
    default: {
      const isEmailInput = field.name.toLowerCase().includes("email");
      return (
        <div className="grid gap-2">
          {renderLabel()}
          <Input
            id={field.name}
            type={isEmailInput ? "email" : "text"}
            className={errorClass}
            value={value || ""}
            onChange={(e) => onChange(field.name, e.target.value)}
            placeholder={field.placeholder || `Nhập ${field.label.toLowerCase()}...`}
          />
          <FieldError errors={fieldErrors} />
        </div>
      );
    }
  }
}

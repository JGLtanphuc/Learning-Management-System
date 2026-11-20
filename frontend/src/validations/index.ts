import type { FieldSchema, FieldValidationRules } from "@/types";

export * from "./common.validation";
export * from "./auth/auth.validation";
export * from "./menu/module.validation";
export * from "./menu/module-group.validation";
export * from "./rbac/user.validation";
export * from "./rbac/role.validation";
export * from "./rbac/permission.validation";
export * from "./fams/student.validation";
export * from "./fams/teacher.validation";
export * from "./fams/class.validation";
export * from "./fams/allocation.validation";
export * from "./fams/attendance.validation";
export * from "./fams/mock-project.validation";
export * from "./demo/book.validation";
export * from "./interview/interview.validation";

export function applyValidation(
  fields: FieldSchema[],
  rules: Record<string, FieldValidationRules>,
): FieldSchema[] {
  return fields.map((field) => {
    const rule = rules[field.name];
    if (rule) {
      return {
        ...field,
        validation: rule,
        required: rule.required ?? field.required,
      };
    }
    return field;
  });
}

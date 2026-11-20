import type { FieldSchema } from "@/types";
import { EMAIL_REGEX, PHONE_REGEX, CODE_REGEX } from "@/validations";

export { EMAIL_REGEX, PHONE_REGEX, CODE_REGEX };

/**
 * Validate một giá trị trường dựa trên cấu hình FieldSchema
 */
export function validateFieldValue(
  field: FieldSchema,
  value: any,
  formValues: Record<string, any> = {}
): string[] {
  const errors: string[] = [];
  const rules = field.validation;
  const isRequired = rules?.required ?? field.required;

  const isEmpty =
    value === undefined ||
    value === null ||
    (typeof value === "string" && value.trim() === "") ||
    (Array.isArray(value) && value.length === 0);

  if (isRequired && isEmpty) {
    errors.push(rules?.requiredMessage || `${field.label} không được để trống`);
    return errors;
  }

  // Nếu không bắt buộc và đang rỗng thì hợp lệ
  if (isEmpty) {
    return errors;
  }

  if (typeof value === "string") {
    const trimmed = value.trim();

    if (rules?.minLength !== undefined && trimmed.length < rules.minLength) {
      errors.push(`${field.label} phải có tối thiểu ${rules.minLength} ký tự`);
    }

    if (rules?.maxLength !== undefined && trimmed.length > rules.maxLength) {
      errors.push(`${field.label} không được vượt quá ${rules.maxLength} ký tự`);
    }

    const isEmail =
      rules?.email === true ||
      field.name.toLowerCase() === "email" ||
      field.name.toLowerCase().endsWith("email");

    if (isEmail && !EMAIL_REGEX.test(trimmed)) {
      errors.push(`${field.label} không đúng định dạng email (ví dụ: example@domain.com)`);
    }

    if (rules?.phone && !PHONE_REGEX.test(trimmed)) {
      errors.push(`${field.label} phải có đúng 10 chữ số (bắt đầu bằng 0)`);
    }

    if (rules?.pattern) {
      const regex = typeof rules.pattern === "string" ? new RegExp(rules.pattern) : rules.pattern;
      if (!regex.test(trimmed)) {
        errors.push(rules.patternMessage || `${field.label} không đúng định dạng`);
      }
    }
  }

  if (field.type === "number" || typeof value === "number") {
    const num = Number(value);
    if (isNaN(num)) {
      errors.push(`${field.label} phải là số hợp lệ`);
    } else {
      if (rules?.min !== undefined && num < rules.min) {
        errors.push(`${field.label} phải lớn hơn hoặc bằng ${rules.min}`);
      }
      if (rules?.max !== undefined && num > rules.max) {
        errors.push(`${field.label} phải nhỏ hơn hoặc bằng ${rules.max}`);
      }
    }
  }

  if (rules?.custom) {
    const customErr = rules.custom(value, formValues);
    if (customErr) {
      errors.push(customErr);
    }
  }

  return errors;
}

/**
 * Validate toàn bộ dữ liệu form dựa trên danh sách FieldSchema
 */
export function validateForm(
  fields: FieldSchema[],
  formValues: Record<string, any>
): { isValid: boolean; errors: Record<string, string[]> } {
  const errors: Record<string, string[]> = {};
  let isValid = true;

  for (const field of fields) {
    if (field.editable === false || field.type === "dynamic" || field.dynamicChild) {
      continue;
    }

    const fieldErrs = validateFieldValue(field, formValues[field.name], formValues);
    if (fieldErrs.length > 0) {
      errors[field.name] = fieldErrs;
      isValid = false;
    }
  }

  return { isValid, errors };
}


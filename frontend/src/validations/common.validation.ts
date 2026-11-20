import type { FieldValidationRules } from "@/types";

export const EMAIL_REGEX = /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/;
export const PHONE_REGEX = /^0[0-9]{9}$/;
export const CODE_REGEX = /^[A-Za-z0-9_.-]+$/;
export const URL_PATH_REGEX = /^\/[a-zA-Z0-9_\-/]*$/;
export const HTTP_URL_REGEX = /^https?:\/\/.+/;

export const commonRules = {
  /** Bắt buộc chọn dropdown / quan hệ */
  select: (message?: string): FieldValidationRules => ({
    required: true,
    requiredMessage: message,
  }),

  /** Văn bản có độ dài min - max (mặc định bắt buộc nhập) */
  text: (min = 1, max = 255, required = true): FieldValidationRules => ({
    required,
    minLength: min,
    maxLength: max,
  }),

  /** Độ dài tối đa (không bắt buộc nhập) */
  max: (max = 255): FieldValidationRules => ({
    maxLength: max,
  }),

  /** Số trong khoảng min - max */
  number: (
    min?: number,
    max?: number,
    required = true,
  ): FieldValidationRules => ({
    required,
    min,
    max,
  }),

  /** Email chuẩn */
  email: (required = true): FieldValidationRules => ({
    required,
    email: true,
    maxLength: 100,
  }),

  /** Số điện thoại chuẩn 10 chữ số */
  phone: (required = true): FieldValidationRules => ({
    required,
    phone: true,
  }),

  /** Mã code chuẩn (chữ cái, chữ số, gạch dưới, gạch ngang) */
  code: (min = 3, max = 50, required = true): FieldValidationRules => ({
    required,
    minLength: min,
    maxLength: max,
    pattern: CODE_REGEX,
    patternMessage:
      "Chỉ được chứa chữ cái, số, gạch dưới (_) hoặc gạch ngang (-)",
  }),

  /** Đường dẫn URL tương đối (bắt đầu bằng /) */
  url: (
    message = "Đường dẫn URL phải bắt đầu bằng dấu gạch chéo '/' (VD: /exams)",
  ): FieldValidationRules => ({
    required: true,
    pattern: URL_PATH_REGEX,
    patternMessage: message,
  }),

  /** Đường dẫn URL web (http:// hoặc https://) */
  httpUrl: (
    message = "Đường dẫn phải bắt đầu bằng http:// hoặc https://",
  ): FieldValidationRules => ({
    pattern: HTTP_URL_REGEX,
    patternMessage: message,
  }),

  /** Mật khẩu */
  password: (min = 6, max = 100, required = true): FieldValidationRules => ({
    required,
    minLength: min,
    maxLength: max,
    patternMessage: `Mật khẩu phải có tối thiểu ${min} ký tự`,
  }),
};

import type { FieldValidationRules } from "@/types";
import { commonRules } from "../common.validation";

export const studentValidation: Record<string, FieldValidationRules> = {
  studentCode: commonRules.code(3, 50),
  fullName: commonRules.text(2, 100),
  email: commonRules.email(),
  phone: commonRules.phone(),
  university: commonRules.max(100),
  major: commonRules.max(100),
  gpa: commonRules.number(0, 10, false),
};

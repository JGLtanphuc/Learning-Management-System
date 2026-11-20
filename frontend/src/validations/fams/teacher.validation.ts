import type { FieldValidationRules } from "@/types";
import { commonRules } from "../common.validation";

export const teacherValidation: Record<string, FieldValidationRules> = {
  teacherCode: commonRules.text(2, 50),
  email: commonRules.email(),
  firstName: commonRules.text(1, 50),
  lastName: commonRules.max(50),
  password: commonRules.password(6, 100, false),
};

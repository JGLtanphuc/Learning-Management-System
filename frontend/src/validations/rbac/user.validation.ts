import type { FieldValidationRules } from "@/types";
import { commonRules } from "../common.validation";

export const userValidation: Record<string, FieldValidationRules> = {
  email: commonRules.email(),
  firstName: commonRules.text(1, 100),
  lastName: commonRules.max(50),
  password: commonRules.password(6, 100, false),
};

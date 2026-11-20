import type { FieldValidationRules } from "@/types";
import { commonRules } from "../common.validation";

export const authValidation: Record<string, FieldValidationRules> = {
  email: commonRules.email(),
  password: commonRules.password(6, 100),
  firstName: commonRules.text(1, 100),
  lastName: commonRules.text(1, 100),
};

import type { FieldValidationRules } from "@/types";
import { commonRules } from "../common.validation";

export const bookValidation: Record<string, FieldValidationRules> = {
  name: commonRules.text(2, 100),
  description: commonRules.max(500),
};

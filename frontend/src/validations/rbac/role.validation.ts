import type { FieldValidationRules } from "@/types";
import { commonRules } from "../common.validation";

export const roleValidation: Record<string, FieldValidationRules> = {
  name: commonRules.text(2, 50),
  description: commonRules.max(255),
};

import type { FieldValidationRules } from "@/types";
import { commonRules } from "../common.validation";

export const moduleGroupValidation: Record<string, FieldValidationRules> = {
  name: commonRules.text(2, 100),
  description: commonRules.max(255),
  displayOrder: commonRules.number(0, 9999, false),
};

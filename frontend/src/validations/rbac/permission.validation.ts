import type { FieldValidationRules } from "@/types";
import { commonRules } from "../common.validation";

export const permissionValidation: Record<string, FieldValidationRules> = {
  name: commonRules.text(2, 100),
  description: commonRules.max(255),
  resource: commonRules.select("Vui lòng chọn tài nguyên (Resource)"),
  action: commonRules.select("Vui lòng chọn hành động (Action)"),
};

import type { FieldValidationRules } from "@/types";
import { commonRules } from "../common.validation";

export const moduleValidation: Record<string, FieldValidationRules> = {
  title: commonRules.text(2, 100),
  moduleGroupId: commonRules.select("Vui lòng chọn nhóm phân hệ"),
  url: commonRules.url(),
  displayOrder: commonRules.number(0, 9999, false),
};

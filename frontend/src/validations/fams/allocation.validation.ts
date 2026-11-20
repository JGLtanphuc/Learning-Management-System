import type { FieldValidationRules } from "@/types";
import { commonRules } from "../common.validation";

export const allocationValidation: Record<string, FieldValidationRules> = {
  studentId: commonRules.select("Vui lòng chọn học viên"),
  targetUnit: commonRules.text(1, 100),
  projectName: commonRules.max(100),
  roleTitle: commonRules.text(1, 100),
  matchScore: commonRules.number(0, 100, false),
  status: commonRules.select(),
};

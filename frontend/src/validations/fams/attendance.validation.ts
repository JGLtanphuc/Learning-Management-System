import type { FieldValidationRules } from "@/types";
import { commonRules } from "../common.validation";

export const attendanceValidation: Record<string, FieldValidationRules> = {
  attendanceDate: commonRules.select("Vui lòng chọn ngày điểm danh"),
  slot: commonRules.select("Vui lòng chọn ca điểm danh (Slot)"),
  status: commonRules.select(),
  note: commonRules.max(255),
};

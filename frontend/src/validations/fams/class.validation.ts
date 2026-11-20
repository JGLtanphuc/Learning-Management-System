import type { FieldValidationRules } from "@/types";
import { commonRules } from "../common.validation";

export const classValidation: Record<string, FieldValidationRules> = {
  classCode: commonRules.code(3, 50),
  className: commonRules.text(3, 100),
  programName: commonRules.max(100),
  status: commonRules.select(),
  capacity: commonRules.number(1, 150),
  location: commonRules.max(100),
  endDate: {
    custom: (val, form) => {
      if (val && form.startDate && new Date(val) < new Date(form.startDate)) {
        return "Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu";
      }
    },
  },
};

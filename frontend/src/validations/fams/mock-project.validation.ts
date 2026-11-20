import type { FieldValidationRules } from "@/types";
import { commonRules } from "../common.validation";

export const mockProjectValidation: Record<string, FieldValidationRules> = {
  projectName: commonRules.text(3, 100),
  mentorName: commonRules.max(100),
  techStack: commonRules.max(100),
  repoUrl: commonRules.httpUrl(
    "Repository URL phải bắt đầu bằng http:// hoặc https://",
  ),
  status: commonRules.select(),
  finalScore: commonRules.number(0, 10, false),
};

import type { FieldValidationRules } from "@/types";
import { commonRules } from "../common.validation";

export const interviewTopicValidation: Record<string, FieldValidationRules> = {
  name: commonRules.text(2, 150),
  description: commonRules.max(500),
  displayOrder: commonRules.number(0, 9999, false),
};

export const interviewQuestionValidation: Record<string, FieldValidationRules> =
  {
    topicId: commonRules.select("Vui lòng chọn mục phỏng vấn"),
    questionText: commonRules.text(5, 1000),
    questionType: commonRules.select("Vui lòng chọn loại câu hỏi"),
    sampleAnswer: commonRules.max(5000),
    keywords: commonRules.max(500),
    difficulty: commonRules.select("Vui lòng chọn độ khó"),
    displayOrder: commonRules.number(0, 9999, false),
  };

export const interviewAssignmentValidation: Record<
  string,
  FieldValidationRules
> = {
  title: commonRules.text(3, 200),
  topicId: commonRules.select("Vui lòng chọn chủ đề phỏng vấn"),
  durationMinutes: commonRules.number(5, 300, true),
  totalQuestions: commonRules.number(1, 50, true),
  easyCount: commonRules.number(0, 50, true),
  mediumCount: commonRules.number(0, 50, true),
  hardCount: commonRules.number(0, 50, true),
  passingScore: commonRules.number(1, 10, true),
};

export const interviewAnswerValidation: Record<string, FieldValidationRules> = {
  teacherNote: commonRules.max(1000),
  score: commonRules.number(0, 10, false),
};

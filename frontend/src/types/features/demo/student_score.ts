import type { BaseDTO, BaseFilter } from "@/types/common/base";

export interface StudentScoreColumnDTO {
  key?: string;
  name?: string;
  label?: string;
  value?: number | string | null;
}

export interface StudentScoreDTO extends BaseDTO {
  studentCode?: string;
  studentName?: string;
  className?: string;
  scores?: Record<string, number | string | null> | StudentScoreColumnDTO[];
  scoreColumns?: StudentScoreColumnDTO[];
  overallScore?: number;
}

export interface StudentScoreFilter extends BaseFilter {
  studentCode?: string;
  studentName?: string;
  className?: string;
}

export interface ClassScoreItemDTO {
  studentId: number;
  studentCode: string;
  studentName: string;
  email?: string;
  scoreId?: number | null;
  scores: Record<string, number | null>;
  overallScore?: number | null;
}

export interface ClassScoreSheetDTO {
  classId: number;
  classCode: string;
  className: string;
  students: ClassScoreItemDTO[];
}

export interface SaveClassScoreBatchRequest {
  classId: number;
  className?: string;
  items: {
    studentCode: string;
    studentName: string;
    scores: Record<string, number | null>;
    overallScore?: number | null;
  }[];
}

import type { BaseDTO, BaseFilter } from "@/types/common/base";

export interface AllocationDTO extends BaseDTO {
  id?: number;
  studentId: number;
  studentCode?: string;
  studentName?: string;
  targetUnit: string;
  projectName?: string;
  roleTitle: string;
  matchScore?: number;
  status: string;
  interviewDate?: string;
  note?: string;
}

export interface CreateAllocationPayload {
  studentId: number;
  targetUnit: string;
  projectName?: string;
  roleTitle?: string;
  matchScore?: number;
  status?: string;
  interviewDate?: string;
  note?: string;
  isActive?: boolean;
}

export interface UpdateAllocationPayload extends CreateAllocationPayload {
  id?: number;
}

export interface AllocationFilter extends BaseFilter {
  targetUnit?: string;
  status?: string;
  roleTitle?: string;
}


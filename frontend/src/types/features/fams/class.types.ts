import type { BaseDTO, BaseFilter } from "@/types/common/base";

export interface ClassDTO extends BaseDTO {
  id?: number;
  classCode: string;
  className: string;
  programName?: string;
  startDate?: string;
  endDate?: string;
  capacity?: number;
  location?: string;
  room?: string;
  status?: string;
  totalStudents?: number;
}

export interface CreateClassPayload {
  classCode: string;
  className: string;
  programName?: string;
  startDate?: string;
  endDate?: string;
  capacity?: number;
  location?: string;
  room?: string;
  status?: string;
  isActive?: boolean;
}

export interface UpdateClassPayload extends CreateClassPayload {
  id?: number;
}

export interface ClassFilter extends BaseFilter {
  status?: string;
  programName?: string;
  fromDate?: string;
  toDate?: string;
}


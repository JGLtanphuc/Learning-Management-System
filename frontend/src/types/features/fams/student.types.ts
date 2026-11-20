import type { BaseDTO, BaseFilter } from "@/types/common/base";

export interface StudentDTO extends BaseDTO {
  id?: number;
  studentCode: string;
  fullName: string;
  email: string;
  phone?: string;
  classId?: number;
  className?: string;
  mockProjectId?: number;
  mockProjectName?: string;
  university?: string;
  major?: string;
  gpa?: number;
  status?: string;
  joinedAt?: string;
}

export interface CreateStudentPayload {
  studentCode: string;
  fullName: string;
  email: string;
  phone?: string;
  classId?: number;
  mockProjectId?: number;
  university?: string;
  major?: string;
  gpa?: number;
  status?: string;
  joinedAt?: string;
  isActive?: boolean;
}

export interface UpdateStudentPayload extends CreateStudentPayload {
  id?: number;
}

export interface StudentFilter extends BaseFilter {
  classId?: number;
  status?: string;
  university?: string;
  major?: string;
  minGpa?: number;
  maxGpa?: number;
}


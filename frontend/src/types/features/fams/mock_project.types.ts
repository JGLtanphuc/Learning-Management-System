import type { BaseDTO, BaseFilter } from "@/types/common/base";

export interface StudentSimpleDTO {
  id: number;
  studentCode: string;
  fullName: string;
  email?: string;
}

export interface MockProjectDTO extends BaseDTO {
  id?: number;
  projectName: string;
  mentorName?: string;
  techStack?: string;
  repoUrl?: string;
  status?: string;
  finalScore?: number;
  studentIds?: number[];
  members?: StudentSimpleDTO[];
}

export interface CreateMockProjectPayload {
  projectName: string;
  mentorName?: string;
  techStack?: string;
  repoUrl?: string;
  status?: string;
  finalScore?: number;
  studentIds?: number[];
  isActive?: boolean;
}

export interface UpdateMockProjectPayload extends CreateMockProjectPayload {
  id?: number;
}

export interface MockProjectFilter extends BaseFilter {
  status?: string;
  mentorName?: string;
  techStack?: string;
}


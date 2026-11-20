import { createBaseApiService } from "@/api/base-service.api";

export interface TeacherDTO {
  id: number;
  teacherCode?: string;
  email: string;
  firstName: string;
  lastName?: string;
  fullName: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
  assignedClasses: string[];
  assignedClassIds: number[];
  classIds?: number[];
  totalAssignedClasses: number;
}

export interface TeacherFilter {
  page?: number;
  size?: number;
  search?: string;
  isActive?: boolean;
}

const path = "/teachers";

const base = createBaseApiService<TeacherDTO, TeacherFilter>({
  path: path,
});

export const teacherApi = base;

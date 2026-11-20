import { createBaseApiService } from "@/api/base-service.api";
import axiosInstance from "@/api/axios";
import type { StudentDTO, StudentFilter } from "@/types";

const path = "/students";

const base = createBaseApiService<StudentDTO, StudentFilter>({
  path: path,
});

export const studentApi = Object.assign({}, base, {
  getAvailableForMockProject: async (currentProjectId?: number, classId?: number): Promise<StudentDTO[]> => {
    const params: Record<string, any> = {};
    if (currentProjectId) params.currentProjectId = currentProjectId;
    if (classId) params.classId = classId;
    const response = await axiosInstance.get<StudentDTO[]>(`${path}/available-for-mock-project`, { params });
    return response.data;
  },
});


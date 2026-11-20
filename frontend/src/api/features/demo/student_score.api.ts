import { createBaseApiService } from "@/api/base-service.api";
import axiosInstance from "@/api/axios";
import type {
  ClassScoreSheetDTO,
  SaveClassScoreBatchRequest,
  StudentScoreDTO,
  StudentScoreFilter,
} from "@/types";

const path = "/student-scores";

const base = createBaseApiService<StudentScoreDTO, StudentScoreFilter>({
  path,
});

export const studentScoreApi = Object.assign({}, base, {
  getClassSheet: async (classId: number): Promise<ClassScoreSheetDTO> => {
    const response = await axiosInstance.get<ClassScoreSheetDTO>(`${path}/class-sheet`, {
      params: { classId },
    });
    return response.data;
  },
  saveBatch: async (payload: SaveClassScoreBatchRequest): Promise<StudentScoreDTO[]> => {
    const response = await axiosInstance.post<StudentScoreDTO[]>(`${path}/batch`, payload);
    return response.data;
  },
});

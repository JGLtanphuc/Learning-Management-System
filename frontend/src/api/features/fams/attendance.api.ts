import { createBaseApiService } from "@/api/base-service.api";
import axiosInstance from "@/api/axios";
import type {
  AttendanceDTO,
  AttendanceFilter,
  ClassAttendanceSheetDTO,
  SaveClassAttendanceRequest,
} from "@/types";

const path = "/attendances";

const base = createBaseApiService<AttendanceDTO, AttendanceFilter>({
  path: path,
});

export const attendanceApi = Object.assign({}, base, {
  getClassSheet: async (classId: number, date?: string, slot?: string): Promise<ClassAttendanceSheetDTO> => {
    const params: Record<string, any> = { classId };
    if (date) params.date = date;
    if (slot) params.slot = slot;
    const response = await axiosInstance.get<ClassAttendanceSheetDTO>(`${path}/class-sheet`, { params });
    return response.data;
  },
  saveBatch: async (payload: SaveClassAttendanceRequest): Promise<AttendanceDTO[]> => {
    const response = await axiosInstance.post<AttendanceDTO[]>(`${path}/batch`, payload);
    return response.data;
  },
});



import type { BaseDTO, BaseFilter } from "@/types/common/base";

export interface AttendanceDTO extends BaseDTO {
  id?: number;
  studentId: number;
  studentCode?: string;
  studentName?: string;
  classId: number;
  className?: string;
  attendanceDate: string;
  slot: string;
  status: string;
  note?: string;
}

export interface CreateAttendancePayload {
  studentId: number;
  classId: number;
  attendanceDate: string;
  slot: string;
  status: string;
  note?: string;
  isActive?: boolean;
}

export interface UpdateAttendancePayload extends CreateAttendancePayload {
  id?: number;
}

export interface AttendanceFilter extends BaseFilter {
  classId?: number;
  studentId?: number;
  date?: string;
  attendanceDate?: string;
  slot?: string;
  status?: string;
}

export interface ClassAttendanceItemDTO {
  studentId: number;
  studentCode: string;
  studentName: string;
  email?: string;
  attendanceId?: number | null;
  status: string;
  note?: string;
}

export interface ClassAttendanceSheetDTO {
  classId: number;
  classCode: string;
  className: string;
  attendanceDate: string;
  slot: string;
  students: ClassAttendanceItemDTO[];
}

export interface SaveClassAttendanceRequest {
  classId: number;
  attendanceDate: string;
  slot: string;
  items: {
    studentId: number;
    status: string;
    note?: string;
  }[];
}


import { attendanceApi, classApi } from "@/api";
import {
  auditableFieldsSchema,
  type EntityConfig,
  type EntitySchema,
  type FieldSchema,
} from "@/types";
import { attendanceValidation } from "@/validations";
import { AttendanceCheckInPage } from "./pages/AttendanceCheckInPage";
import { AttendanceDetailPage } from "./pages/AttendanceDetailPage";
import { AttendanceEditPage } from "./pages/AttendanceEditPage";

const attendanceSchema: EntitySchema = {
  entityName: "attendance",
  idField: "id",
  fields: [
    {
      name: "studentCode",
      label: "Student Code",
      type: "text",
      sortable: true,
      filterable: true,
      filterType: "text",
      bold: true,
    },
    {
      name: "studentName",
      label: "Student Name",
      type: "text",
      sortable: true,
      filterable: true,
      filterType: "text",
    },
    {
      name: "className",
      label: "Class",
      type: "text",
      sortable: true,
      filterable: false,
    },
    {
      name: "classId",
      label: "Class",
      type: "relation",
      relation: {
        api: classApi,
        valueField: "id",
        labelField: "classCode",
      },
      visible: false,
      hideable: false,
      filterable: true,
      filterType: "select",
    },
    {
      name: "attendanceDate",
      label: "Date",
      type: "date",
      sortable: true,
      filterable: true,
      filterType: "dateRange",
      validation: attendanceValidation.attendanceDate,
    },
    {
      name: "slot",
      label: "Slot",
      type: "select",
      sortable: true,
      filterable: true,
      filterType: "select",
      validation: attendanceValidation.slot,
      filterOptions: [
        { label: "Morning", value: "MORNING" },
        { label: "Afternoon", value: "AFTERNOON" },
        { label: "All Day", value: "ALL_DAY" },
      ],
      options: [
        { label: "Morning", value: "MORNING" },
        { label: "Afternoon", value: "AFTERNOON" },
        { label: "All Day", value: "ALL_DAY" },
      ],
    },
    {
      name: "status",
      label: "Attendance Status",
      type: "select",
      sortable: true,
      filterable: true,
      filterType: "select",
      validation: attendanceValidation.status,
      filterOptions: [
        { label: "Present", value: "PRESENT" },
        { label: "Absent", value: "ABSENT" },
        { label: "Late", value: "LATE" },
        { label: "Excused", value: "EXCUSED" },
      ],
      options: [
        { label: "Present", value: "PRESENT" },
        { label: "Absent", value: "ABSENT" },
        { label: "Late", value: "LATE" },
        { label: "Excused", value: "EXCUSED" },
      ],
    },
    {
      name: "note",
      label: "Note",
      type: "text",
    },
    ...(auditableFieldsSchema as FieldSchema[]),
  ],
};

export const entityConfig: EntityConfig = {
  name: "Attendance Tracking",
  path: "/attendances",
  api: attendanceApi,
  schema: attendanceSchema,
  actionPages: {
    create: {
      path: "/attendances/create",
      component: AttendanceCheckInPage,
    },
    edit: {
      path: "/attendances/:id/edit",
      component: AttendanceEditPage,
    },
    detail: {
      path: "/attendances/:id/detail",
      component: AttendanceDetailPage,
    },
  },
};


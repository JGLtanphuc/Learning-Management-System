import { classApi, teacherApi } from "@/api";
import {
  auditableFieldsSchema,
  type EntityConfig,
  type EntitySchema,
  type FieldSchema,
} from "@/types";
import { classValidation } from "@/validations";

const classSchema: EntitySchema = {
  entityName: "class",
  idField: "id",
  fields: [
    {
      name: "classCode",
      label: "Class Code",
      type: "text",
      sortable: true,
      filterable: true,
      filterType: "text",
      bold: true,
      validation: classValidation.classCode,
    },
    {
      name: "className",
      label: "Class Name",
      type: "text",
      sortable: true,
      filterable: true,
      filterType: "text",
      validation: classValidation.className,
    },
    {
      name: "programName",
      label: "Training Program",
      type: "text",
      sortable: true,
      filterable: true,
      filterType: "text",
      validation: classValidation.programName,
    },
    {
      name: "status",
      label: "Status",
      type: "select",
      sortable: true,
      filterable: true,
      filterType: "select",
      hideOnCreate: true,
      validation: classValidation.status,
      filterOptions: [
        { label: "Planning", value: "PLANNING" },
        { label: "Open for Registration", value: "OPEN_FOR_REGISTRATION" },
        { label: "Ongoing", value: "ONGOING" },
        { label: "Completed", value: "COMPLETED" },
        { label: "Cancelled", value: "CANCELLED" },
      ],
      options: [
        { label: "Planning", value: "PLANNING" },
        { label: "Open for Registration", value: "OPEN_FOR_REGISTRATION" },
        { label: "Ongoing", value: "ONGOING" },
        { label: "Completed", value: "COMPLETED" },
        { label: "Cancelled", value: "CANCELLED" },
      ],
    },
    {
      name: "capacity",
      label: "Capacity",
      type: "number",
      sortable: true,
      validation: classValidation.capacity,
    },
    {
      name: "location",
      label: "Location",
      type: "text",
      sortable: true,
      filterable: true,
      filterType: "text",
      validation: classValidation.location,
    },
    {
      name: "room",
      label: "Room",
      type: "text",
    },
    {
      name: "teacherId",
      label: "Giảng viên Lead",
      type: "relation",
      relation: {
        api: teacherApi,
        valueField: "id",
        labelField: "fullName",
      },
      sortable: true,
      filterable: true,
      filterType: "select",
    },
    {
      name: "startDate",
      label: "Start Date",
      type: "date",
      sortable: true,
    },
    {
      name: "endDate",
      label: "End Date",
      type: "date",
      sortable: true,
      validation: classValidation.endDate,
    },
    ...(auditableFieldsSchema as FieldSchema[]),
  ],
};

export const entityConfig: EntityConfig = {
  name: "Training Class",
  path: "/classes",
  api: classApi,
  schema: classSchema,
};


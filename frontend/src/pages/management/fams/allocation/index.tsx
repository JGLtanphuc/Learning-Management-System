import { allocationApi, studentApi } from "@/api";
import {
  auditableFieldsSchema,
  type EntityConfig,
  type EntitySchema,
  type FieldSchema,
} from "@/types";
import { allocationValidation } from "@/validations";

const allocationSchema: EntitySchema = {
  entityName: "allocation",
  idField: "id",
  fields: [
    {
      name: "studentId",
      label: "Student",
      type: "relation",
      relation: {
        api: studentApi,
        valueField: "id",
        labelField: "fullName",
      },
      validation: allocationValidation.studentId,
      visible: false,
      hideable: false,
    },
    {
      name: "studentCode",
      label: "Student Code",
      type: "text",
      sortable: true,
      filterable: true,
      filterType: "text",
      bold: true,
      editable: false,
    },
    {
      name: "studentName",
      label: "Student Name",
      type: "text",
      sortable: true,
      filterable: true,
      filterType: "text",
      editable: false,
    },
    {
      name: "targetUnit",
      label: "Target Unit (BU)",
      type: "text",
      sortable: true,
      filterable: true,
      filterType: "text",
      validation: allocationValidation.targetUnit,
      bold: true,
    },
    {
      name: "projectName",
      label: "Project",
      type: "text",
      sortable: true,
      validation: allocationValidation.projectName,
    },
    {
      name: "roleTitle",
      label: "Role",
      type: "text",
      sortable: true,
      filterable: true,
      filterType: "text",
      validation: allocationValidation.roleTitle,
    },
    {
      name: "matchScore",
      label: "Match (%)",
      type: "number",
      sortable: true,
      filterable: true,
      filterType: "numberRange",
      validation: allocationValidation.matchScore,
    },
    {
      name: "status",
      label: "Status",
      type: "select",
      sortable: true,
      filterable: true,
      filterType: "select",
      hideOnCreate: true,
      validation: allocationValidation.status,
      filterOptions: [
        { label: "Proposed", value: "PROPOSED" },
        { label: "Interviewing", value: "INTERVIEWING" },
        { label: "Allocated", value: "ALLOCATED" },
        { label: "Accepted", value: "ACCEPTED" },
        { label: "Rejected", value: "REJECTED" },
        { label: "Transferred", value: "TRANSFERRED" },
      ],
      options: [
        { label: "Proposed", value: "PROPOSED" },
        { label: "Interviewing", value: "INTERVIEWING" },
        { label: "Allocated", value: "ALLOCATED" },
        { label: "Accepted", value: "ACCEPTED" },
        { label: "Rejected", value: "REJECTED" },
        { label: "Transferred", value: "TRANSFERRED" },
      ],
    },
    {
      name: "interviewDate",
      label: "Interview Date",
      type: "date",
      sortable: true,
    },
    {
      name: "note",
      label: "Notes",
      type: "text",
    },
    ...(auditableFieldsSchema as FieldSchema[]),
  ],
};

export const entityConfig: EntityConfig = {
  name: "BU Allocation",
  path: "/allocations",
  api: allocationApi,
  schema: allocationSchema,
};


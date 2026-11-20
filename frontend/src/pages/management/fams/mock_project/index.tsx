import { mockProjectApi, studentApi } from "@/api";
import {
  auditableFieldsSchema,
  type EntityConfig,
  type EntitySchema,
  type FieldSchema,
} from "@/types";
import { mockProjectValidation } from "@/validations";

const mockProjectSchema: EntitySchema = {
  entityName: "mock-project",
  idField: "id",
  fields: [
    {
      name: "projectName",
      label: "Project Name",
      type: "text",
      sortable: true,
      filterable: true,
      filterType: "text",
      bold: true,
      validation: mockProjectValidation.projectName,
    },
    {
      name: "studentIds",
      label: "Students",
      type: "relation",
      relation: {
        api: studentApi,
        valueField: "id",
        labelField: "fullName",
        multiple: true,
        max: 3,
        fetchOptions: (params?: any) =>
          studentApi.getAvailableForMockProject(params?.currentId),
      },
      sortable: false,
      filterable: false,
    },
    {
      name: "mentorName",
      label: "Mentor",
      type: "text",
      sortable: true,
      filterable: true,
      filterType: "text",
      validation: mockProjectValidation.mentorName,
    },
    {
      name: "techStack",
      label: "Tech Stack",
      type: "text",
      sortable: true,
      filterable: true,
      filterType: "text",
      validation: mockProjectValidation.techStack,
    },
    {
      name: "repoUrl",
      label: "Repository URL",
      type: "text",
      validation: mockProjectValidation.repoUrl,
    },
    {
      name: "status",
      label: "Status",
      type: "select",
      sortable: true,
      filterable: true,
      filterType: "select",
      hideOnCreate: true,
      validation: mockProjectValidation.status,
      filterOptions: [
        { label: "Planning", value: "PLANNING" },
        { label: "Active", value: "ACTIVE" },
        { label: "Completed", value: "COMPLETED" },
        { label: "Cancelled", value: "CANCELLED" },
      ],
      options: [
        { label: "Planning", value: "PLANNING" },
        { label: "Active", value: "ACTIVE" },
        { label: "Completed", value: "COMPLETED" },
        { label: "Cancelled", value: "CANCELLED" },
      ],
    },
    {
      name: "finalScore",
      label: "Score",
      type: "number",
      sortable: true,
      filterable: true,
      filterType: "numberRange",
      validation: mockProjectValidation.finalScore,
    },
    ...(auditableFieldsSchema as FieldSchema[]),
  ],
};

export const entityConfig: EntityConfig = {
  name: "Mock Project",
  path: "/mock-projects",
  api: mockProjectApi,
  schema: mockProjectSchema,
};


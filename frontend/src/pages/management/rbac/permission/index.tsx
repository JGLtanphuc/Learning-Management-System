import { permissionApi } from "@/api";
import {
  auditableFieldsSchema,
  type EntityConfig,
  type EntitySchema,
  type FieldSchema,
} from "@/types";
import { permissionValidation } from "@/validations";

const permissionSchema: EntitySchema = {
  entityName: "permission",
  idField: "id",
  fields: [
    {
      name: "name",
      label: "Name",
      type: "text",
      sortable: true,
      bold: true,
      validation: permissionValidation.name,
    },
    {
      name: "description",
      label: "Description",
      type: "textarea",
      sortable: true,
      validation: permissionValidation.description,
    },
    {
      name: "resource",
      label: "Resource",
      type: "select",
      sortable: true,
      filterable: true,
      filterType: "select",
      validation: permissionValidation.resource,
      filterOptions: [
        { label: "Allocation", value: "ALLOCATION" },
        { label: "Attendance", value: "ATTENDANCE" },
        { label: "Book", value: "BOOK" },
        { label: "Class", value: "CLASS" },
        { label: "Interview Topic", value: "INTERVIEW_TOPIC" },
        { label: "Interview Question", value: "INTERVIEW_QUESTION" },
        { label: "Menu", value: "MENU" },
        { label: "Mock Project", value: "MOCK_PROJECT" },
        { label: "Module", value: "MODULE" },
        { label: "Permission", value: "PERMISSION" },
        { label: "Role", value: "ROLE" },
        { label: "Student", value: "STUDENT" },
        { label: "Student Score", value: "STUDENT_SCORE" },
        { label: "User", value: "USER" },
      ],
      options: [
        { label: "Allocation", value: "ALLOCATION" },
        { label: "Attendance", value: "ATTENDANCE" },
        { label: "Book", value: "BOOK" },
        { label: "Class", value: "CLASS" },
        { label: "Interview Topic", value: "INTERVIEW_TOPIC" },
        { label: "Interview Question", value: "INTERVIEW_QUESTION" },
        { label: "Menu", value: "MENU" },
        { label: "Mock Project", value: "MOCK_PROJECT" },
        { label: "Module", value: "MODULE" },
        { label: "Permission", value: "PERMISSION" },
        { label: "Role", value: "ROLE" },
        { label: "Student", value: "STUDENT" },
        { label: "Student Score", value: "STUDENT_SCORE" },
        { label: "User", value: "USER" },
      ],
    },
    {
      name: "action",
      label: "Action",
      type: "select",
      sortable: true,
      filterable: true,
      filterType: "select",
      validation: permissionValidation.action,
      filterOptions: [
        { label: "CREATE", value: "CREATE" },
        { label: "READ", value: "READ" },
        { label: "UPDATE", value: "UPDATE" },
        { label: "DELETE", value: "DELETE" },
      ],
      options: [
        { label: "CREATE", value: "CREATE" },
        { label: "READ", value: "READ" },
        { label: "UPDATE", value: "UPDATE" },
        { label: "DELETE", value: "DELETE" },
      ],
    },
    ...(auditableFieldsSchema as FieldSchema[]),
  ],
};

export const entityConfig: EntityConfig = {
  name: "Permission",
  path: "/permissions",
  api: permissionApi,
  schema: permissionSchema,
  permissionOverrides: {
    canImport: false,
  },
};

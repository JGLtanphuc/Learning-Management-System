import { studentApi, classApi } from "@/api";
import {
  auditableFieldsSchema,
  type EntityConfig,
  type EntitySchema,
  type FieldSchema,
} from "@/types";
import { studentValidation } from "@/validations";

const studentSchema: EntitySchema = {
  entityName: "student",
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
      validation: studentValidation.studentCode,
    },
    {
      name: "fullName",
      label: "Full Name",
      type: "text",
      sortable: true,
      filterable: true,
      filterType: "text",
      validation: studentValidation.fullName,
    },
    {
      name: "email",
      label: "Email",
      type: "text",
      sortable: true,
      filterable: true,
      filterType: "text",
      validation: studentValidation.email,
    },
    {
      name: "phone",
      label: "Phone",
      type: "text",
      validation: studentValidation.phone,
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
      sortable: true,
      filterable: true,
      filterType: "select",
    },
    {
      name: "mockProjectName",
      label: "Mock Project",
      type: "text",
      editable: false,
      sortable: true,
      filterable: true,
      filterType: "text",
    },
    {
      name: "university",
      label: "University",
      type: "text",
      sortable: true,
      filterable: true,
      filterType: "text",
      validation: studentValidation.university,
    },
    {
      name: "major",
      label: "Major",
      type: "text",
      sortable: true,
      filterable: true,
      filterType: "text",
      validation: studentValidation.major,
    },
    {
      name: "gpa",
      label: "GPA",
      type: "number",
      sortable: true,
      filterable: true,
      filterType: "numberRange",
      validation: studentValidation.gpa,
    },
    ...(auditableFieldsSchema as FieldSchema[]),
  ],
};

export const entityConfig: EntityConfig = {
  name: "Fresher Trainee",
  path: "/students",
  api: studentApi,
  schema: studentSchema,
};


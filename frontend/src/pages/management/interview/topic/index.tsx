import { interviewTopicApi } from "@/api";
import {
  auditableFieldsSchema,
  type EntityConfig,
  type EntitySchema,
  type FieldSchema,
} from "@/types";
import { interviewTopicValidation } from "@/validations";

const interviewTopicSchema: EntitySchema = {
  entityName: "interview-topic",
  idField: "id",
  fields: [
    {
      name: "name",
      label: "Mục phỏng vấn",
      type: "text",
      sortable: true,
      bold: true,
      placeholder: "VD: Java & Spring Boot Fresher, React Fresher...",
      validation: interviewTopicValidation.name,
    },
    {
      name: "description",
      label: "Mô tả lĩnh vực",
      type: "textarea",
      sortable: false,
      placeholder: "Mô tả ngắn gọn nội dung kiến thức của mục phỏng vấn...",
      validation: interviewTopicValidation.description,
    },
    {
      name: "questionCount",
      label: "Số câu hỏi",
      type: "number",
      editable: false,
      sortable: true,
    },
    {
      name: "displayOrder",
      label: "Thứ tự hiển thị",
      type: "number",
      sortable: true,
      validation: interviewTopicValidation.displayOrder,
    },
    ...(auditableFieldsSchema as FieldSchema[]),
  ],
};

export const entityConfig: EntityConfig = {
  name: "Interview Topic",
  path: "/interview-topics",
  api: interviewTopicApi,
  schema: interviewTopicSchema,
  permissionOverrides: {
    canExport: true,
    canImport: false,
  },
};

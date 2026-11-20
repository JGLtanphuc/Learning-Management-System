import { interviewQuestionApi, interviewTopicApi } from "@/api";
import {
  auditableFieldsSchema,
  type EntityConfig,
  type EntitySchema,
  type FieldSchema,
} from "@/types";
import { interviewQuestionValidation } from "@/validations";

const interviewQuestionSchema: EntitySchema = {
  entityName: "interview-question",
  idField: "id",
  fields: [
    {
      name: "topicId",
      label: "Mục phỏng vấn",
      type: "relation",
      relation: {
        api: interviewTopicApi,
        valueField: "id",
        labelField: "name",
      },
      visible: false,
      hideable: false,
      filterable: true,
      filterType: "select",
      validation: interviewQuestionValidation.topicId,
    },
    {
      name: "topicName",
      label: "Mục phỏng vấn",
      type: "text",
      editable: false,
      sortable: true,
      bold: true,
    },
    {
      name: "questionType",
      label: "Loại câu hỏi",
      type: "select",
      sortable: true,
      filterable: true,
      filterType: "select",
      filterOptions: [
        { label: "Tự luận (Written)", value: "WRITTEN" },
        { label: "Trắc nghiệm (MCQ)", value: "MCQ" },
      ],
      options: [
        { label: "Tự luận (Written)", value: "WRITTEN" },
        { label: "Trắc nghiệm (MCQ)", value: "MCQ" },
      ],
      validation: interviewQuestionValidation.questionType,
    },
    {
      name: "questionText",
      label: "Nội dung câu hỏi",
      type: "textarea",
      sortable: false,
      bold: true,
      placeholder: "Nhập câu hỏi phỏng vấn...",
      validation: interviewQuestionValidation.questionText,
    },
    {
      name: "mcqOptions",
      label: "Đáp án trắc nghiệm",
      type: "mcq-options",
      visible: false,
      sortable: false,
    },
    {
      name: "sampleAnswer",
      label: "Gợi ý đáp án / Giải thích",
      type: "textarea",
      sortable: false,
      placeholder: "Nhập gợi ý đáp án chuẩn (Tự luận) hoặc giải thích thêm (Trắc nghiệm)...",
      validation: interviewQuestionValidation.sampleAnswer,
    },
    {
      name: "keywords",
      label: "Từ khóa chấm điểm (AI)",
      type: "text",
      sortable: false,
      placeholder: "VD: Dependency Injection, IoC, Bean, Constructor (cách nhau dấu phẩy)",
      validation: interviewQuestionValidation.keywords,
    },
    {
      name: "difficulty",
      label: "Độ khó",
      type: "select",
      sortable: true,
      filterable: true,
      filterType: "select",
      filterOptions: [
        { label: "Dễ (Easy)", value: "EASY" },
        { label: "Trung bình (Medium)", value: "MEDIUM" },
        { label: "Khó (Hard)", value: "HARD" },
      ],
      options: [
        { label: "Dễ (Easy)", value: "EASY" },
        { label: "Trung bình (Medium)", value: "MEDIUM" },
        { label: "Khó (Hard)", value: "HARD" },
      ],
      validation: interviewQuestionValidation.difficulty,
    },
    {
      name: "displayOrder",
      label: "Thứ tự",
      type: "number",
      sortable: true,
      validation: interviewQuestionValidation.displayOrder,
    },
    ...(auditableFieldsSchema as FieldSchema[]),
  ],
};

export const entityConfig: EntityConfig = {
  name: "Interview Question",
  path: "/interview-questions",
  api: interviewQuestionApi,
  schema: interviewQuestionSchema,
  permissionOverrides: {
    canExport: true,
    canImport: true,
  },
};

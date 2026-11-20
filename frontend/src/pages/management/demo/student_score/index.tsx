import { studentScoreApi } from "@/api";
import { StudentScoreCreatePage } from "./pages/StudentScoreCreatePage";
import { StudentScoreDetailPage } from "./pages/StudentScoreDetailPage";
import { StudentScoreEditPage } from "./pages/StudentScoreEditPage";
import {
  auditableFieldsSchema,
  createDynamicField,
  type EntityConfig,
  type EntitySchema,
  type FieldSchema,
} from "@/types";

const scoreColumnOrder = [
  "attendance",
  "quiz1",
  "quiz2",
  "assignment",
  "practice",
  "midterm",
  "final",
  "project",
  "bonus",
];

const scoreColumnLabels: Record<string, string> = {
  attendance: "Chuyên cần",
  quiz1: "Quiz 1",
  quiz2: "Quiz 2",
  assignment: "Assignment",
  practice: "Practice",
  midterm: "Midterm",
  final: "Final",
  project: "Mock Project",
  bonus: "Điểm cộng",
};

const studentScoreSchema: EntitySchema = {
  entityName: "student-score",
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
      type: "select",
      sortable: true,
      filterable: true,
      filterType: "select",
      filterOptions: [
        { label: "SE-01", value: "SE-01" },
        { label: "SE-02", value: "SE-02" },
        { label: "SE-03", value: "SE-03" },
      ],
      options: [
        { label: "SE-01", value: "SE-01" },
        { label: "SE-02", value: "SE-02" },
        { label: "SE-03", value: "SE-03" },
      ],
    },
    createDynamicField({
      name: "scores",
      label: "Score",
      keyOrder: scoreColumnOrder,
      keyLabelMap: scoreColumnLabels,
    }),
    {
      name: "overallScore",
      label: "Overall",
      type: "number",
      sortable: true,
      filterable: true,
      filterType: "numberRange",
      editable: false,
    },
    ...(auditableFieldsSchema as FieldSchema[]),
  ],
};

export const entityConfig: EntityConfig = {
  name: "Student Score",
  path: "/student-scores",
  api: studentScoreApi,
  schema: studentScoreSchema,
  actionPages: {
    create: {
      path: "/student-scores/create",
      component: StudentScoreCreatePage,
    },
    edit: {
      path: "/student-scores/:id/edit",
      component: StudentScoreEditPage,
    },
    detail: {
      path: "/student-scores/:id/detail",
      component: StudentScoreDetailPage,
    },
  },
};

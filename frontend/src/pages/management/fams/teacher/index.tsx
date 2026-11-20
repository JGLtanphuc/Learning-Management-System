import { teacherApi, classApi } from "@/api";
import {
  auditableFieldsSchema,
  type EntityConfig,
  type EntitySchema,
  type FieldSchema,
} from "@/types";
import { teacherValidation } from "@/validations";

const teacherSchema: EntitySchema = {
  entityName: "teacher",
  idField: "id",
  fields: [
    {
      name: "teacherCode",
      label: "Mã Giảng viên",
      type: "text",
      sortable: true,
      filterable: true,
      filterType: "text",
      bold: true,
      validation: teacherValidation.teacherCode,
    },
    {
      name: "fullName",
      label: "Họ và tên Giảng viên",
      type: "text",
      sortable: true,
      filterable: true,
      filterType: "text",
      bold: true,
      editable: false,
    },
    {
      name: "firstName",
      label: "Tên (First Name)",
      type: "text",
      visible: false,
      validation: teacherValidation.firstName,
    },
    {
      name: "lastName",
      label: "Họ và đệm (Last Name)",
      type: "text",
      visible: false,
      validation: teacherValidation.lastName,
    },
    {
      name: "email",
      label: "Email Giảng viên",
      type: "text",
      sortable: true,
      filterable: true,
      filterType: "text",
      validation: teacherValidation.email,
    },
    {
      name: "password",
      label: "Mật khẩu (Password)",
      type: "password",
      visible: false,
      hideable: false,
      validation: teacherValidation.password,
    },
    {
      name: "totalAssignedClasses",
      label: "Số lớp phụ trách",
      type: "number",
      sortable: true,
      editable: false,
    },
    {
      name: "classIds",
      label: "Phân công lớp phụ trách",
      type: "relation",
      relation: {
        api: classApi,
        valueField: "id",
        labelField: "className",
        multiple: true,
      },
      visible: false,
      hideable: false,
    },
    ...(auditableFieldsSchema as FieldSchema[]),
  ],
};

export const entityConfig: EntityConfig = {
  name: "Teacher",
  path: "/teachers",
  api: teacherApi,
  schema: teacherSchema,
};

export default entityConfig;


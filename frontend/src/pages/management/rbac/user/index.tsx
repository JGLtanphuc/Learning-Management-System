import { userApi } from "@/api";
import { roleApi } from "@/api/features/rbac/role.api";
import {
  auditableFieldsSchema,
  type EntityConfig,
  type EntitySchema,
  type FieldSchema,
} from "@/types";
import { userValidation } from "@/validations";

const userSchema: EntitySchema = {
  entityName: "user",
  idField: "id",
  fields: [
    {
      name: "email",
      label: "Email",
      type: "text",
      sortable: true,
      bold: true,
      validation: userValidation.email,
    },
    {
      name: "firstName",
      label: "First Name",
      type: "text",
      sortable: true,
      validation: userValidation.firstName,
    },
    {
      name: "lastName",
      label: "Last Name",
      type: "text",
      sortable: true,
      validation: userValidation.lastName,
    },
    {
      name: "password",
      label: "Password",
      type: "password",
      visible: false,
      hideable: false,
      validation: userValidation.password,
    },
    {
      name: "roleIds",
      label: "Role",
      type: "relation",
      relation: {
        api: roleApi,
        valueField: "id",
        labelField: "name",
        multiple: true,
      },
      filterable: true,
      filterType: "select",
    },
    ...(auditableFieldsSchema as FieldSchema[]),
  ],
};

export const entityConfig: EntityConfig = {
  name: "User",
  path: "/users",
  api: userApi,
  schema: userSchema,
};

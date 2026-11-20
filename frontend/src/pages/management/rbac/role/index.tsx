import { permissionApi, roleApi } from "@/api";
import {
  auditableFieldsSchema,
  type EntityConfig,
  type EntitySchema,
  type FieldSchema,
} from "@/types";
import { roleValidation } from "@/validations";

const roleSchema: EntitySchema = {
  entityName: "role",
  idField: "id",
  fields: [
    {
      name: "name",
      label: "Name",
      type: "text",
      sortable: true,
      bold: true,
      validation: roleValidation.name,
    },
    {
      name: "description",
      label: "Description",
      type: "textarea",
      sortable: true,
      validation: roleValidation.description,
    },
    {
      name: "permissionIds",
      label: "Permissions",
      type: "relation",
      editable: true,
      relation: {
        api: permissionApi,
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
  name: "Role",
  path: "/roles",
  api: roleApi,
  schema: roleSchema,
  permissionOverrides: {
    canImport: false,
  },
};

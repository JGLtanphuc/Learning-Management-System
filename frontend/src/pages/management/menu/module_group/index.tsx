import { moduleGroupApi } from "@/api";
import {
  auditableFieldsSchema,
  type EntityConfig,
  type EntitySchema,
  type FieldSchema,
} from "@/types";
import { moduleGroupValidation } from "@/validations";

const moduleGroupSchema: EntitySchema = {
  entityName: "module-group",
  idField: "id",
  fields: [
    {
      name: "name",
      label: "Name",
      type: "text",
      sortable: true,
      bold: true,
      validation: moduleGroupValidation.name,
    },
    {
      name: "description",
      label: "Description",
      type: "textarea",
      sortable: true,
      validation: moduleGroupValidation.description,
    },
    {
      name: "displayOrder",
      label: "Display Order",
      type: "number",
      sortable: true,
      validation: moduleGroupValidation.displayOrder,
    },
    ...(auditableFieldsSchema as FieldSchema[]),
  ],
};

export const entityConfig: EntityConfig = {
  name: "Module Group",
  path: "/menu-groups",
  api: moduleGroupApi,
  schema: moduleGroupSchema,
  permissionOverrides: {
    canImport: false,
  },
};

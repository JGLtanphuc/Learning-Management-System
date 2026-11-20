import { moduleApi, moduleGroupApi, permissionApi } from "@/api";
import { iconKeyOptions } from "@/components/datatable/iconMap";
import { auditableFieldsSchema, type EntityConfig, type EntitySchema, type FieldSchema } from "@/types";
import { moduleValidation } from "@/validations";

const moduleSchema: EntitySchema = {
    entityName: "module",
    idField: "id",
    fields: [
        {
            name: "title",
            label: "Tên phân hệ (Title)",
            type: "text",
            sortable: true,
            bold: true,
            placeholder: "VD: Lịch thi, Khảo sát...",
            validation: moduleValidation.title,
        },
        {
            name: "moduleGroupId",
            label: "Nhóm phân hệ (Group)",
            type: "relation",
            editable: true,
            relation: {
                api: moduleGroupApi,
                valueField: "id",
                labelField: "name",
            },
            filterable: true,
            filterType: "select",
            validation: moduleValidation.moduleGroupId,
        },
        {
            name: "url",
            label: "Đường dẫn (URL)",
            type: "text",
            sortable: true,
            placeholder: "VD: /exams, /surveys...",
            validation: moduleValidation.url,
        },
        {
            name: "icon",
            label: "Biểu tượng (Icon)",
            type: "icon",
            options: iconKeyOptions,
            filterable: true,
            filterType: "select",
        },
        {
            name: "requiredPermission",
            label: "Quyền truy cập",
            type: "relation",
            editable: true,
            relation: {
                api: permissionApi,
                valueField: "name",
                labelField: "name",
            },
            filterable: true,
            filterType: "select",
        },
        {
            name: "displayOrder",
            label: "Thứ tự hiển thị",
            type: "number",
            sortable: true,
            validation: moduleValidation.displayOrder,
        },
        ...(auditableFieldsSchema as FieldSchema[])
    ],
};

export const entityConfig: EntityConfig = {
    name: "Module",
    path: "/menus",
    api: moduleApi,
    schema: moduleSchema,
    permissionOverrides: {
        canImport: false,
    },
};

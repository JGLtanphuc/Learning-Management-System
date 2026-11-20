import {
  DefaultPagination,
  type FieldSchema,
  type EntitySchema,
  type FileFormat,
  type ImportResult,
  type Pagination,
  type SortEntry,
} from "@/types";
import { userApi } from "@/api";
import { downloadBlob, getFilenameFromHeader } from "@/components/datatable/util/dataio.utils";
import { usePermissions } from "@/hooks/usePermissions";
import {
  keepPreviousData,
  useMutation,
  useQueries,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import type { ValidationErrorResponse } from "@/types/common/error";
import { useEffect, useMemo, useState } from "react";
import axios from "axios";
import { toast } from "sonner";

const DYNAMIC_FIELD_PREFIX = "__dynamic__";

interface UseProTableOptions {
  permissionOverrides?: Partial<{
    canRead: boolean;
    canCreate: boolean;
    canUpdate: boolean;
    canDelete: boolean;
    canImport: boolean;
    canExport: boolean;
  }>;
}

const resolvePermissionResource = (entityName: string) => {
  let normalized = (entityName || "").trim().toLowerCase();
  if (normalized.startsWith("/")) normalized = normalized.substring(1);

  if (
    normalized === "module" ||
    normalized === "modules" ||
    normalized === "menu" ||
    normalized === "menus" ||
    normalized === "module-group" ||
    normalized === "module-groups" ||
    normalized === "module_group" ||
    normalized === "module_groups" ||
    normalized === "menu-group" ||
    normalized === "menu-groups"
  ) {
    return "MENU";
  }

  if (normalized === "permission" || normalized === "permissions") {
    return "PERMISSION";
  }

  if (normalized === "role" || normalized === "roles") {
    return "ROLE";
  }

  if (normalized === "user" || normalized === "users") {
    return "USER";
  }

  if (normalized === "student" || normalized === "students") {
    return "STUDENT";
  }

  if (
    normalized === "class" ||
    normalized === "classes" ||
    normalized === "training-class" ||
    normalized === "training-classes"
  ) {
    return "CLASS";
  }

  if (normalized === "book" || normalized === "books") {
    return "BOOK";
  }

  if (normalized === "attendance" || normalized === "attendances") {
    return "ATTENDANCE";
  }

  if (normalized === "allocation" || normalized === "allocations") {
    return "ALLOCATION";
  }

  if (
    normalized === "mock-project" ||
    normalized === "mock-projects" ||
    normalized === "mock_projects"
  ) {
    return "MOCK_PROJECT";
  }

  if (
    normalized === "student-score" ||
    normalized === "student-scores" ||
    normalized === "student_scores"
  ) {
    return "STUDENT_SCORE";
  }

  let code = normalized.replace(/[-\s]+/g, "_").toUpperCase();
  if (code.endsWith("ES") && code.length > 4) {
    code = code.substring(0, code.length - 2);
  } else if (code.endsWith("S") && code.length > 3 && !code.endsWith("SS")) {
    code = code.substring(0, code.length - 1);
  }
  return code;
};

const buildDynamicFieldName = (parentField: string, key: string) =>
  `${DYNAMIC_FIELD_PREFIX}${parentField}__${encodeURIComponent(key)}`;

const formatDynamicLabel = (key: string) =>
  key
    .replace(/[_-]+/g, " ")
    .trim()
    .replace(/\b\w/g, (char) => char.toUpperCase());

const isRecord = (value: any): value is Record<string, any> =>
  typeof value === "object" && value !== null && !Array.isArray(value);

const parseDynamicEntries = (
  field: FieldSchema,
  rawValue: any,
  row: any,
): Record<string, any> => {
  const customParser = field.dynamic?.parse;

  if (customParser) {
    const parsed = customParser(rawValue, row);
    return isRecord(parsed) ? parsed : {};
  }

  if (isRecord(rawValue)) {
    return rawValue;
  }

  if (!Array.isArray(rawValue)) {
    return {};
  }

  return rawValue.reduce<Record<string, any>>((acc, item, index) => {
    if (isRecord(item)) {
      const explicitKey = item.key ?? item.name ?? item.label;

      if (explicitKey !== undefined && explicitKey !== null && "value" in item) {
        acc[String(explicitKey)] = item.value;
        return acc;
      }

      const entries = Object.entries(item);

      if (entries.length === 1) {
        const [entryKey, entryValue] = entries[0];
        acc[String(entryKey)] = entryValue;
        return acc;
      }
    }

    acc[String(index + 1)] = item;
    return acc;
  }, {});
};

const orderDynamicKeys = (keys: string[], keyOrder: string[] = []) => {
  if (!keyOrder.length) {
    return [...keys].sort((a, b) => a.localeCompare(b));
  }

  const orderIndex = new Map(keyOrder.map((key, index) => [key, index]));

  return [...keys].sort((a, b) => {
    const aIndex = orderIndex.get(a);
    const bIndex = orderIndex.get(b);

    if (aIndex !== undefined && bIndex !== undefined) {
      return aIndex - bIndex;
    }

    if (aIndex !== undefined) {
      return -1;
    }

    if (bIndex !== undefined) {
      return 1;
    }

    return a.localeCompare(b);
  });
};

const mergeKeyOrder = (baseOrder: string[] = [], runtimeOrder: string[] = []) => {
  const seen = new Set<string>();
  const merged: string[] = [];

  [...baseOrder, ...runtimeOrder].forEach((key) => {
    if (!key || seen.has(key)) {
      return;
    }

    seen.add(key);
    merged.push(key);
  });

  return merged;
};

const formatEntityLabel = (entityName: string) =>
  entityName
    .split(/[-_\s]+/)
    .filter(Boolean)
    .map((chunk) => chunk.charAt(0).toUpperCase() + chunk.slice(1))
    .join(" ");

export function useProTable(api: any, schema: EntitySchema, options: UseProTableOptions = {}) {
  const qc = useQueryClient();
  const { hasPermission, activeRole } = usePermissions();
  const isAdmin = (activeRole || "").toUpperCase() === "ADMIN";

  const [pagination, setPagination] = useState<Pagination>(DefaultPagination);
  const [sortState, setSortState] = useState<SortEntry[]>([]);
  const [filters, setFilters] = useState<any>({});
  const [search, setSearch] = useState<string>("");

  const [selected, setSelected] = useState<any[]>([]);

  const [editingRow, setEditingRow] = useState<any | null>(null);
  const [isFormOpen, setFormOpen] = useState(false);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({});

  const [columnVisibility, setColumnVisibility] = useState<Record<string, boolean>>(() => {
    const initial: Record<string, boolean> = {};
    schema.fields.forEach((field) => {
      initial[field.name] = field.visible !== false;
    });
    return initial;
  });

  const toggleFieldVisibility = (fieldName: string, visible: boolean) => {
    setColumnVisibility((prev) => ({
      ...prev,
      [fieldName]: visible,
    }));
  };

  const toggleSort = (fieldName: string) => {
    setSortState((prev) => {
      const idx = prev.findIndex((s) => s.field === fieldName);
      if (idx === -1)
        return [...prev, { field: fieldName, direction: "asc" as const }];
      if (prev[idx].direction === "asc") {
        return prev.map((s, i) =>
          i === idx ? { ...s, direction: "desc" as const } : s,
        );
      }
      return prev.filter((_, i) => i !== idx);
    });
  };

  const clearSort = () => setSortState([]);

  const currentSort: string[] = sortState.map(
    (s) => `${s.field},${s.direction}`,
  );

  const relationFields = useMemo(
    () => schema.fields.filter((f) => f.type === "relation" && f.relation),
    [schema],
  );

  const permissionResource = useMemo(
    () => resolvePermissionResource(schema.entityName),
    [schema.entityName],
  );

  const entityLabel = useMemo(
    () => formatEntityLabel(schema.entityName),
    [schema.entityName],
  );

  const permissionKeys = useMemo(
    () => ({
      read: `${permissionResource}_READ`,
      create: `${permissionResource}_CREATE`,
      update: `${permissionResource}_UPDATE`,
      delete: `${permissionResource}_DELETE`,
      import: `${permissionResource}_CREATE`,
      export: `${permissionResource}_READ`,
    }),
    [permissionResource],
  );

  const permission = useMemo(
    () => ({
      resource: permissionResource,
      keys: permissionKeys,
      canRead: options.permissionOverrides?.canRead ?? hasPermission(permissionKeys.read),
      canCreate: options.permissionOverrides?.canCreate ?? hasPermission(permissionKeys.create),
      canUpdate: options.permissionOverrides?.canUpdate ?? hasPermission(permissionKeys.update),
      canDelete: options.permissionOverrides?.canDelete ?? hasPermission(permissionKeys.delete),
      canImport: options.permissionOverrides?.canImport ?? hasPermission(permissionKeys.import),
      canExport: options.permissionOverrides?.canExport ?? hasPermission(permissionKeys.export),
    }),
    [hasPermission, options.permissionOverrides, permissionKeys, permissionResource],
  );

  const hasAuditUserFields = useMemo(
    () => schema.fields.some((f) => f.name === "createdBy" || f.name === "updatedBy"),
    [schema],
  );

  const relationQueries = useQueries({
    queries: relationFields.map((field) => {
      const relPath = (field.relation?.api as any)?.path;
      let canReadRelation = true;
      if (relPath && !isAdmin) {
        const cleanPath = relPath.replace(/^\/+|\/+$/g, "");
        const resName = resolvePermissionResource(cleanPath);
        canReadRelation = hasPermission(`${resName}_READ`);
      }

      return {
        queryKey: ["relation", schema.entityName, field.name],
        queryFn: async () => {
          try {
            const res = await field.relation!.api.getPage({
              page: 0,
              size: 1000,
            });
            return res.content ?? (res as any).items ?? [];
          } catch {
            return [];
          }
        },
        enabled: permission.canRead && canReadRelation,
        retry: false,
        staleTime: 5 * 60 * 1000,
      };
    }),
  });

  const canReadUsers = isAdmin || hasPermission("USER_READ");

  const auditUsersQuery = useQuery({
    queryKey: ["relation", schema.entityName, "audit-users"],
    queryFn: async () => {
      try {
        const res = await userApi.getPage({
          page: 0,
          size: 1000,
        });
        return res.content ?? (res as any).items ?? [];
      } catch {
        return [];
      }
    },
    enabled: hasAuditUserFields && permission.canRead && canReadUsers,
    retry: false,
    staleTime: 5 * 60 * 1000,
  });


  const relationOptions = useMemo(() => {
    const map: Record<string, any[]> = {};
    relationFields.forEach((field, index) => {
      map[field.name] = relationQueries[index]?.data ?? [];
    });

    if (hasAuditUserFields) {
      const users = auditUsersQuery.data ?? [];
      if (schema.fields.some((f) => f.name === "createdBy")) {
        map.createdBy = users;
      }
      if (schema.fields.some((f) => f.name === "updatedBy")) {
        map.updatedBy = users;
      }
    }

    return map;
  }, [relationFields, relationQueries, hasAuditUserFields, auditUsersQuery.data, schema.fields]);

  const queryKey = [
    schema.entityName,
    pagination.page,
    pagination.size,
    currentSort,
    filters,
    search,
  ];

  const query = useQuery({
    queryKey,
    enabled: permission.canRead,
    queryFn: () =>
      api.getPage(
        {
          ...pagination,
          sort: currentSort.length > 0 ? currentSort : undefined,
        },
        search,
        filters,
      ),
    placeholderData: keepPreviousData,
  });

  const rawData = query.data?.content ?? [];

  const dynamicParentFields = useMemo(
    () => schema.fields.filter((field) => field.type === "dynamic"),
    [schema.fields],
  );

  const dynamicParentFieldMap = useMemo(
    () => new Map(dynamicParentFields.map((field) => [field.name, field])),
    [dynamicParentFields],
  );

  const dynamicChildFields = useMemo(() => {
    if (!dynamicParentFields.length || !rawData.length) {
      return [] as FieldSchema[];
    }

    return dynamicParentFields.flatMap((field) => {
      const keySet = new Set<string>();
      const runtimeKeyOrder: string[] = [];
      const runtimeKeyOrderSet = new Set<string>();
      const runtimeKeyLabelMap: Record<string, string> = {};

      rawData.forEach((row: any) => {
        const runtimeMeta = field.dynamic?.resolveMeta?.(row);

        runtimeMeta?.keyOrder?.forEach((key) => {
          if (!key || runtimeKeyOrderSet.has(key)) {
            return;
          }

          runtimeKeyOrderSet.add(key);
          runtimeKeyOrder.push(key);
        });

        if (runtimeMeta?.keyLabelMap) {
          Object.assign(runtimeKeyLabelMap, runtimeMeta.keyLabelMap);
        }

        const parsed = parseDynamicEntries(field, row?.[field.name], row);

        Object.keys(parsed).forEach((key) => {
          if (key) {
            keySet.add(key);
          }
        });
      });

      const orderedKeys = orderDynamicKeys(
        [...keySet],
        mergeKeyOrder(field.dynamic?.keyOrder, runtimeKeyOrder),
      );

      const keyLabelMap = {
        ...(field.dynamic?.keyLabelMap ?? {}),
        ...runtimeKeyLabelMap,
      };

      return orderedKeys.map((sourceKey) => {
        const mappedLabel = keyLabelMap[sourceKey] || formatDynamicLabel(sourceKey);
        const label = field.dynamic?.labelPrefix ? `${field.label} ${mappedLabel}` : mappedLabel;

        return {
          ...field,
          name: buildDynamicFieldName(field.name, sourceKey),
          label,
          type: field.dynamic?.valueType || "number",
          editable: false,
          sortable: false,
          filterable: false,
          relation: undefined,
          options: undefined,
          filterType: undefined,
          filterOptions: undefined,
          dynamicChild: {
            parentField: field.name,
            sourceKey,
          },
        } satisfies FieldSchema;
      });
    });
  }, [dynamicParentFields, rawData]);

  const dynamicChildrenByParent = useMemo(() => {
    const map = new Map<string, FieldSchema[]>();

    dynamicChildFields.forEach((field) => {
      const parentField = field.dynamicChild?.parentField;
      if (!parentField) {
        return;
      }

      if (!map.has(parentField)) {
        map.set(parentField, []);
      }

      map.get(parentField)!.push(field);
    });

    return map;
  }, [dynamicChildFields]);

  const resolvedSchema = useMemo<EntitySchema>(() => {
    const fields = schema.fields.flatMap((field) => {
      if (field.type !== "dynamic") {
        return [field];
      }

      return dynamicChildrenByParent.get(field.name) ?? [];
    });

    return {
      ...schema,
      fields,
    };
  }, [schema, dynamicChildrenByParent]);

  const data = useMemo(() => {
    if (!dynamicChildFields.length) {
      return rawData;
    }

    return rawData.map((row: any) => {
      let projectedRow = row;
      const parsedByParent = new Map<string, Record<string, any>>();

      dynamicChildFields.forEach((field) => {
        const dynamicChild = field.dynamicChild;
        if (!dynamicChild) {
          return;
        }

        const { parentField, sourceKey } = dynamicChild;
        let parsed = parsedByParent.get(parentField);

        if (!parsed) {
          const parent = dynamicParentFieldMap.get(parentField);
          parsed = parent ? parseDynamicEntries(parent, row?.[parentField], row) : {};
          parsedByParent.set(parentField, parsed);
        }

        if (projectedRow === row) {
          projectedRow = { ...row };
        }

        projectedRow[field.name] = parsed[sourceKey];
      });

      return projectedRow;
    });
  }, [rawData, dynamicChildFields, dynamicParentFieldMap]);

  useEffect(() => {
    const fieldNames = new Set(resolvedSchema.fields.map((field) => field.name));

    setColumnVisibility((prev) => {
      const next: Record<string, boolean> = { ...prev };
      let changed = false;

      Object.keys(next).forEach((key) => {
        if (!fieldNames.has(key)) {
          delete next[key];
          changed = true;
        }
      });

      resolvedSchema.fields.forEach((field) => {
        if (!(field.name in next)) {
          next[field.name] = field.visible !== false;
          changed = true;
        }
      });

      return changed ? next : prev;
    });
  }, [resolvedSchema.fields]);

  const visibleFields = resolvedSchema.fields.filter(
    (field) => columnVisibility[field.name] !== false,
  );

  const invalidate = () => {
    qc.invalidateQueries({ queryKey: [schema.entityName] });
    qc.invalidateQueries({ queryKey: ["rbac-roles-permissions"] });
    qc.invalidateQueries({ queryKey: ["sidebar-menu"] });
  };

  const handleMutationError = (error: unknown) => {
    if (axios.isAxiosError(error) && error.response?.status === 400) {
      const body = error.response.data as ValidationErrorResponse;
      if (body?.errors) {
        setFieldErrors(body.errors);
      }
    }
  };

  const getMutationErrorMessage = (error: unknown, fallback: string) => {
    if (axios.isAxiosError(error)) {
      const data = error.response?.data;
      if (data) {
        if (typeof data.message === "string" && data.message.trim()) {
          return data.message;
        }
        if (data.errors && typeof data.errors === "object") {
          const firstErrList = Object.values(data.errors).flat();
          if (firstErrList.length > 0 && typeof firstErrList[0] === "string") {
            return firstErrList[0];
          }
        }
        if (typeof data.detail === "string" && data.detail.trim()) {
          return data.detail;
        }
        if (typeof data.title === "string" && data.title.trim()) {
          return data.title;
        }
      }
      if (error.response?.status === 403) {
        return "Bạn không có quyền thực hiện thao tác này (403 Forbidden).";
      }
      if (error.response?.status === 401) {
        return "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại (401 Unauthorized).";
      }
    }

    return fallback;
  };

  const createMutation = useMutation({
    mutationFn: (data: any) => api.create(data),
    onSuccess: () => {
      invalidate();
      setFieldErrors({});
      setEditingRow(null);
      setFormOpen(false);
      toast.success(`${entityLabel} created successfully.`);
    },
    onError: (error) => {
      handleMutationError(error);
      toast.error(getMutationErrorMessage(error, `Create ${entityLabel} failed.`));
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: any) => api.update(id, data),
    onSuccess: () => {
      invalidate();
      setFieldErrors({});
      setEditingRow(null);
      setFormOpen(false);
      toast.success(`${entityLabel} updated successfully.`);
    },
    onError: (error) => {
      handleMutationError(error);
      toast.error(getMutationErrorMessage(error, `Update ${entityLabel} failed.`));
    },
  });

  const removeMutation = useMutation({
    mutationFn: (id: any) => api.delete(id),
  });

  const patchFieldMutation = useMutation({
    mutationFn: ({ id, data }: { id: any; data: Record<string, any> }) =>
      api.update(id, data),
    onSuccess: invalidate,
  });

  const patchField = (id: any, fieldName: string, value: any) => {
    if (!permission.canUpdate) return;

    const currentData: any[] = rawData;
    const row = currentData.find((r: any) => r[schema.idField] === id);
    if (!row) return;

    const updateData = { ...row, [fieldName]: value };

    delete updateData.createdAt;
    delete updateData.updatedAt;

    qc.setQueryData(queryKey, (old: any) => {
      if (!old?.content) return old;
      return {
        ...old,
        content: old.content.map((r: any) =>
          r[schema.idField] === id ? { ...r, [fieldName]: value } : r,
        ),
      };
    });
    patchFieldMutation.mutate(
      { id, data: updateData },
      {
        onError: () => {
          // Revert on error
          invalidate();
        },
      },
    );
  };

  const importMutation = useMutation<ImportResult, any, File>({
    mutationFn: api.import,
    onSuccess: invalidate,
  });

  const importFile = async (file: File) => {
    if (!permission.canImport) {
      throw new Error("Missing import permission");
    }

    return importMutation.mutateAsync(file);
  };

  const exportFile = async (format: FileFormat) => {
    if (!permission.canExport) {
      throw new Error("Missing export permission");
    }

    const res = await api.export(format);

    const contentDisposition = res.headers["content-disposition"];
    let filename = getFilenameFromHeader(contentDisposition);

    const ext = format === "EXCEL" ? "xlsx" : format === "CSV" ? "csv" : "pdf";
    const entityName = schema?.entityName || "data";

    if (!filename || filename.includes(";") || filename.includes("*=")) {
      const now = new Date();
      const dateStr = now.toISOString().slice(0, 10).replace(/-/g, "");
      const timeStr = String(now.getHours()).padStart(2, "0") + String(now.getMinutes()).padStart(2, "0");
      const cleanEntity = entityName.replace(/[^a-zA-Z0-9]/g, "_");
      filename = `FAS_${cleanEntity}_${dateStr}_${timeStr}.${ext}`;
    }

    const contentType =
      format === "EXCEL"
        ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        : format === "CSV"
          ? "text/csv"
          : "application/pdf";

    const blob = new Blob([res.data], { type: contentType });

    downloadBlob(blob, filename);
  };

  const openCreate = () => {
    if (!permission.canCreate) return;

    setEditingRow(null);
    setFieldErrors({});
    setFormOpen(true);
  };

  const openEdit = (row: any) => {
    if (!permission.canUpdate) return;

    setEditingRow(row);
    setFieldErrors({});
    setFormOpen(true);
  };

  const bulkDelete = async () => {
    if (!permission.canDelete) return;

    if (!selected.length) {
      return;
    }

    const ids = [...selected];

    const results = await Promise.all(
      ids.map((id) => remove(id, { silent: true, skipInvalidate: true })),
    );

    const successCount = results.filter(Boolean).length;
    const failedCount = ids.length - successCount;

    if (successCount > 0) {
      invalidate();
    }

    if (successCount > 0 && failedCount === 0) {
      toast.success(`Deleted ${successCount} ${entityLabel} record(s) successfully.`);
    } else if (successCount > 0) {
      toast.error(
        `Deleted ${successCount}/${ids.length} ${entityLabel} record(s). ${failedCount} failed.`,
      );
    } else {
      toast.error(`Delete ${entityLabel} failed.`);
    }

    setSelected([]);
  };

  const create = (data: any) => {
    if (!permission.canCreate) return;
    createMutation.mutate(data);
  };

  const update = ({ id, data }: any) => {
    if (!permission.canUpdate) return;
    updateMutation.mutate({ id, data });
  };

  const remove = async (
    id: any,
    options: { silent?: boolean; skipInvalidate?: boolean } = {},
  ) => {
    if (!permission.canDelete) return;

    try {
      await removeMutation.mutateAsync(id);

      if (!options.skipInvalidate) {
        invalidate();
      }

      if (!options.silent) {
        toast.success(`${entityLabel} deleted successfully.`);
      }

      return true;
    } catch (error) {
      if (!options.silent) {
        toast.error(getMutationErrorMessage(error, `Delete ${entityLabel} failed.`));
      }

      return false;
    }
  };

  return {
    schema: resolvedSchema,

    data,
    total: query.data?.totalElements ?? 0,
    loading: query.isLoading,
    isFetching: query.isFetching,

    page: pagination.page,
    size: pagination.size,
    sortState,
    toggleSort,
    clearSort,
    setPage: (page: number) => setPagination((prev) => ({ ...prev, page })),
    setSize: (size: number) => setPagination((prev) => ({ ...prev, size })),

    filters,
    setFilters,
    clearFilters: () => setFilters({}),
    search,
    setSearch,

    selected,
    setSelected,

    editingRow,
    isFormOpen,
    setFormOpen,

    columnVisibility,
    visibleFields,
    toggleFieldVisibility,

    openCreate,
    openEdit,

    create,
    update,
    remove,
    patchField,
    bulkDelete,

    isSubmitting: createMutation.isPending || updateMutation.isPending,

    importFile,
    exportFile,

    fieldErrors,
    setFieldErrors,
    relationOptions,
    permission,
  };
}

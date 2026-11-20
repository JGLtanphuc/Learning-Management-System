import { useState, useMemo, useEffect } from "react";
import { Checkbox } from "@/components/ui/checkbox";
import { extractResourceCode } from "@/utils/resource.utils";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { iconMap } from "@/components/datatable/iconMap";
import { moduleApi, moduleGroupApi, permissionApi } from "@/api";
import {
  Search,
  ShieldCheck,
  CheckCheck,
  XCircle,
  Eye,
  Plus,
  Pencil,
  Trash2,
  FolderTree,
  SlidersHorizontal,
  X,
} from "lucide-react";
import type { FieldSchema, RelationConfig } from "@/types";

const VIETNAMESE_NAMES: Record<string, string> = {
  "/attendances": "Điểm danh",
  "/student-scores": "Bảng điểm",
  "/topic-marks/gradebook-demo": "Sổ điểm",
  "/mock-projects": "Đồ án",
  "/books": "Tài liệu",
  "/classes": "Lớp học",
  "/teachers": "Giáo viên",
  "/students": "Học viên Fresher",
  "/allocations": "Phân bổ BU",
  "/users": "Người dùng",
  "/roles": "Vai trò",
  "/permissions": "Quyền hạn",
  "/menus": "Phân hệ (Modules)",
  "/menu-groups": "Nhóm phân hệ",
  "/dashboard": "Bảng tin Dashboard",
  "/interview-topics": "Chủ đề phỏng vấn",
  "/interview-questions": "Ngân hàng câu hỏi",
  "/interview-assignments": "Đợt phỏng vấn",
  "Teacher": "Giáo viên",
  "TEACHER": "Giáo viên",
  "Giảng viên": "Giáo viên",
};

const getVietnameseBadge = (row: { url?: string; title?: string; resourceCode?: string }) => {
  return (
    (row.url && VIETNAMESE_NAMES[row.url]) ||
    (row.title && VIETNAMESE_NAMES[row.title]) ||
    (row.resourceCode && VIETNAMESE_NAMES[row.resourceCode]) ||
    ""
  );
};

const SYSTEM_RESOURCE_METADATA: Record<
  string,
  { title: string; groupName: string; icon?: string }
> = {};

interface RolePermissionMatrixProps {
  field: FieldSchema;
  rel: RelationConfig;
  items: any[];
  value: any;
  onChange: (val: any[]) => void;
  fieldErrors?: string[];
}

interface ModuleRowItem {
  id: string | number;
  title: string;
  groupName: string;
  icon?: string;
  url?: string;
  resourceCode: string;
  readPerm?: any;
  createPerm?: any;
  updatePerm?: any;
  deletePerm?: any;
  otherPerms: any[];
  extraReadPerms?: any[];
  extraCreatePerms?: any[];
  extraUpdatePerms?: any[];
  extraDeletePerms?: any[];
}

const extractResourceFromModule = extractResourceCode;

export function RolePermissionMatrix({
  value,
  onChange,
  items: preloadedPermissions = [],
}: RolePermissionMatrixProps) {
  const [search, setSearch] = useState("");
  const [selectedGroup, setSelectedGroup] = useState<string>("ALL");
  const [allPermissions, setAllPermissions] =
    useState<any[]>(preloadedPermissions);
  const [allModules, setAllModules] = useState<any[]>([]);
  const [allGroups, setAllGroups] = useState<any[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  const selectedIds = useMemo(() => {
    return new Set<number>(Array.isArray(value) ? value.map(Number) : []);
  }, [value]);

  useEffect(() => {
    let isMounted = true;
    async function loadData() {
      setIsLoading(true);
      try {
        const [permRes, modRes, grpRes] = await Promise.all([
          allPermissions.length > 0
            ? Promise.resolve({ content: allPermissions })
            : permissionApi.getPage({ page: 0, size: 9999 }),
          moduleApi.getPage({ page: 0, size: 9999 }),
          moduleGroupApi.getPage({ page: 0, size: 9999 }),
        ]);

        if (!isMounted) return;

        const permList = permRes.content ?? (permRes as any).items ?? [];
        const modList = modRes.content ?? (modRes as any).items ?? [];
        const grpList = grpRes.content ?? (grpRes as any).items ?? [];

        setAllPermissions(permList);
        setAllModules(modList);
        setAllGroups(grpList);
      } catch (err) {
        console.error("Failed to load permission matrix data:", err);
      } finally {
        if (isMounted) setIsLoading(false);
      }
    }

    loadData();
    return () => {
      isMounted = false;
    };
  }, []);

  // Map module groups by ID
  const groupMap = useMemo(() => {
    const map = new Map<number | string, string>();
    allGroups.forEach((g) => {
      if (g.id) map.set(g.id, g.name || "Khác");
    });
    return map;
  }, [allGroups]);

  // Build matrix rows
  const matrixRows = useMemo(() => {
    const rows: ModuleRowItem[] = [];
    const matchedPermIds = new Set<number | string>();

    // 1. Process each module from DB
    allModules.forEach((mod) => {
      const resCode = extractResourceFromModule(
        mod.url,
        mod.title,
        mod.requiredPermission,
      );
      const groupName = groupMap.get(mod.moduleGroupId) || "Phân hệ chung";

      const isInterviewAssignment =
        resCode === "INTERVIEW_ASSIGNMENT" ||
        mod.url === "/interview-assignments";

      // Find all permissions matching this module resource exactly
      const normResCode = resCode.replace(/-/g, "_").replace(/S$/, "");
      const related = allPermissions.filter((p) => {
        const pRes = (p.resource || "")
          .toUpperCase()
          .replace(/-/g, "_")
          .replace(/S$/, "");
        const pName = (p.name || "").toUpperCase();

        if (isInterviewAssignment) {
          if (
            pRes === "INTERVIEW_ASSIGNMENT" ||
            pRes === "INTERVIEW_ANSWER" ||
            pRes === "EXAM" ||
            pName.startsWith("INTERVIEW_ASSIGNMENT_") ||
            pName.startsWith("INTERVIEW_ANSWER_") ||
            pName.startsWith("EXAM_")
          ) {
            return true;
          }
        }

        return (
          pRes === resCode ||
          pRes === normResCode ||
          pName.startsWith(`${resCode}_`) ||
          pName.startsWith(`${normResCode}_`) ||
          (mod.requiredPermission &&
            pName === mod.requiredPermission.toUpperCase())
        );
      });

      let readPerm: any;
      let createPerm: any;
      let updatePerm: any;
      let deletePerm: any;
      const otherPerms: any[] = [];
      const extraReadPerms: any[] = [];
      const extraCreatePerms: any[] = [];
      const extraUpdatePerms: any[] = [];
      const extraDeletePerms: any[] = [];

      related.forEach((p) => {
        matchedPermIds.add(p.id);
        const action = (p.action || "").toUpperCase();
        const name = (p.name || "").toUpperCase();

        if (action === "CREATE" || name.endsWith("_CREATE")) {
          if (
            !createPerm ||
            name === `${resCode}_CREATE` ||
            name === `${normResCode}_CREATE`
          ) {
            if (createPerm && createPerm.id !== p.id)
              extraCreatePerms.push(createPerm);
            createPerm = p;
          } else {
            extraCreatePerms.push(p);
          }
        } else if (action === "UPDATE" || name.endsWith("_UPDATE")) {
          if (
            !updatePerm ||
            name === `${resCode}_UPDATE` ||
            name === `${normResCode}_UPDATE`
          ) {
            if (updatePerm && updatePerm.id !== p.id)
              extraUpdatePerms.push(updatePerm);
            updatePerm = p;
          } else {
            extraUpdatePerms.push(p);
          }
        } else if (action === "DELETE" || name.endsWith("_DELETE")) {
          if (
            !deletePerm ||
            name === `${resCode}_DELETE` ||
            name === `${normResCode}_DELETE`
          ) {
            if (deletePerm && deletePerm.id !== p.id)
              extraDeletePerms.push(deletePerm);
            deletePerm = p;
          } else {
            extraDeletePerms.push(p);
          }
        } else if (
          action === "READ" ||
          name.endsWith("_READ") ||
          (mod.requiredPermission &&
            name === mod.requiredPermission.toUpperCase())
        ) {
          if (
            !readPerm ||
            name === `${resCode}_READ` ||
            name === `${normResCode}_READ`
          ) {
            if (readPerm && readPerm.id !== p.id) extraReadPerms.push(readPerm);
            readPerm = p;
          } else {
            extraReadPerms.push(p);
          }
        } else {
          otherPerms.push(p);
        }
      });

      rows.push({
        id: mod.id || `mod-${mod.title}`,
        title: mod.title,
        groupName,
        icon: mod.icon,
        url: mod.url,
        resourceCode: resCode,
        readPerm,
        createPerm,
        updatePerm,
        deletePerm,
        otherPerms,
        extraReadPerms,
        extraCreatePerms,
        extraUpdatePerms,
        extraDeletePerms,
      });
    });

    // 2. Any unmatched permissions go into "System Resources" or designated groups
    const remaining = allPermissions.filter((p) => !matchedPermIds.has(p.id));
    if (remaining.length > 0) {
      const remByRes = new Map<string, any[]>();
      remaining.forEach((p) => {
        let res = (p.resource || "HỆ THỐNG").toUpperCase().replace(/-/g, "_");
        if (res.endsWith("S") && res.length > 4 && !res.endsWith("SS")) {
          res = res.substring(0, res.length - 1);
        }
        if (!remByRes.has(res)) remByRes.set(res, []);
        remByRes.get(res)!.push(p);
      });

      remByRes.forEach((perms, res) => {
        let readPerm: any;
        let createPerm: any;
        let updatePerm: any;
        let deletePerm: any;
        const otherPerms: any[] = [];

        perms.forEach((p) => {
          const action = (p.action || "").toUpperCase();
          const name = (p.name || "").toUpperCase();
          if (action === "READ" || name.endsWith("_READ")) readPerm = p;
          else if (action === "CREATE" || name.endsWith("_CREATE"))
            createPerm = p;
          else if (action === "UPDATE" || name.endsWith("_UPDATE"))
            updatePerm = p;
          else if (action === "DELETE" || name.endsWith("_DELETE"))
            deletePerm = p;
          else otherPerms.push(p);
        });

        const meta =
          SYSTEM_RESOURCE_METADATA[res] ||
          (res.startsWith("INTERVIEW_")
            ? {
                title: `Phỏng vấn: ${res.replace("INTERVIEW_", "")}`,
                groupName: "Interview Management",
                icon: "message-square",
              }
            : null);

        rows.push({
          id: `sys-${res}`,
          title: meta ? meta.title : `Tài nguyên: ${res}`,
          groupName: meta ? meta.groupName : "Quản trị hệ thống nâng cao",
          icon: meta?.icon,
          resourceCode: res,
          readPerm,
          createPerm,
          updatePerm,
          deletePerm,
          otherPerms,
        });
      });
    }

    return rows;
  }, [allModules, allPermissions, groupMap]);

  // Unique groups for tabs
  const groupTabs = useMemo(() => {
    const set = new Set<string>();
    matrixRows.forEach((r) => set.add(r.groupName));
    return ["ALL", ...Array.from(set)];
  }, [matrixRows]);

  // Filter rows by group and search query
  const filteredRows = useMemo(() => {
    return matrixRows.filter((r) => {
      const matchGroup =
        selectedGroup === "ALL" || r.groupName === selectedGroup;
      const q = search.trim().toLowerCase();
      const vn =
        r.url && VIETNAMESE_NAMES[r.url]
          ? VIETNAMESE_NAMES[r.url].toLowerCase()
          : "";
      const matchSearch =
        !q ||
        r.title.toLowerCase().includes(q) ||
        vn.includes(q) ||
        r.groupName.toLowerCase().includes(q) ||
        r.resourceCode.toLowerCase().includes(q);
      return matchGroup && matchSearch;
    });
  }, [matrixRows, selectedGroup, search]);

  // Toggle single permission (with optional related permissions)
  const handleToggle = (
    permId?: number | string,
    extraIds?: (number | string)[],
  ) => {
    if (!permId) return;
    const numId = Number(permId);
    const next = new Set(selectedIds);
    const willAdd = !next.has(numId);
    if (willAdd) {
      next.add(numId);
      extraIds?.forEach((eid) => next.add(Number(eid)));
    } else {
      next.delete(numId);
      extraIds?.forEach((eid) => next.delete(Number(eid)));
    }
    onChange(Array.from(next));
  };

  // Toggle all permissions for a specific module
  const handleToggleModuleAll = (row: ModuleRowItem) => {
    const modulePerms = [
      row.readPerm?.id,
      row.createPerm?.id,
      row.updatePerm?.id,
      row.deletePerm?.id,
      ...(row.extraReadPerms || []).map((p) => p.id),
      ...(row.extraCreatePerms || []).map((p) => p.id),
      ...(row.extraUpdatePerms || []).map((p) => p.id),
      ...(row.extraDeletePerms || []).map((p) => p.id),
      ...row.otherPerms.map((p) => p.id),
    ]
      .filter(Boolean)
      .map(Number);

    const allSelected =
      modulePerms.length > 0 && modulePerms.every((id) => selectedIds.has(id));
    const next = new Set(selectedIds);

    if (allSelected) {
      modulePerms.forEach((id) => next.delete(id));
    } else {
      modulePerms.forEach((id) => next.add(id));
    }
    onChange(Array.from(next));
  };

  // Select all permissions in entire system
  const handleSelectAll = () => {
    const allIds = allPermissions.map((p) => p.id).filter(Boolean);
    onChange(allIds);
  };

  // Select only READ permissions
  const handleSelectReadOnly = () => {
    const readIds = allPermissions
      .filter((p) => {
        const action = (p.action || "").toUpperCase();
        const name = (p.name || "").toUpperCase();
        return action === "READ" || name.endsWith("_READ");
      })
      .map((p) => p.id);
    onChange(readIds);
  };

  // Clear all
  const handleClearAll = () => {
    onChange([]);
  };

  return (
    <div className="space-y-3.5 w-full min-w-0">
      {/* Top Action Card */}
      <div className="flex flex-col gap-3 bg-slate-50/80 dark:bg-slate-800/40 p-3 sm:p-3.5 rounded-2xl border border-slate-200/90 dark:border-slate-800 shadow-2xs">
        <div className="flex items-center justify-between gap-2">
          <div className="flex items-center gap-2.5 min-w-0">
            <div className="size-8 rounded-xl bg-slate-900 text-white dark:bg-white dark:text-slate-900 flex items-center justify-center shadow-xs shrink-0">
              <ShieldCheck className="size-4.5" />
            </div>
            <div className="min-w-0">
              <span className="text-xs font-bold text-slate-900 dark:text-slate-100 block truncate">
                Ma trận Phân quyền theo Phân hệ
              </span>
              <div className="flex items-center gap-1.5 mt-0.5">
                <span className="text-[11px] text-slate-500">
                  Đã kích hoạt:
                </span>
                <span className="text-[10px] font-bold px-2 py-0.5 rounded-full bg-slate-100 dark:bg-slate-800 text-slate-800 dark:text-slate-200 border border-slate-300 dark:border-slate-700">
                  {selectedIds.size} / {allPermissions.length} quyền
                </span>
              </div>
            </div>
          </div>
        </div>

        {/* Quick Batch Buttons - Responsive: 3 columns on mobile, inline flex on desktop */}
        <div className="grid grid-cols-3 gap-1.5 sm:flex sm:flex-wrap sm:items-center">
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={handleSelectAll}
            className="text-[11px] sm:text-xs h-8 px-2 sm:px-3 text-slate-900 dark:text-slate-100 border-slate-300 dark:border-slate-700 hover:bg-slate-100 dark:hover:bg-slate-800 font-medium cursor-pointer shadow-2xs truncate justify-center"
          >
            <CheckCheck className="size-3.5 mr-1 shrink-0 hidden sm:inline" />
            Chọn tất cả
          </Button>

          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={handleSelectReadOnly}
            className="text-[11px] sm:text-xs h-8 px-2 sm:px-3 text-slate-700 dark:text-slate-300 border-slate-300 dark:border-slate-700 hover:bg-slate-100 dark:hover:bg-slate-800 font-medium cursor-pointer shadow-2xs truncate justify-center"
          >
            <Eye className="size-3.5 mr-1 shrink-0 hidden sm:inline" />
            Chỉ quyền Xem
          </Button>

          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={handleClearAll}
            className="text-[11px] sm:text-xs h-8 px-2 sm:px-3 text-slate-600 hover:bg-slate-100 dark:text-slate-400 dark:hover:bg-slate-800 cursor-pointer shadow-2xs truncate justify-center"
          >
            <XCircle className="size-3.5 mr-1 shrink-0 hidden sm:inline" />
            Bỏ chọn hết
          </Button>
        </div>
      </div>

      {/* Filter and Search Bar */}
      <div className="space-y-2 w-full min-w-0">
        <div className="relative w-full">
          <Search className="size-4 text-slate-400 absolute left-3 top-1/2 -translate-y-1/2" />
          <Input
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Tìm kiếm phân hệ, nhóm, quyền..."
            className="pl-9 pr-8 h-9 text-xs rounded-xl bg-slate-50/50 dark:bg-slate-800/40 border-slate-200 dark:border-slate-800"
          />
          {search && (
            <button
              type="button"
              onClick={() => setSearch("")}
              className="absolute right-2.5 top-1/2 -translate-y-1/2 text-slate-400 hover:text-slate-600 cursor-pointer p-0.5"
            >
              <X className="size-3.5" />
            </button>
          )}
        </div>

        {/* Group Tab Pills with smooth horizontal swipe */}
        <div className="flex items-center gap-1.5 overflow-x-auto w-full scrollbar-none py-1 -mx-0.5 px-0.5">
          {groupTabs.map((grp) => {
            const isActive = selectedGroup === grp;
            return (
              <button
                key={grp}
                type="button"
                onClick={() => setSelectedGroup(grp)}
                className={`text-[11px] px-3 py-1.5 rounded-xl whitespace-nowrap transition-all font-medium border cursor-pointer shrink-0 ${
                  isActive
                    ? "bg-slate-900 text-white border-slate-900 dark:bg-white dark:text-slate-900 dark:border-white shadow-2xs font-semibold"
                    : "bg-white dark:bg-slate-900 text-slate-600 dark:text-slate-400 border-slate-200 dark:border-slate-800 hover:bg-slate-50 dark:hover:bg-slate-800/50"
                }`}
              >
                {grp === "ALL" ? "Tất cả nhóm" : grp}
              </button>
            );
          })}
        </div>
      </div>

      {/* MOBILE VIEW (< 640px): Touch-friendly, high-information card list */}
      <div className="block sm:hidden space-y-2.5 max-h-[460px] overflow-y-auto pr-0.5">
        {isLoading ? (
          <div className="py-12 text-center text-xs text-slate-400">
            Đang tải danh sách phân hệ và quyền...
          </div>
        ) : filteredRows.length === 0 ? (
          <div className="py-12 text-center text-xs text-slate-400">
            Không tìm thấy phân hệ nào khớp với từ khóa tìm kiếm.
          </div>
        ) : (
          filteredRows.map((row) => {
            const IconComponent =
              row.icon && row.icon in iconMap
                ? iconMap[row.icon as keyof typeof iconMap]
                : null;

            const modulePermIds = [
              row.readPerm?.id,
              row.createPerm?.id,
              row.updatePerm?.id,
              row.deletePerm?.id,
              ...row.otherPerms.map((p) => p.id),
            ]
              .filter(Boolean)
              .map(Number);

            const isAllSelected =
              modulePermIds.length > 0 &&
              modulePermIds.every((id) => selectedIds.has(id));

            return (
              <div
                key={row.id}
                className="p-3 bg-white dark:bg-slate-900 border border-slate-200/90 dark:border-slate-800 rounded-2xl shadow-2xs space-y-2.5"
              >
                {/* Card Header: Icon, Titles & All Toggle */}
                <div className="flex items-start justify-between gap-2">
                  <div className="flex items-start gap-2.5 min-w-0">
                    <div className="size-8 rounded-xl bg-slate-100 dark:bg-slate-800 text-slate-700 dark:text-slate-300 flex items-center justify-center shrink-0 border border-slate-200 dark:border-slate-700 shadow-2xs mt-0.5">
                      {IconComponent ? (
                        <IconComponent className="size-4" />
                      ) : (
                        <SlidersHorizontal className="size-4" />
                      )}
                    </div>
                    <div className="min-w-0 flex-1">
                      <div className="flex flex-wrap items-center gap-1.5 font-bold text-slate-800 dark:text-slate-200 text-xs">
                        <span>{row.title}</span>
                        {getVietnameseBadge(row) && (
                          <span className="text-[10px] text-slate-700 dark:text-slate-300 font-semibold px-1.5 py-0.2 rounded-md bg-slate-100 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 shrink-0">
                            {getVietnameseBadge(row)}
                          </span>
                        )}
                      </div>
                      <div className="mt-0.5 text-[10px] text-slate-400 truncate">
                        <span>{row.groupName}</span>
                      </div>
                    </div>
                  </div>

                  {/* Toggle All Checkbox/Button */}
                  {modulePermIds.length > 0 && (
                    <button
                      type="button"
                      onClick={() => handleToggleModuleAll(row)}
                      className={`text-[10px] px-2 py-1 rounded-lg border font-semibold shrink-0 transition-colors flex items-center gap-1 cursor-pointer ${
                        isAllSelected
                          ? "bg-slate-900 text-white border-slate-900 dark:bg-white dark:text-slate-900 dark:border-white shadow-2xs"
                          : "bg-slate-50 dark:bg-slate-800 text-slate-600 dark:text-slate-400 border-slate-200 dark:border-slate-700 hover:bg-slate-100"
                      }`}
                    >
                      <CheckCheck className="size-3" />
                      {isAllSelected ? "Tất cả" : "Chọn hết"}
                    </button>
                  )}
                </div>

                {/* 4 CRUD Action Buttons (Pill Grid) */}
                <div className="grid grid-cols-4 gap-1.5 pt-1 border-t border-slate-100 dark:border-slate-800">
                  {/* READ */}
                  {row.readPerm ? (
                    <button
                      type="button"
                      onClick={() =>
                        handleToggle(
                          row.readPerm.id,
                          row.resourceCode === "INTERVIEW_ASSIGNMENT" ||
                            row.url === "/interview-assignments"
                            ? [
                                ...(row.extraReadPerms || []),
                                ...(row.extraCreatePerms || []),
                                ...(row.extraUpdatePerms || []),
                                ...(row.extraDeletePerms || []),
                                ...(row.otherPerms || []),
                              ].map((p: any) => p.id)
                            : row.extraReadPerms?.map((p: any) => p.id),
                        )
                      }
                      className={`flex items-center justify-center gap-1 py-1.5 px-1 rounded-xl border text-[11px] font-medium transition-all cursor-pointer ${
                        selectedIds.has(Number(row.readPerm.id))
                          ? "bg-slate-900 dark:bg-white text-white dark:text-slate-900 border-slate-900 dark:border-white font-bold shadow-2xs"
                          : "bg-slate-50/60 dark:bg-slate-800/40 text-slate-400 dark:text-slate-500 border-slate-200/80 dark:border-slate-800 hover:bg-slate-100"
                      }`}
                    >
                      <Eye className="size-3 shrink-0" />
                      <span>Xem</span>
                    </button>
                  ) : (
                    <div className="py-1.5 text-center text-[10px] text-slate-300 dark:text-slate-600">
                      —
                    </div>
                  )}

                  {/* CREATE */}
                  {row.createPerm ? (
                    <button
                      type="button"
                      onClick={() =>
                        handleToggle(
                          row.createPerm.id,
                          row.extraCreatePerms?.map((p: any) => p.id),
                        )
                      }
                      className={`flex items-center justify-center gap-1 py-1.5 px-1 rounded-xl border text-[11px] font-medium transition-all cursor-pointer ${
                        selectedIds.has(Number(row.createPerm.id))
                          ? "bg-slate-900 dark:bg-white text-white dark:text-slate-900 border-slate-900 dark:border-white font-bold shadow-2xs"
                          : "bg-slate-50/60 dark:bg-slate-800/40 text-slate-400 dark:text-slate-500 border-slate-200/80 dark:border-slate-800 hover:bg-slate-100"
                      }`}
                    >
                      <Plus className="size-3 shrink-0" />
                      <span>Thêm</span>
                    </button>
                  ) : (
                    <div className="py-1.5 text-center text-[10px] text-slate-300 dark:text-slate-600">
                      —
                    </div>
                  )}

                  {/* UPDATE */}
                  {row.updatePerm ? (
                    <button
                      type="button"
                      onClick={() =>
                        handleToggle(
                          row.updatePerm.id,
                          [
                            ...(row.extraUpdatePerms || []),
                            ...(row.otherPerms || []),
                          ].map((p: any) => p.id),
                        )
                      }
                      className={`flex items-center justify-center gap-1 py-1.5 px-1 rounded-xl border text-[11px] font-medium transition-all cursor-pointer ${
                        selectedIds.has(Number(row.updatePerm.id))
                          ? "bg-slate-900 dark:bg-white text-white dark:text-slate-900 border-slate-900 dark:border-white font-bold shadow-2xs"
                          : "bg-slate-50/60 dark:bg-slate-800/40 text-slate-400 dark:text-slate-500 border-slate-200/80 dark:border-slate-800 hover:bg-slate-100"
                      }`}
                    >
                      <Pencil className="size-3 shrink-0" />
                      <span>Sửa</span>
                    </button>
                  ) : (
                    <div className="py-1.5 text-center text-[10px] text-slate-300 dark:text-slate-600">
                      —
                    </div>
                  )}

                  {/* DELETE */}
                  {row.deletePerm ? (
                    <button
                      type="button"
                      onClick={() =>
                        handleToggle(
                          row.deletePerm.id,
                          row.extraDeletePerms?.map((p: any) => p.id),
                        )
                      }
                      className={`flex items-center justify-center gap-1 py-1.5 px-1 rounded-xl border text-[11px] font-medium transition-all cursor-pointer ${
                        selectedIds.has(Number(row.deletePerm.id))
                          ? "bg-slate-900 dark:bg-white text-white dark:text-slate-900 border-slate-900 dark:border-white font-bold shadow-2xs"
                          : "bg-slate-50/60 dark:bg-slate-800/40 text-slate-400 dark:text-slate-500 border-slate-200/80 dark:border-slate-800 hover:bg-slate-100"
                      }`}
                    >
                      <Trash2 className="size-3 shrink-0" />
                      <span>Xóa</span>
                    </button>
                  ) : (
                    <div className="py-1.5 text-center text-[10px] text-slate-300 dark:text-slate-600">
                      —
                    </div>
                  )}
                </div>
              </div>
            );
          })
        )}
      </div>

      {/* DESKTOP / TABLET VIEW (>= 640px): Concise Table Matrix */}
      <div className="hidden sm:flex flex-col border border-slate-200/90 dark:border-slate-800 rounded-2xl overflow-hidden bg-white dark:bg-slate-900 shadow-xs w-full min-w-0">
        {/* Table Header */}
        <div className="grid grid-cols-12 gap-2 bg-slate-50 dark:bg-slate-800/90 px-4 py-3 text-xs font-bold text-slate-700 dark:text-slate-300 border-b border-slate-200/80 dark:border-slate-800 items-center select-none shrink-0 min-w-0">
          <div className="col-span-5 md:col-span-6 flex items-center gap-2 min-w-0">
            <FolderTree className="size-4 text-slate-500 shrink-0" />
            <span className="truncate">Phân hệ / Chức năng</span>
          </div>
          <div className="col-span-6 md:col-span-5 grid grid-cols-4 gap-1 text-center min-w-0">
            <span className="text-slate-700 dark:text-slate-300 font-semibold flex items-center justify-center gap-1 truncate">
              <Eye className="size-3 shrink-0" /> Xem
            </span>
            <span className="text-slate-700 dark:text-slate-300 font-semibold flex items-center justify-center gap-1 truncate">
              <Plus className="size-3 shrink-0" /> Thêm
            </span>
            <span className="text-slate-700 dark:text-slate-300 font-semibold flex items-center justify-center gap-1 truncate">
              <Pencil className="size-3 shrink-0" /> Sửa
            </span>
            <span className="text-slate-700 dark:text-slate-300 font-semibold flex items-center justify-center gap-1 truncate">
              <Trash2 className="size-3 shrink-0" /> Xóa
            </span>
          </div>
          <div className="col-span-1 text-center text-[11px] text-slate-500 font-medium min-w-0 truncate">
            Tất cả
          </div>
        </div>

        {/* Table Rows - Smooth vertical scroll, zero horizontal bar */}
        <div className="overflow-y-auto overflow-x-hidden max-h-[380px] divide-y divide-slate-100 dark:divide-slate-800/60 w-full min-w-0">
          {isLoading ? (
            <div className="py-12 text-center text-xs text-slate-400">
              Đang tải danh sách phân hệ và quyền...
            </div>
          ) : filteredRows.length === 0 ? (
            <div className="py-12 text-center text-xs text-slate-400">
              Không tìm thấy phân hệ nào khớp với từ khóa tìm kiếm.
            </div>
          ) : (
            filteredRows.map((row) => {
              const IconComponent =
                row.icon && row.icon in iconMap
                  ? iconMap[row.icon as keyof typeof iconMap]
                  : null;

              const modulePermIds = [
                row.readPerm?.id,
                row.createPerm?.id,
                row.updatePerm?.id,
                row.deletePerm?.id,
                ...row.otherPerms.map((p) => p.id),
              ]
                .filter(Boolean)
                .map(Number);

              const isAllSelected =
                modulePermIds.length > 0 &&
                modulePermIds.every((id) => selectedIds.has(id));

              return (
                <div
                  key={row.id}
                  className="grid grid-cols-12 gap-2 px-4 py-2.5 text-xs items-center hover:bg-slate-50/70 dark:hover:bg-slate-800/40 transition-colors w-full min-w-0"
                >
                  {/* Module Info */}
                  <div className="col-span-5 md:col-span-6 flex items-center gap-2.5 min-w-0 pr-2">
                    <div className="size-8 rounded-lg bg-slate-100 dark:bg-slate-800 text-slate-700 dark:text-slate-300 flex items-center justify-center shrink-0 border border-slate-200 dark:border-slate-700 shadow-2xs">
                      {IconComponent ? (
                        <IconComponent className="size-4" />
                      ) : (
                        <SlidersHorizontal className="size-4" />
                      )}
                    </div>
                    <div className="min-w-0 flex-1">
                      <div className="font-semibold text-slate-800 dark:text-slate-200 truncate flex items-center gap-1.5 min-w-0">
                        <span className="truncate">{row.title}</span>
                        {getVietnameseBadge(row) && (
                          <span className="text-[10px] text-slate-700 dark:text-slate-300 font-medium px-1.5 py-0.2 rounded-md bg-slate-100 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 shrink-0">
                            {getVietnameseBadge(row)}
                          </span>
                        )}
                      </div>
                      <div className="mt-0.5 min-w-0">
                        <span className="text-[10px] text-slate-400 truncate block">
                          {row.groupName}
                        </span>
                      </div>
                    </div>
                  </div>

                  {/* 4 CRUD Checkboxes */}
                  <div className="col-span-6 md:col-span-5 grid grid-cols-4 gap-1 items-center text-center min-w-0">
                    {/* READ */}
                    <div className="flex items-center justify-center">
                      {row.readPerm ? (
                        <Checkbox
                          checked={selectedIds.has(Number(row.readPerm.id))}
                          onCheckedChange={() =>
                            handleToggle(
                              row.readPerm.id,
                              row.resourceCode === "INTERVIEW_ASSIGNMENT" ||
                                row.url === "/interview-assignments"
                                ? [
                                    ...(row.extraReadPerms || []),
                                    ...(row.extraCreatePerms || []),
                                    ...(row.extraUpdatePerms || []),
                                    ...(row.extraDeletePerms || []),
                                    ...(row.otherPerms || []),
                                  ].map((p: any) => p.id)
                                : row.extraReadPerms?.map((p: any) => p.id),
                            )
                          }
                          title={`Xem / Truy cập: ${row.readPerm.name}`}
                          className="data-[state=checked]:bg-slate-900 data-[state=checked]:text-white dark:data-[state=checked]:bg-white dark:data-[state=checked]:text-slate-900 data-[state=checked]:border-slate-900 dark:data-[state=checked]:border-white size-4.5 rounded cursor-pointer"
                        />
                      ) : (
                        <span className="text-[10px] text-slate-300 dark:text-slate-600">
                          —
                        </span>
                      )}
                    </div>

                    {/* CREATE */}
                    <div className="flex items-center justify-center">
                      {row.createPerm ? (
                        <Checkbox
                          checked={selectedIds.has(Number(row.createPerm.id))}
                          onCheckedChange={() =>
                            handleToggle(
                              row.createPerm.id,
                              row.extraCreatePerms?.map((p: any) => p.id),
                            )
                          }
                          title={`Tạo mới: ${row.createPerm.name}`}
                          className="data-[state=checked]:bg-slate-900 data-[state=checked]:text-white dark:data-[state=checked]:bg-white dark:data-[state=checked]:text-slate-900 data-[state=checked]:border-slate-900 dark:data-[state=checked]:border-white size-4.5 rounded cursor-pointer"
                        />
                      ) : (
                        <span className="text-[10px] text-slate-300 dark:text-slate-600">
                          —
                        </span>
                      )}
                    </div>

                    {/* UPDATE */}
                    <div className="flex items-center justify-center">
                      {row.updatePerm ? (
                        <Checkbox
                          checked={selectedIds.has(Number(row.updatePerm.id))}
                          onCheckedChange={() =>
                            handleToggle(
                              row.updatePerm.id,
                              [
                                ...(row.extraUpdatePerms || []),
                                ...(row.otherPerms || []),
                              ].map((p: any) => p.id),
                            )
                          }
                          title={`Chỉnh sửa: ${row.updatePerm.name}`}
                          className="data-[state=checked]:bg-slate-900 data-[state=checked]:text-white dark:data-[state=checked]:bg-white dark:data-[state=checked]:text-slate-900 data-[state=checked]:border-slate-900 dark:data-[state=checked]:border-white size-4.5 rounded cursor-pointer"
                        />
                      ) : (
                        <span className="text-[10px] text-slate-300 dark:text-slate-600">
                          —
                        </span>
                      )}
                    </div>

                    {/* DELETE */}
                    <div className="flex items-center justify-center">
                      {row.deletePerm ? (
                        <Checkbox
                          checked={selectedIds.has(Number(row.deletePerm.id))}
                          onCheckedChange={() =>
                            handleToggle(
                              row.deletePerm.id,
                              row.extraDeletePerms?.map((p: any) => p.id),
                            )
                          }
                          title={`Xóa: ${row.deletePerm.name}`}
                          className="data-[state=checked]:bg-slate-900 data-[state=checked]:text-white dark:data-[state=checked]:bg-white dark:data-[state=checked]:text-slate-900 data-[state=checked]:border-slate-900 dark:data-[state=checked]:border-white size-4.5 rounded cursor-pointer"
                        />
                      ) : (
                        <span className="text-[10px] text-slate-300 dark:text-slate-600">
                          —
                        </span>
                      )}
                    </div>
                  </div>

                  {/* Toggle All Checkbox for this row */}
                  <div className="col-span-1 flex items-center justify-center min-w-0">
                    {modulePermIds.length > 0 ? (
                      <Checkbox
                        checked={isAllSelected}
                        onCheckedChange={() => handleToggleModuleAll(row)}
                        title="Chọn/Bỏ tất cả quyền của module này"
                        className="data-[state=checked]:bg-slate-900 data-[state=checked]:text-white dark:data-[state=checked]:bg-white dark:data-[state=checked]:text-slate-900 data-[state=checked]:border-slate-900 dark:data-[state=checked]:border-white size-4.5 rounded cursor-pointer"
                      />
                    ) : (
                      <span className="text-[10px] text-slate-300 dark:text-slate-600">
                        —
                      </span>
                    )}
                  </div>
                </div>
              );
            })
          )}
        </div>
      </div>
    </div>
  );
}

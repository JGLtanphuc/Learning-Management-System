import type { RootState } from "@/store/store";
import { normalizePermission, resolveEffectivePermissions, uniquePermissions } from "@/utils/rbac.utils";
import { useMemo } from "react";
import { useSelector } from "react-redux";
import { useRoleSwitch } from "@/contexts/RoleSwitchContext";
import { useQuery } from "@tanstack/react-query";
import { permissionApi, roleApi } from "@/api";

export const usePermissions = () => {
  const { permissions, role, roles, rolePermissions, email, firstName, lastName, isAuthenticated } = useSelector(
    (state: RootState) => state.auth,
  );
  const { activeRole, viewRole, availableRoles, isPreviewMode } = useRoleSwitch();

  // Truy vấn trực tiếp phân quyền của tất cả Role từ CSDL theo thời gian thực
  const { data: dbRolePermissions } = useQuery({
    queryKey: ["rbac-roles-permissions"],
    enabled: isAuthenticated,
    queryFn: async () => {
      const [rolePage, permPage] = await Promise.all([
        roleApi.getPage({ page: 0, size: 9999 }),
        permissionApi.getPage({ page: 0, size: 9999 }),
      ]);
      const rolesList = rolePage.content ?? rolePage.items ?? [];
      const permList = permPage.content ?? permPage.items ?? [];
      const permMap = new Map<string, string>();
      permList.forEach((p) => {
        if (p.id != null && p.name) permMap.set(String(p.id), String(p.name));
      });

      const map: Record<string, string[]> = {};
      rolesList.forEach((r) => {
        const roleName = (r.name || "").toUpperCase();
        const pNames = (r.permissionIds || [])
          .map((id: any) => permMap.get(id?.toString()))
          .filter(Boolean) as string[];
        map[roleName] = pNames;
      });
      return map;
    },
    staleTime: 5 * 1000,
  });

  const currentRole = (activeRole || role || "").toUpperCase();

  const effectivePermissions = useMemo(() => {
    if (dbRolePermissions && dbRolePermissions[currentRole] !== undefined) {
      if (currentRole === "ADMIN") {
        return uniquePermissions([...dbRolePermissions[currentRole], ...permissions]);
      }
      return uniquePermissions(dbRolePermissions[currentRole]);
    }
    return resolveEffectivePermissions(activeRole, rolePermissions, permissions);
  }, [currentRole, dbRolePermissions, activeRole, rolePermissions, permissions]);

  const effectivePermissionSet = useMemo(
    () => new Set(effectivePermissions.map((permission) => normalizePermission(permission))),
    [effectivePermissions],
  );

  const user = {
    email,
    role,
    roles,
    firstName,
    lastName,
    permissions: effectivePermissions,
    rolePermissions: dbRolePermissions || rolePermissions,
  };

  const hasPermission = (permission: string): boolean =>
    effectivePermissionSet.has(normalizePermission(permission));

  const hasAnyPermission = (requiredPermissions: string[]): boolean =>
    requiredPermissions.some((permission) => hasPermission(permission));

  const hasAllPermissions = (requiredPermissions: string[]): boolean =>
    requiredPermissions.every((permission) => hasPermission(permission));

  const canAccessResource = (resource: string, action: string): boolean =>
    hasPermission(`${resource.toUpperCase()}_${action.toUpperCase()}`);

  return {
    user,
    activeRole,
    viewRole,
    availableRoles,
    isPreviewMode,
    effectivePermissions,
    hasPermission,
    hasAnyPermission,
    hasAllPermissions,
    canAccessResource,
  };
};

import React from "react";
import { Navigate } from "react-router-dom";

import { useSelector } from "react-redux";
import type { RootState } from "@/store/store";
import { usePermissions } from "@/hooks/usePermissions";
import { getHomePathByRole } from "@/utils/rbac.utils";

interface ProtectedRouteProps {
  children: React.ReactNode;
  requiredPermission?: string;
  requiredPermissions?: string[];
  requireAll?: boolean;
}

export const ProtectedRoute: React.FC<ProtectedRouteProps> = ({
  children,
  requiredPermission,
  requiredPermissions,
  requireAll = false,
}) => {
  const { isAuthenticated, isInitializing } = useSelector((state: RootState) => state.auth);
  const { hasPermission, hasAnyPermission, hasAllPermissions, activeRole, isPreviewMode } = usePermissions();

  if (isInitializing) {
    return null;
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  const isDenied =
    (requiredPermission && !hasPermission(requiredPermission)) ||
    (requiredPermissions?.length &&
      (requireAll ? !hasAllPermissions(requiredPermissions) : !hasAnyPermission(requiredPermissions)));

  if (isDenied) {
    // Nếu người dùng đang xem trước vai trò khác (Preview Mode) mà trang hiện tại không có quyền truy cập
    // (ví dụ đang ở /roles mà bấm xem Student Preview): tự động chuyển về trang chủ của Role đó (/student)
    if (isPreviewMode) {
      return <Navigate to={getHomePathByRole(activeRole)} replace />;
    }
    return <Navigate to="/unauthorized" replace />;
  }

  return <>{children}</>;
};

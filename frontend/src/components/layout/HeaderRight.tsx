import ToggleTheme from "@/components/ToggleTheme";
import { useEffect, useRef, useState } from "react";
import type { RootState } from "@/store/store";
import { useDispatch, useSelector } from "react-redux";
import { useRoleSwitch } from "@/contexts/RoleSwitchContext";
import { ChevronDown, LogOut } from "lucide-react";
import {
  formatRoleLabel,
  getHomePathByRole,
  normalizeRole,
  uniqueRoles,
} from "@/utils/rbac.utils";
import { useNavigate } from "react-router-dom";
import { authApi } from "@/api/features/auth.api";
import { setLogout } from "@/store/slices/auth/authSlice";

export default function HeaderRight() {
  const { role, roles } = useSelector((state: RootState) => state.auth);
  const { activeRole, availableRoles, setViewRole } = useRoleSwitch();
  const [open, setOpen] = useState(false);
  const dropdownRef = useRef<HTMLDivElement>(null);
  const navigate = useNavigate();

  const primaryRole = normalizeRole(role);
  const assignedRoles = uniqueRoles(roles, primaryRole);
  const canSwitchRole = availableRoles.length > 1;
  const currentRole = activeRole ?? primaryRole;
  const isStudentRole = currentRole === "STUDENT";

  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      if (
        dropdownRef.current &&
        !dropdownRef.current.contains(e.target as Node)
      ) {
        setOpen(false);
      }
    };
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  const dispatch = useDispatch();

  const handleLogout = async () => {
    try {
      await authApi.logout();
    } finally {
      dispatch(setLogout());
      navigate("/login", { replace: true });
    }
  };

  if (!role) {
    return (
      <div className="flex items-center gap-1.5 sm:gap-3">
        <ToggleTheme />
      </div>
    );
  }

  if (!canSwitchRole) {
    return (
      <div className="flex items-center gap-1.5 sm:gap-3">
        <button
          type="button"
          onClick={() => navigate(getHomePathByRole(currentRole))}
          title="Quay về trang chủ"
          className="flex items-center text-xs sm:text-sm font-medium text-blue-700 bg-blue-50 hover:bg-blue-100 border border-blue-200 px-2.5 sm:px-3 py-1 sm:py-1.5 rounded-full transition-colors cursor-pointer"
        >
          <span>{formatRoleLabel(currentRole)}</span>
        </button>

        <ToggleTheme />

        <button
          onClick={handleLogout}
          title="Đăng xuất"
          className="flex items-center justify-center size-8 sm:size-9 rounded-lg border border-gray-200 dark:border-gray-700 hover:bg-red-50 dark:hover:bg-red-950/30 hover:border-red-200 text-gray-600 dark:text-gray-300 hover:text-red-600 transition-colors cursor-pointer"
        >
          <LogOut size={15} />
        </button>
      </div>
    );
  }

  const switchRole = (nextRole: string) => {
    const normalizedRole = normalizeRole(nextRole);
    setViewRole(normalizedRole === primaryRole ? null : normalizedRole);
    setOpen(false);
    navigate(getHomePathByRole(normalizedRole));
  };

  return (
    <div className="flex items-center gap-1.5 sm:gap-2.5" ref={dropdownRef}>
      {/* Segmented Role Switcher Pills (như hình mẫu) */}
      <div className="hidden lg:inline-flex items-center bg-slate-100 dark:bg-slate-800/90 p-0.5 rounded-lg border border-slate-200/80 dark:border-slate-700/80 text-xs">
        {availableRoles.slice(0, 3).map((r) => {
          const isSelected = normalizeRole(r) === currentRole;
          const isPreview = !assignedRoles.includes(normalizeRole(r));
          return (
            <button
              key={r}
              type="button"
              onClick={() => switchRole(r)}
              className={`px-2.5 py-1 rounded-md text-xs font-medium transition-all cursor-pointer ${
                isSelected
                  ? "bg-white dark:bg-slate-700 text-indigo-700 dark:text-indigo-300 shadow-2xs font-semibold"
                  : "text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white"
              }`}
            >
              {formatRoleLabel(r)}{isPreview ? " (Preview)" : ""}
            </button>
          );
        })}
      </div>

      {/* Dropdown fallback trên màn hình nhỏ */}
      <div className="relative lg:hidden">
        <div
          className={`inline-flex items-center rounded-full text-xs font-medium border transition-colors ${
            isStudentRole
              ? "bg-green-50 text-green-700 border-green-200"
              : "bg-blue-50 text-blue-700 border-blue-200"
          }`}
        >
          <button
            type="button"
            onClick={() => navigate(getHomePathByRole(currentRole))}
            title="Quay về trang chủ"
            className="px-2.5 py-1 hover:underline cursor-pointer truncate max-w-[100px]"
          >
            {formatRoleLabel(currentRole)}
          </button>

          <button
            type="button"
            onClick={() => setOpen((o) => !o)}
            title="Đổi vai trò"
            className="pr-2 pl-0.5 py-1 hover:bg-black/5 dark:hover:bg-white/5 rounded-r-full cursor-pointer flex items-center justify-center"
          >
            <ChevronDown
              size={12}
              className={`shrink-0 transition-transform duration-200 ${open ? "rotate-180" : ""}`}
            />
          </button>
        </div>

        {open && (
          <div className="absolute right-0 top-full mt-1.5 w-44 bg-white border border-gray-200 rounded-lg shadow-lg z-50 overflow-hidden">
            {availableRoles.map((availableRole) => {
              const selected = normalizeRole(availableRole) === currentRole;
              const studentRow = normalizeRole(availableRole) === "STUDENT";

              return (
                <button
                  key={availableRole}
                  onClick={() => switchRole(availableRole)}
                  className={`w-full flex items-center gap-2 px-3 py-2 text-xs hover:bg-gray-50 transition-colors cursor-pointer ${
                    selected
                      ? studentRow
                        ? "text-green-700 font-medium bg-green-50"
                        : "text-blue-700 font-medium bg-blue-50"
                      : "text-gray-700"
                  }`}
                >
                  <span>{formatRoleLabel(availableRole)}</span>
                  {selected && (
                    <span className="ml-auto text-xs text-blue-500">●</span>
                  )}
                </button>
              );
            })}
          </div>
        )}
      </div>

      {/* Theme Toggle */}
      <ToggleTheme />

      {/* Icon Đăng Xuất */}
      <button
        onClick={handleLogout}
        title="Đăng xuất"
        className="flex items-center justify-center size-8 sm:size-9 rounded-lg border border-gray-200 dark:border-gray-700 hover:bg-red-50 dark:hover:bg-red-950/30 hover:border-red-200 text-gray-600 dark:text-gray-300 hover:text-red-600 transition-colors cursor-pointer"
      >
        <LogOut size={15} />
      </button>
    </div>
  );
}

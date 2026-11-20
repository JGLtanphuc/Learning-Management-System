import { Sidebar, SidebarContent, SidebarFooter, SidebarHeader, useSidebar } from "@/components/ui/sidebar";

import { NavMain } from "@/components/layout/NavMain";

import { TooltipWrapper } from "@/components/datatable/common/TooltipWrapper";
import { Button } from "@/components/ui/button";
import { GraduationCap, LogOutIcon } from "lucide-react";
import { useMemo } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { authApi } from "@/api/features/auth.api";
import { useActiveModuleGroups } from "@/hooks/useSidebarMenus";
import { iconMap } from "@/components/datatable/iconMap";
import { usePermissions } from "@/hooks/usePermissions";
import { normalizeRole, TEACHER_ROLE } from "@/utils/rbac.utils";

export function SidebarMenu() {
    const location = useLocation();
    const navigate = useNavigate();
    const { toggleSidebar, state } = useSidebar();
    const { data: moduleGroups = [] } = useActiveModuleGroups();
    const { activeRole, user } = usePermissions();

    const isTeacherView = normalizeRole(activeRole) === TEACHER_ROLE;
    const portalSubtitle = isTeacherView ? "Teacher Portal" : "Executive Operations";

    const displayName =
        [user?.firstName, user?.lastName].filter(Boolean).join(" ") ||
        (isTeacherView ? "Giảng viên" : "Admin Quản trị");

    const userInitials =
        displayName
            .split(" ")
            .filter(Boolean)
            .map((w) => w[0])
            .slice(-2)
            .join("")
            .toUpperCase() || "AD";

    const navGroups = useMemo(() => {
        const resolveIcon = (iconName?: string) => {
            const key: keyof typeof iconMap =
                iconName && iconName in iconMap ? (iconName as keyof typeof iconMap) : "menu";

            return iconMap[key];
        };

        return moduleGroups
            .map((group) => ({
                id: group.id ?? group.name ?? "group",
                name: group.name ?? "Menu",
                items: group.modules
                    .filter((module) => !!module.url)
                    .map((module) => ({
                        title: module.title ?? "Untitled",
                        url: module.url ?? "#",
                        icon: resolveIcon(module.icon),
                        isActive:
                            !!module.url &&
                            (location.pathname === module.url || location.pathname.startsWith(`${module.url}/`)),
                    })),
            }))
            .filter((group) => group.items.length > 0);
    }, [location.pathname, moduleGroups]);

    return (
        <Sidebar variant="inset" collapsible="icon">
            {/* Header */}
            <SidebarHeader className="py-3 px-0">
                <Button
                    type="button"
                    variant="ghost"
                    onClick={toggleSidebar}
                    className="
            flex h-auto w-full min-w-0 items-center justify-start gap-3
            group-data-[collapsible=icon]:justify-center
            group-data-[collapsible=icon]:px-0
          "
                >
                    <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-xl bg-[#4f46e5] text-white shadow-sm shadow-indigo-200 dark:shadow-none">
                        <GraduationCap className="size-5" />
                    </div>

                    {state === "expanded" && (
                        <div className="flex flex-col items-start text-left min-w-0">
                            <span className="text-base font-bold tracking-tight text-slate-900 dark:text-white leading-none truncate">
                                FAS Portal
                            </span>
                            <span className="text-[11px] text-slate-400 font-medium mt-1 truncate">
                                {portalSubtitle}
                            </span>
                        </div>
                    )}
                </Button>
            </SidebarHeader>

            {/* Content */}
            <SidebarContent>
                {navGroups.map((group) =>
                    group ? (
                        <NavMain key={group.id} title={group.name} items={group.items} sidebarState={state} />
                    ) : null,
                )}
            </SidebarContent>

            {/* Footer */}
            <SidebarFooter className="p-2.5 text-xs border-t border-slate-200/80 dark:border-slate-800">
                {state === "expanded" ? (
                    <div className="flex items-center justify-between gap-2 p-1.5 rounded-xl bg-slate-50 dark:bg-slate-800/60 border border-slate-200/70 dark:border-slate-800 w-full min-w-0">
                        <div className="flex items-center gap-2.5 min-w-0 overflow-hidden">
                            <div className="w-8 h-8 rounded-full bg-[#4f46e5] text-white font-bold flex items-center justify-center text-xs shrink-0 shadow-xs">
                                {userInitials}
                            </div>
                            <div className="flex flex-col min-w-0 overflow-hidden text-left">
                                <span className="text-xs font-semibold text-slate-800 dark:text-slate-200 truncate">
                                    {displayName}
                                </span>
                                <span className="text-[10px] text-slate-400 truncate">
                                    {user?.email || "admin.fas@fpt.com"}
                                </span>
                            </div>
                        </div>

                        <TooltipWrapper content="Đăng xuất">
                            <Button
                                size="icon"
                                variant="ghost"
                                className="size-8 shrink-0 text-slate-500 hover:text-rose-600 hover:bg-rose-50 dark:hover:bg-rose-950/40 rounded-lg transition-colors cursor-pointer"
                                onClick={async () => {
                                    await authApi.logout();
                                    navigate("/login");
                                }}
                            >
                                <LogOutIcon className="size-4" />
                            </Button>
                        </TooltipWrapper>
                    </div>
                ) : (
                    <TooltipWrapper content="Đăng xuất">
                        <Button
                            size="icon"
                            variant="ghost"
                            className="size-9 text-slate-500 hover:text-rose-600 hover:bg-rose-50 dark:hover:bg-rose-950/40 rounded-lg transition-colors cursor-pointer"
                            onClick={async () => {
                                await authApi.logout();
                                navigate("/login");
                            }}
                        >
                            <LogOutIcon className="size-4" />
                        </Button>
                    </TooltipWrapper>
                )}
            </SidebarFooter>
        </Sidebar>
    );
}

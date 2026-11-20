import {
  SidebarInset,
  SidebarProvider,
  SidebarTrigger,
} from "@/components/ui/sidebar";
import { Separator } from "@/components/ui/separator";
import HeaderRight from "@/components/layout/HeaderRight";
import { SidebarMenu } from "@/components/layout/Sidebar.tsx";
import DynamicBreadcrumbs from "@/components/layout/DynamicBreadcrumbs.tsx";
import { PortalFooter } from "@/components/layout/PortalFooter";
import { usePermissions } from "@/hooks/usePermissions";
import { normalizeRole, STUDENT_ROLE } from "@/utils/rbac.utils";
import { GraduationCap, Search } from "lucide-react";
import { useMemo } from "react";
import { Link, useLocation } from "react-router-dom";
import { useActiveModuleGroups } from "@/hooks/useSidebarMenus";

const VIETNAMESE_MODULE_NAMES: Record<string, string> = {
  "/attendances": "Điểm danh",
  "/student-scores": "Bảng điểm",
  "/topic-marks/gradebook-demo": "Sổ điểm",
  "/mock-projects": "Đồ án",
  "/books": "Tài liệu",
  "/classes": "Lớp học",
  "/teachers": "Giáo viên",
  "/students": "Học viên",
  "/allocations": "Phân bổ BU",
  "/student/mock-interview": "Phỏng vấn",
};

export function MainLayout({
  children,
  pathName,
  showFooter = true,
}: {
  children: React.ReactNode;
  pathName?: Record<string, string>;
  showFooter?: boolean;
}) {
  const { activeRole } = usePermissions();
  const location = useLocation();
  const isStudentView = normalizeRole(activeRole) === STUDENT_ROLE;
  const { data: moduleGroups = [] } = useActiveModuleGroups(isStudentView);

  const dynamicStudentNavItems = useMemo(() => {
    const items: Array<{ label: string; path: string }> = [
      { label: "Trang chủ", path: "/student" },
    ];

    const allUserModules = moduleGroups.flatMap((g) => g.modules);

    // Lấy module phỏng vấn động từ cấu hình Database (được Admin thiết lập trong bảng modules)
    const interviewModule =
      allUserModules.find((m) => m.url === "/interview-assignments") ||
      allUserModules.find((m) => m.url === "/student/mock-interview") ||
      allUserModules.find((m) => m.url?.includes("interview"));

    allUserModules.forEach((m) => {
      if (
        !m.url ||
        m.url === "/dashboard" ||
        m.url === "/student" ||
        m.url === "/interview-topics" ||
        m.url === "/interview-questions" ||
        m.url === "/interview-answers" ||
        m.url === "/interview-assignments" ||
        m.url === "/student/mock-interview"
      ) return;

      // Ưu tiên tiêu đề m.title động từ database trước, fallback sang danh mục nếu chưa đặt
      const label = m.title || m.name || VIETNAMESE_MODULE_NAMES[m.url] || m.url;
      if (!items.some((i) => i.path === m.url)) {
        items.push({ label, path: m.url });
      }
    });

    // Menu phỏng vấn học viên: lấy động title từ database (VD: "Interview" do người dùng sửa)
    const interviewLabel =
      interviewModule?.title ||
      interviewModule?.name ||
      VIETNAMESE_MODULE_NAMES["/student/mock-interview"] ||
      "Phỏng vấn";

    items.push({ label: interviewLabel, path: "/student/mock-interview" });
    return items;
  }, [moduleGroups]);

  if (isStudentView) {
    return (
      <div className="h-screen flex flex-col overflow-hidden max-h-[100dvh] bg-[#f8fafc] dark:bg-slate-950">
        <header className="sticky top-0 z-50 shrink-0 bg-white dark:bg-slate-900 border-b border-slate-200 dark:border-slate-800 shadow-xs">
          <div className="max-w-[1440px] mx-auto flex justify-between items-center w-full px-4 sm:px-6 h-16">
            {/* Left side: Brand Logo + Portal Label + Breadcrumb + Navigation Links */}
            <div className="flex items-center gap-4 sm:gap-6 min-w-0 flex-1 mr-2">
              <Link
                to="/student"
                className="flex items-center gap-2 shrink-0 hover:opacity-90 transition-opacity"
              >
                <div className="w-9 h-9 rounded-lg bg-[#4f46e5] flex items-center justify-center text-white shadow-xs">
                  <GraduationCap className="size-5" />
                </div>
                <div className="flex flex-col">
                  <span className="text-base font-bold tracking-tight text-slate-900 dark:text-white leading-none">
                    FAS Portal
                  </span>
                  <span className="text-[10px] text-slate-400 font-medium mt-0.5">
                    Student Portal
                  </span>
                </div>
              </Link>

              <div className="hidden lg:flex items-center min-w-0 shrink-0">
                <DynamicBreadcrumbs pathTitles={pathName} variant="student" />
              </div>

              <nav
                aria-label="Main Navigation"
                className="hidden md:flex items-center gap-4 lg:gap-6 ml-2 overflow-x-auto no-scrollbar"
              >
                {dynamicStudentNavItems.map((item) => {
                  const isActive =
                    item.path === "/student"
                      ? location.pathname === "/student"
                      : location.pathname.startsWith(item.path);
                  return (
                    <Link
                      key={item.path}
                      to={item.path}
                      className={`text-sm transition-colors whitespace-nowrap pb-1 inline-flex items-center ${
                        isActive
                          ? "text-[#3525cd] dark:text-indigo-400 border-b-2 border-[#3525cd] dark:border-indigo-400 font-semibold"
                          : "text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white font-normal"
                      }`}
                    >
                      {item.label}
                    </Link>
                  );
                })}
              </nav>
            </div>

            {/* Right side: Trailing Actions */}
            <div className="shrink-0">
              <HeaderRight />
            </div>
          </div>
        </header>

        <main className="flex-1 overflow-y-auto min-h-0 flex flex-col">
          <div className="flex-1 max-w-[1440px] w-full mx-auto px-4 md:px-8 pt-6 pb-2 sm:pb-3 flex flex-col justify-between">
            <div className="flex-1">
              {children}
            </div>
            {showFooter && (
              <div className="mt-auto pt-8">
                <PortalFooter />
              </div>
            )}
          </div>
        </main>
      </div>
    );
  }

  return (
    <SidebarProvider>
      <SidebarMenu />
      <SidebarInset className="h-screen flex flex-col overflow-hidden max-h-[100dvh] w-full min-w-0 max-w-full">
        <header className="flex h-14 sm:h-16 shrink-0 items-center border-b px-2 sm:px-4 w-full min-w-0">
          <div className="flex items-center gap-1.5 sm:gap-2 w-full justify-between min-w-0">
            <div className="flex items-center gap-1 sm:gap-2 min-w-0 flex-1 overflow-hidden mr-2">
              <SidebarTrigger className="-ml-1 shrink-0" />
              <Separator
                orientation="vertical"
                className="mr-1 h-4 hidden sm:block shrink-0"
              />
              <div className="min-w-0 truncate">
                <DynamicBreadcrumbs pathTitles={pathName} variant="default" />
              </div>
            </div>

            {/* Quick Search Input matching Stitch design */}
            <div className="hidden xl:flex items-center flex-1 max-w-xs mx-4">
              <div className="relative w-full">
                <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 size-3.5 text-slate-400" />
                <input
                  type="text"
                  placeholder="Tìm kiếm học viên, mã lớp, BU"
                  className="w-full pl-8 pr-3 py-1.5 text-xs bg-slate-50 dark:bg-slate-800/80 border border-slate-200/90 dark:border-slate-700/80 rounded-lg focus:outline-none focus:ring-1 focus:ring-indigo-500 text-slate-800 dark:text-slate-200 placeholder:text-slate-400 transition-colors"
                />
              </div>
            </div>

            <div className="shrink-0">
              <HeaderRight />
            </div>
          </div>
        </header>

        <main className="flex-1 overflow-y-auto overflow-x-hidden min-h-0 flex flex-col px-3 pb-3 pt-3 sm:px-6 sm:pb-3 sm:pt-6 w-full min-w-0 max-w-full">
          <div className="flex-1 flex flex-col justify-between w-full min-h-full">
            <div className="flex-1">
              {children}
            </div>
            {showFooter && (
              <div className="mt-auto pt-8">
                <PortalFooter />
              </div>
            )}
          </div>
        </main>
      </SidebarInset>
    </SidebarProvider>
  );
}

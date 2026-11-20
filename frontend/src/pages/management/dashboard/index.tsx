import React, { useEffect, useMemo, useState } from "react";
import {
  ChevronLeft,
  ChevronRight,
  Eye,
  GraduationCap,
  Kanban,
  LogOut,
  Plus,
  RefreshCw,
  Search,
  Share2,
  TrendingUp,
  Users,
} from "lucide-react";
import { Link, useNavigate } from "react-router-dom";
import { useDispatch } from "react-redux";
import { dashboardApi, type DashboardStats } from "../../../api/features/dashboard.api";
import { classApi } from "@/api/features/fams/class.api";
import type { ClassDTO } from "@/types/features/fams/class.types";
import { MainLayout } from "../../../components/layout/MainLayout";
import { authApi } from "@/api/features/auth.api";
import { setLogout } from "@/store/slices/auth/authSlice";
import { usePermissions } from "@/hooks/usePermissions";
import { normalizeRole, TEACHER_ROLE } from "@/utils/rbac.utils";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";

interface CohortRow {
  id?: number;
  code: string;
  name: string;
  tech: string;
  type: "java" | "dotnet" | "cloud" | "ai";
  mentor: string;
  enrolled: string;
  sprint: string;
  progress: number;
  attendance: string;
  mockProject: string;
  targetBu: string;
  status: "active" | "finishing" | "mock";
  statusLabel: string;
}

export const Dashboard: React.FC = () => {
  const { activeRole, user, hasPermission } = usePermissions();
  const isTeacher = normalizeRole(activeRole) === TEACHER_ROLE;
  const canCreateClass = hasPermission("CLASS_CREATE");

  const [stats, setStats] = useState<DashboardStats | null>(null);
  const [classes, setClasses] = useState<ClassDTO[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [tableFilter, setTableFilter] = useState<"all" | "active" | "finishing" | "mock">("all");
  const [searchQuery, setSearchQuery] = useState("");
  const [showLogoutDialog, setShowLogoutDialog] = useState(false);
  const [currentPage, setCurrentPage] = useState(1);

  const navigate = useNavigate();
  const dispatch = useDispatch();

  useEffect(() => {
    loadDashboardData();
  }, [activeRole]);

  const loadDashboardData = async () => {
    setIsLoading(true);
    try {
      const [statsResult, classesResult] = await Promise.allSettled([
        dashboardApi.getStats(),
        classApi.getPage({ page: 0, size: 50 }),
      ]);
      if (statsResult.status === "fulfilled") {
        setStats(statsResult.value);
      }
      if (classesResult.status === "fulfilled") {
        setClasses(classesResult.value.content ?? (classesResult.value as any).items ?? []);
      } else {
        setClasses([]);
      }
    } catch (error) {
      console.error("Error loading dashboard data:", error);
    } finally {
      setIsLoading(false);
    }
  };

  const handleLogout = async () => {
    try {
      await authApi.logout();
    } finally {
      dispatch(setLogout());
      navigate("/login", { replace: true });
    }
  };

  const cohorts: CohortRow[] = useMemo(() => {
    return classes.map((c) => {
      const pName = c.programName || "";
      const lowerCode = (c.classCode || "").toLowerCase();
      const lowerProg = pName.toLowerCase();

      let type: CohortRow["type"] = "dotnet";
      if (lowerCode.includes("java") || lowerProg.includes("java")) {
        type = "java";
      } else if (lowerCode.includes("cloud") || lowerProg.includes("devops") || lowerProg.includes("aws")) {
        type = "cloud";
      } else if (lowerCode.includes("ai") || lowerProg.includes("ai") || lowerProg.includes("data")) {
        type = "ai";
      }

      let progress = 50;
      if (c.status === "COMPLETED") {
        progress = 100;
      } else if (c.status === "PLANNING") {
        progress = 15;
      } else if (c.startDate && c.endDate) {
        const start = new Date(c.startDate).getTime();
        const end = new Date(c.endDate).getTime();
        const now = Date.now();
        if (end > start) {
          progress = Math.min(100, Math.max(10, Math.round(((now - start) / (end - start)) * 100)));
        }
      }

      const sprint =
        progress >= 75
          ? "Sprint 4"
          : progress >= 50
          ? "Sprint 3"
          : progress >= 25
          ? "Sprint 2"
          : "Sprint 1";

      let status: "active" | "finishing" | "mock" = "active";
      let statusLabel = "Đang đào tạo";
      if (c.status === "COMPLETED") {
        status = "finishing";
        statusLabel = "Hoàn thành";
      } else if (c.status === "PLANNING") {
        status = "mock";
        statusLabel = "Lên kế hoạch";
      }

      const mentor =
        (c as any).teacherName ||
        (isTeacher
          ? [user.firstName, user.lastName].filter(Boolean).join(" ") || "Giảng viên phụ trách"
          : "Chưa phân công");

      return {
        id: c.id,
        code: c.classCode,
        name: c.className,
        tech: pName || "Fresher Training",
        type,
        mentor,
        enrolled: `${c.totalStudents ?? 0}/${c.capacity ?? 25}`,
        sprint,
        progress,
        attendance: `${(c.id != null && stats?.classAttendanceRates && stats.classAttendanceRates[c.id] != null) ? stats.classAttendanceRates[c.id] : (stats?.averageAttendanceRate ?? 100)}%`,
        mockProject: pName || "Đồ án Mock Project",
        targetBu: `${c.room ? `Phòng: ${c.room}` : "Lab"} • ${c.location || "FAS"}`,
        status,
        statusLabel,
      };
    });
  }, [classes, isTeacher, user]);

  const counts = useMemo(() => {
    return {
      all: cohorts.length,
      active: cohorts.filter((c) => c.status === "active").length,
      mock: cohorts.filter((c) => c.status === "mock").length,
      finishing: cohorts.filter((c) => c.status === "finishing").length,
    };
  }, [cohorts]);

  const filteredCohorts = useMemo(() => {
    return cohorts.filter((item) => {
      const matchesFilter = tableFilter === "all" || item.status === tableFilter;
      const q = searchQuery.toLowerCase().trim();
      const matchesSearch =
        !q ||
        item.code.toLowerCase().includes(q) ||
        item.name.toLowerCase().includes(q) ||
        item.mentor.toLowerCase().includes(q) ||
        item.tech.toLowerCase().includes(q) ||
        item.mockProject.toLowerCase().includes(q) ||
        item.targetBu.toLowerCase().includes(q);
      return matchesFilter && matchesSearch;
    });
  }, [cohorts, tableFilter, searchQuery]);

  const totalClassesCount = stats?.totalClasses ?? cohorts.length;
  const totalStudentsCount = stats?.totalStudents ?? 0;
  const activeProjectsCount = stats?.activeMockProjects ?? 0;
  const totalAllocations = stats?.totalAllocations ?? 0;

  const allocationPercent =
    totalStudentsCount > 0
      ? Math.min(100, Math.round((totalAllocations / totalStudentsCount) * 100))
      : 0;

  return (
    <MainLayout>
      <div className="space-y-6 w-full">
        {/* ============================================================== */}
        {/* 1. 4 EXECUTIVE KPI STAT CARDS (Hàng trên cùng chuẩn Stitch)     */}
        {/* ============================================================== */}
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 lg:gap-5 w-full">
          {/* Card 1: Lớp Đào Tạo Đang Mở / Lớp Phụ Trách */}
          <div className="bg-white dark:bg-slate-900 rounded-2xl p-5 border border-slate-200/90 dark:border-slate-800 shadow-2xs hover:shadow-xs transition-shadow flex flex-col justify-between">
            <div>
              <div className="flex items-center justify-between mb-3">
                <span className="text-xs font-semibold text-slate-600 dark:text-slate-400">
                  {isTeacher ? "Lớp Đào Tạo Phụ Trách" : "Lớp Đào Tạo Đang Mở"}
                </span>
                <div className="size-8 rounded-lg bg-purple-50 dark:bg-purple-950/60 text-purple-600 dark:text-purple-400 flex items-center justify-center">
                  <Users size={16} />
                </div>
              </div>

              <div className="flex items-baseline gap-2 mb-4">
                <span className="text-3xl font-bold tracking-tight text-slate-900 dark:text-white">
                  {isLoading ? "..." : totalClassesCount}
                </span>
                <span className="text-base font-semibold text-slate-900 dark:text-white">Lớp</span>
              </div>
            </div>

            <div>
              <div className="flex items-center justify-between text-xs mb-1.5">
                <span className="text-slate-500 dark:text-slate-400">Tỷ lệ On-track</span>
                <span className="font-bold text-emerald-600 dark:text-emerald-400">
                  {isLoading ? "..." : `${stats?.onTrackRate ?? 100}%`}
                </span>
              </div>
              <div className="h-1.5 w-full bg-slate-100 dark:bg-slate-800 rounded-full overflow-hidden">
                <div
                  className="h-full bg-emerald-500 rounded-full transition-all duration-500"
                  style={{ width: `${stats?.onTrackRate ?? 100}%` }}
                />
              </div>
            </div>
          </div>

          {/* Card 2: Tổng Số Fresher Đang Đào Tạo / Fresher Phụ Trách */}
          <div className="bg-white dark:bg-slate-900 rounded-2xl p-5 border border-slate-200/90 dark:border-slate-800 shadow-2xs hover:shadow-xs transition-shadow flex flex-col justify-between">
            <div>
              <div className="flex items-center justify-between mb-3">
                <span className="text-xs font-semibold text-slate-600 dark:text-slate-400">
                  {isTeacher ? "Fresher Lớp Phụ Trách" : "Tổng Số Fresher Đang Đào Tạo"}
                </span>
                <div className="size-8 rounded-lg bg-blue-50 dark:bg-blue-950/60 text-blue-600 dark:text-blue-400 flex items-center justify-center">
                  <GraduationCap size={16} />
                </div>
              </div>

              <div className="flex items-baseline gap-2 mb-4">
                <span className="text-3xl font-bold tracking-tight text-slate-900 dark:text-white">
                  {isLoading ? "..." : totalStudentsCount}
                </span>
                <span className="text-base font-semibold text-slate-900 dark:text-white">Học Viên</span>
              </div>
            </div>

            <div>
              <div className="flex items-center justify-between text-xs mb-1.5">
                <span className="text-slate-500 dark:text-slate-400">Chuyên cần trung bình</span>
                <span className="font-bold text-sky-600 dark:text-sky-400">
                  {isLoading ? "..." : `${stats?.averageAttendanceRate ?? 100}%`}
                </span>
              </div>
              <div className="h-1.5 w-full bg-slate-100 dark:bg-slate-800 rounded-full overflow-hidden">
                <div
                  className="h-full bg-sky-500 rounded-full transition-all duration-500"
                  style={{ width: `${stats?.averageAttendanceRate ?? 100}%` }}
                />
              </div>
            </div>
          </div>

          {/* Card 3: Đồ Án Mock Project */}
          <div className="bg-white dark:bg-slate-900 rounded-2xl p-5 border border-slate-200/90 dark:border-slate-800 shadow-2xs hover:shadow-xs transition-shadow flex flex-col justify-between">
            <div>
              <div className="flex items-center justify-between mb-3">
                <span className="text-xs font-semibold text-slate-600 dark:text-slate-400">
                  {isTeacher ? "Đồ Án Mock Project Phụ Trách" : "Đồ Án Mock Project"}
                </span>
                <div className="size-8 rounded-lg bg-amber-50 dark:bg-amber-950/60 text-amber-600 dark:text-amber-400 flex items-center justify-center">
                  <Kanban size={16} />
                </div>
              </div>

              <div className="flex items-baseline gap-2 mb-4">
                <span className="text-3xl font-bold tracking-tight text-slate-900 dark:text-white">
                  {isLoading ? "..." : activeProjectsCount}
                </span>
                <span className="text-base font-semibold text-slate-900 dark:text-white">Đồ Án</span>
              </div>
            </div>

            <div className="pt-2 text-xs text-slate-400 dark:text-slate-500">
              {activeProjectsCount > 0
                ? `${activeProjectsCount} Sprint active • Đang bảo vệ giai đoạn`
                : "0 Sprint active • Chưa có đồ án"}
            </div>
          </div>

          {/* Card 4: Tỷ Lệ Phân Bổ Về BU Sản Xuất */}
          <div className="bg-white dark:bg-slate-900 rounded-2xl p-5 border border-slate-200/90 dark:border-slate-800 shadow-2xs hover:shadow-xs transition-shadow flex flex-col justify-between">
            <div>
              <div className="flex items-center justify-between mb-3">
                <span className="text-xs font-semibold text-slate-600 dark:text-slate-400">
                  {isTeacher ? "Tỷ Lệ Nhận BU Sản Xuất" : "Tỷ Lệ Phân Bổ Về BU Sản Xuất"}
                </span>
                <div className="size-8 rounded-lg bg-emerald-50 dark:bg-emerald-950/60 text-emerald-600 dark:text-emerald-400 flex items-center justify-center">
                  <TrendingUp size={16} />
                </div>
              </div>

              <div className="flex items-baseline gap-2 mb-4">
                <span className="text-3xl font-bold tracking-tight text-slate-900 dark:text-white">
                  {isLoading ? "..." : `${allocationPercent}%`}
                </span>
              </div>
            </div>

            <div className="pt-2 text-xs text-emerald-600 dark:text-emerald-400 font-medium">
              {isLoading
                ? "..."
                : `${totalAllocations} / ${totalStudentsCount} Fresher ${isTeacher ? "lớp phụ trách " : ""}đã tiếp nhận BU`}
            </div>
          </div>
        </div>

        {/* ============================================================== */}
        {/* 2. BẢNG VẬN HÀNH HIỂN THỊ HẾT PHẦN CÒN LẠI (FULL WIDTH)        */}
        {/* ============================================================== */}
        <div className="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200/90 dark:border-slate-800 shadow-2xs overflow-hidden w-full">
          {/* Toolbar & Filter Header */}
          <div className="p-4 sm:p-5 border-b border-slate-100 dark:border-slate-800 flex flex-col sm:flex-row sm:items-center justify-between gap-4">
            {/* Filter Tabs */}
            <div className="flex flex-wrap items-center gap-1.5 p-1 bg-slate-100 dark:bg-slate-800/80 rounded-xl text-xs">
              <button
                onClick={() => setTableFilter("all")}
                className={`px-3 py-1.5 rounded-lg font-medium transition-colors cursor-pointer ${
                  tableFilter === "all"
                    ? "bg-white dark:bg-slate-700 text-indigo-700 dark:text-indigo-300 shadow-2xs font-semibold"
                    : "text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white"
                }`}
              >
                Tất cả ({counts.all})
              </button>
              <button
                onClick={() => setTableFilter("active")}
                className={`px-3 py-1.5 rounded-lg font-medium transition-colors cursor-pointer ${
                  tableFilter === "active"
                    ? "bg-white dark:bg-slate-700 text-indigo-700 dark:text-indigo-300 shadow-2xs font-semibold"
                    : "text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white"
                }`}
              >
                Đang học ({counts.active})
              </button>
              <button
                onClick={() => setTableFilter("mock")}
                className={`px-3 py-1.5 rounded-lg font-medium transition-colors cursor-pointer ${
                  tableFilter === "mock"
                    ? "bg-white dark:bg-slate-700 text-indigo-700 dark:text-indigo-300 shadow-2xs font-semibold"
                    : "text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white"
                }`}
              >
                Mock Project ({counts.mock})
              </button>
              <button
                onClick={() => setTableFilter("finishing")}
                className={`px-3 py-1.5 rounded-lg font-medium transition-colors cursor-pointer ${
                  tableFilter === "finishing"
                    ? "bg-white dark:bg-slate-700 text-indigo-700 dark:text-indigo-300 shadow-2xs font-semibold"
                    : "text-slate-600 dark:text-slate-400 hover:text-slate-900 dark:hover:text-white"
                }`}
              >
                Sắp tốt nghiệp ({counts.finishing})
              </button>
            </div>

            {/* Search Input & Action Buttons */}
            <div className="flex items-center gap-2.5">
              <div className="relative w-full sm:w-64">
                <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 size-3.5 text-slate-400" />
                <input
                  type="text"
                  placeholder="Lọc mã lớp, GV, dự án..."
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  className="w-full pl-8 pr-3 py-1.5 text-xs bg-slate-50 dark:bg-slate-800 border border-slate-200 dark:border-slate-700 rounded-lg focus:outline-none focus:ring-1 focus:ring-indigo-500 text-slate-800 dark:text-slate-200 placeholder:text-slate-400"
                />
              </div>

              <Button
                variant="outline"
                size="sm"
                onClick={() => loadDashboardData()}
                className="gap-1.5 text-slate-700 dark:text-slate-300 border-slate-200 dark:border-slate-700 h-8 text-xs shrink-0"
                title="Làm mới bảng"
              >
                <RefreshCw size={13} className={isLoading ? "animate-spin" : ""} />
                Làm mới
              </Button>

              {canCreateClass && (
                <Link to="/classes">
                  <Button size="sm" className="bg-[#4f46e5] hover:bg-indigo-700 text-white gap-1.5 shadow-xs h-8 text-xs shrink-0">
                    <Plus size={14} />
                    Mở Lớp Mới
                  </Button>
                </Link>
              )}
            </div>
          </div>

          {/* Live Operations Table (Full Width) */}
          <div className="overflow-x-auto w-full">
            <table className="w-full text-left text-xs border-collapse">
              <thead>
                <tr className="border-b border-slate-100 dark:border-slate-800/80 bg-slate-50/60 dark:bg-slate-800/40 text-slate-500 dark:text-slate-400 font-semibold uppercase tracking-wider text-[11px]">
                  <th className="py-3 px-4 sm:px-6">Lớp / Chuyên Ngành</th>
                  <th className="py-3 px-4">Giảng Viên Lead</th>
                  <th className="py-3 px-4 text-center">Sĩ Số</th>
                  <th className="py-3 px-4 min-w-[160px]">Tiến Độ Sprint</th>
                  <th className="py-3 px-4 text-center">Chuyên Cần</th>
                  <th className="py-3 px-4 min-w-[200px]">Mock Project & Doanh Nghiệp</th>
                  <th className="py-3 px-4 text-center">Trạng Thái</th>
                  <th className="py-3 px-4 sm:px-6 text-center">Thao Tác</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100 dark:divide-slate-800/60">
                {filteredCohorts.length === 0 ? (
                  <tr>
                    <td colSpan={8} className="py-12 text-center text-slate-500 dark:text-slate-400">
                      <div className="flex flex-col items-center justify-center gap-2">
                        <GraduationCap className="size-8 text-slate-300 dark:text-slate-600" />
                        <p className="text-sm font-medium">
                          {isLoading ? "Đang tải danh sách lớp học..." : "Không tìm thấy lớp học nào thuộc quyền phụ trách."}
                        </p>
                        <p className="text-xs text-slate-400">
                          {isTeacher
                            ? "Bạn hiện chỉ xem được các lớp được Quản trị viên phân công phụ trách."
                            : "Chưa có dữ liệu lớp học nào phù hợp với bộ lọc."}
                        </p>
                      </div>
                    </td>
                  </tr>
                ) : (
                  filteredCohorts.map((row) => (
                    <tr
                      key={row.code}
                      className="hover:bg-slate-50/80 dark:hover:bg-slate-800/50 transition-colors"
                    >
                      {/* 1. Lớp / Chuyên Ngành */}
                      <td className="py-3.5 px-4 sm:px-6">
                        <div>
                          <p className="font-bold text-slate-900 dark:text-white text-xs hover:text-indigo-600 transition-colors cursor-pointer">
                            {row.code}
                          </p>
                          <p className="text-[11px] text-slate-500 dark:text-slate-400 mt-0.5">
                            {row.name}
                          </p>
                        </div>
                      </td>

                      {/* 2. Giảng Viên Lead */}
                      <td className="py-3.5 px-4">
                        <p className="font-medium text-slate-800 dark:text-slate-200">
                          {row.mentor}
                        </p>
                      </td>

                      {/* 3. Sĩ Số */}
                      <td className="py-3.5 px-4 text-center">
                        <span className="font-bold text-indigo-600 dark:text-indigo-400 bg-indigo-50 dark:bg-indigo-950/60 px-2 py-0.5 rounded-full text-xs">
                          {row.enrolled}
                        </span>
                      </td>

                      {/* 4. Tiến Độ Sprint */}
                      <td className="py-3.5 px-4">
                        <div className="flex items-center justify-between text-[11px] mb-1">
                          <span className="font-medium text-slate-700 dark:text-slate-300">{row.sprint}</span>
                          <span className="font-bold text-indigo-600 dark:text-indigo-400">{row.progress}%</span>
                        </div>
                        <div className="h-1.5 w-full bg-slate-100 dark:bg-slate-800 rounded-full overflow-hidden">
                          <div
                            className="h-full bg-indigo-600 dark:bg-indigo-500 rounded-full"
                            style={{ width: `${row.progress}%` }}
                          />
                        </div>
                      </td>

                      {/* 5. Chuyên Cần */}
                      <td className="py-3.5 px-4 text-center">
                        <span className="font-semibold text-emerald-600 dark:text-emerald-400">
                          {row.attendance}
                        </span>
                      </td>

                      {/* 6. Mock Project & Doanh Nghiệp */}
                      <td className="py-3.5 px-4">
                        <p className="font-semibold text-slate-800 dark:text-slate-200 text-xs">
                          {row.mockProject}
                        </p>
                        <p className="text-[11px] text-slate-500 dark:text-slate-400 mt-0.5">
                          {row.targetBu}
                        </p>
                      </td>

                      {/* 7. Trạng Thái */}
                      <td className="py-3.5 px-4 text-center">
                        <span className={`inline-flex items-center px-2.5 py-1 rounded-full text-[11px] font-semibold border ${
                          row.status === "active"
                            ? "bg-emerald-50 text-emerald-700 border-emerald-200 dark:bg-emerald-950/60 dark:text-emerald-400 dark:border-emerald-800"
                            : row.status === "finishing"
                            ? "bg-blue-50 text-blue-700 border-blue-200 dark:bg-blue-950/60 dark:text-blue-400 dark:border-blue-800"
                            : "bg-purple-50 text-purple-700 border-purple-200 dark:bg-purple-950/60 dark:text-purple-400 dark:border-purple-800"
                        }`}>
                          {row.statusLabel}
                        </span>
                      </td>

                      {/* 8. Thao Tác (Chi tiết, Chấm điểm, Phân bổ) */}
                      <td className="py-3.5 px-4 sm:px-6 text-center">
                        <div className="flex items-center justify-center gap-1.5">
                          <Link to="/classes" title="Chi tiết lớp học">
                            <Button
                              variant="ghost"
                              size="icon"
                              className="size-7 rounded-lg text-slate-500 hover:text-indigo-600 hover:bg-indigo-50 dark:hover:bg-indigo-950/50"
                            >
                              <Eye size={14} />
                            </Button>
                          </Link>
                          <Link to="/student-scores" title="Sổ điểm & Đánh giá">
                            <Button
                              variant="ghost"
                              size="icon"
                              className="size-7 rounded-lg text-slate-500 hover:text-emerald-600 hover:bg-emerald-50 dark:hover:bg-emerald-950/50"
                            >
                              <GraduationCap size={14} />
                            </Button>
                          </Link>
                          {hasPermission("ALLOCATION_READ") && (
                            <Link to="/allocations" title="Phân bổ nhân lực BU">
                              <Button
                                variant="ghost"
                                size="icon"
                                className="size-7 rounded-lg text-slate-500 hover:text-amber-600 hover:bg-amber-50 dark:hover:bg-amber-950/50"
                              >
                                <Share2 size={14} />
                              </Button>
                            </Link>
                          )}
                        </div>
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>

          {/* Table Pagination Bar (Chuẩn bản Stitch: < 1 2 3 >) */}
          <div className="p-4 border-t border-slate-100 dark:border-slate-800 flex flex-col sm:flex-row items-center justify-between gap-3 text-xs text-slate-500 dark:text-slate-400">
            <div>
              Hiển thị <span className="font-semibold text-slate-700 dark:text-slate-300">{filteredCohorts.length > 0 ? 1 : 0} - {filteredCohorts.length}</span> trên tổng số{" "}
              <span className="font-semibold text-slate-700 dark:text-slate-300">{cohorts.length}</span> lớp học
              {isTeacher && (
                <span className="ml-1 text-slate-400 font-normal">
                  (lớp phụ trách)
                </span>
              )}
            </div>

            <div className="flex items-center gap-1.5">
              <button
                type="button"
                onClick={() => setCurrentPage(Math.max(1, currentPage - 1))}
                className="size-8 rounded-lg border border-slate-200 dark:border-slate-700 flex items-center justify-center hover:bg-slate-50 dark:hover:bg-slate-800 text-slate-600 dark:text-slate-400 transition-colors cursor-pointer"
                title="Trang trước"
              >
                <ChevronLeft size={14} />
              </button>

              {[1].map((page) => (
                <button
                  key={page}
                  type="button"
                  onClick={() => setCurrentPage(page)}
                  className={`size-8 rounded-lg font-semibold text-xs transition-colors cursor-pointer ${
                    currentPage === page
                      ? "bg-[#3525cd] text-white shadow-2xs"
                      : "border border-slate-200 dark:border-slate-700 hover:bg-slate-50 dark:hover:bg-slate-800 text-slate-700 dark:text-slate-300"
                  }`}
                >
                  {page}
                </button>
              ))}

              <button
                type="button"
                onClick={() => setCurrentPage(currentPage + 1)}
                className="size-8 rounded-lg border border-slate-200 dark:border-slate-700 flex items-center justify-center hover:bg-slate-50 dark:hover:bg-slate-800 text-slate-600 dark:text-slate-400 transition-colors cursor-pointer"
                title="Trang sau"
              >
                <ChevronRight size={14} />
              </button>
            </div>
          </div>
        </div>
      </div>

      {/* ============================================================== */}
      {/* 4. MODAL DIALOG XÁC NHẬN ĐĂNG XUẤT                              */}
      {/* ============================================================== */}
      <Dialog open={showLogoutDialog} onOpenChange={setShowLogoutDialog}>
        <DialogContent className="sm:max-w-[400px]">
          <DialogHeader>
            <div className="size-11 rounded-full bg-rose-50 text-rose-600 flex items-center justify-center mb-2">
              <LogOut size={20} />
            </div>
            <DialogTitle>Xác nhận đăng xuất</DialogTitle>
            <DialogDescription>
              Bạn có chắc chắn muốn đăng xuất khỏi Cổng Quản Trị Hệ Thống (FAS Portal)? Các phiên làm việc và bảo mật RBAC hiện tại sẽ được kết thúc an toàn.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter className="gap-2 sm:gap-0">
            <Button
              variant="outline"
              onClick={() => setShowLogoutDialog(false)}
            >
              Hủy bỏ
            </Button>
            <Button
              variant="destructive"
              onClick={handleLogout}
            >
              Đăng xuất
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </MainLayout>
  );
};

export default Dashboard;

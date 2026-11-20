import { MainLayout } from "@/components/layout/MainLayout";
import { usePermissions } from "@/hooks/usePermissions";
import { useActiveModuleGroups } from "@/hooks/useSidebarMenus";
import { formatRoleLabel } from "@/utils/rbac.utils";
import { motion } from "motion/react";
import {
  ArrowRight,
  Award,
  BookOpen,
  CalendarCheck,
  Kanban,
  Mic,
  Search,
} from "lucide-react";
import { useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { AcademicPolicyModal } from "@/components/layout/AcademicPolicyModal";
import { formatGreetingTitle, formatUserDisplayName } from "@/utils/user.utils";

type ModuleCategory = "ALL" | "TRAINING" | "ASSESSMENT" | "PROJECT" | "INTERVIEW" | "RESOURCE";

interface StudentModuleItem {
  key?: string;
  title: string;
  description: string;
  path: string;
  buttonText: string;
  icon: React.ComponentType<{ className?: string }>;
  category: ModuleCategory;
  iconBg: string;
  iconColor: string;
  hoverBorder: string;
  hoverTitle: string;
  isAlwaysEnabled?: boolean;
}


const studentModules: StudentModuleItem[] = [
  {
    key: "ATTENDANCE_READ",
    title: "Điểm danh chuyên cần",
    description:
      "Tra cứu lịch sử điểm danh và chuyên cần theo từng buổi học (sáng, chiều, cả ngày).",
    path: "/attendances",
    buttonText: "Vào điểm danh",
    icon: CalendarCheck,
    category: "TRAINING",
    iconBg: "bg-blue-50 dark:bg-blue-950/50",
    iconColor: "text-blue-600 dark:text-blue-400",
    hoverBorder: "hover:border-blue-300 dark:hover:border-blue-700",
    hoverTitle: "group-hover:text-blue-600 dark:group-hover:text-blue-400",
  },
  {
    key: "STUDENT_SCORE_READ",
    title: "Kết quả & Bảng điểm",
    description:
      "Tra cứu bảng điểm chi tiết các bài Quiz, Assignment, Lab thực hành và điểm tổng kết.",
    path: "/student-scores",
    buttonText: "Xem bảng điểm",
    icon: Award,
    category: "ASSESSMENT",
    iconBg: "bg-emerald-50 dark:bg-emerald-950/50",
    iconColor: "text-emerald-600 dark:text-emerald-400",
    hoverBorder: "hover:border-emerald-300 dark:hover:border-emerald-700",
    hoverTitle: "group-hover:text-emerald-600 dark:group-hover:text-emerald-400",
  },
  {
    key: "MOCK_PROJECT_READ",
    title: "Đồ án tốt nghiệp (Mock Project)",
    description:
      "Theo dõi tiến độ đề tài dự án thực tế, các mốc Sprint và điểm đánh giá đồ án tốt nghiệp.",
    path: "/mock-projects",
    buttonText: "Xem đồ án",
    icon: Kanban,
    category: "PROJECT",
    iconBg: "bg-amber-50 dark:bg-amber-950/50",
    iconColor: "text-amber-600 dark:text-amber-400",
    hoverBorder: "hover:border-amber-300 dark:hover:border-amber-700",
    hoverTitle: "group-hover:text-amber-600 dark:group-hover:text-amber-400",
  },
  {
    title: "Phỏng vấn thực hành theo chuyên môn",
    description:
      "Chọn lĩnh vực muốn phỏng vấn, xem bộ câu hỏi và trả lời bằng cách bật Mic nói hoặc gõ trực tiếp.",
    path: "/student/mock-interview",
    buttonText: "Bắt đầu phỏng vấn",
    icon: Mic,
    category: "INTERVIEW",
    iconBg: "bg-purple-50 dark:bg-purple-950/50",
    iconColor: "text-purple-600 dark:text-purple-400",
    hoverBorder: "hover:border-purple-300 dark:hover:border-purple-700",
    hoverTitle: "group-hover:text-purple-600 dark:group-hover:text-purple-400",
    isAlwaysEnabled: true,
  },
  {
    title: "Kho Giáo trình & Tài liệu Đào tạo",
    description:
      "Truy cập kho giáo trình kỹ thuật chuyên sâu Java, .NET, React, SQL & Clean Code chuẩn FPT Software Academy.",
    path: "#curriculum",
    buttonText: "Xem giáo trình",
    icon: BookOpen,
    category: "RESOURCE",
    iconBg: "bg-sky-50 dark:bg-sky-950/50",
    iconColor: "text-sky-600 dark:text-sky-400",
    hoverBorder: "hover:border-sky-300 dark:hover:border-sky-700",
    hoverTitle: "group-hover:text-sky-600 dark:group-hover:text-sky-400",
    isAlwaysEnabled: true,
  },
];

export default function StudentLandingPage() {
  const { user, activeRole, hasPermission } = usePermissions();
  const { data: moduleGroups = [] } = useActiveModuleGroups(true);
  const [selectedCategory, setSelectedCategory] = useState<ModuleCategory>("ALL");
  const [searchQuery, setSearchQuery] = useState("");
  const [showCurriculumModal, setShowCurriculumModal] = useState(false);

  const interviewModule = useMemo(() => {
    return moduleGroups
      .flatMap((g) => g.modules)
      .find(
        (m) =>
          m.url === "/interview-assignments" ||
          m.url === "/student/mock-interview" ||
          m.url?.includes("interview")
      );
  }, [moduleGroups]);

  const interviewTitle = interviewModule?.title || interviewModule?.name || "Phỏng vấn";

  const categoryTabs = useMemo(() => {
    return [
      { id: "ALL" as const, label: "Tất cả chức năng", count: 5 },
      { id: "TRAINING" as const, label: "Điểm danh", count: 1 },
      { id: "ASSESSMENT" as const, label: "Bảng điểm", count: 1 },
      { id: "PROJECT" as const, label: "Đồ án", count: 1 },
      { id: "INTERVIEW" as const, label: interviewTitle, count: 1 },
      { id: "RESOURCE" as const, label: "Tài liệu", count: 1 },
    ];
  }, [interviewTitle]);

  const displayName = useMemo(() => formatUserDisplayName(user, "Học viên"), [user]);
  const greetingTitle = useMemo(
    () => formatGreetingTitle(displayName, "STUDENT"),
    [displayName]
  );

  const dynamicStudentModules = useMemo(() => {
    return studentModules.map((item) => {
      if (item.category === "INTERVIEW") {
        return {
          ...item,
          title: interviewTitle !== "Phỏng vấn" ? `${interviewTitle} thực hành theo chuyên môn` : item.title,
          buttonText: interviewTitle !== "Phỏng vấn" ? `Bắt đầu ${interviewTitle}` : item.buttonText,
        };
      }
      return item;
    });
  }, [interviewTitle]);

  const filteredModules = useMemo(() => {
    return dynamicStudentModules.filter((item) => {
      // Chỉ hiển thị module nếu Học viên có quyền READ (hoặc là tính năng luôn mở như Phỏng vấn)
      const hasAccess = item.isAlwaysEnabled || (item.key ? hasPermission(item.key) : false);
      if (!hasAccess) return false;

      const matchesCategory =
        selectedCategory === "ALL" || item.category === selectedCategory;
      const q = searchQuery.trim().toLowerCase();
      const matchesSearch =
        !q ||
        item.title.toLowerCase().includes(q) ||
        item.description.toLowerCase().includes(q);

      return matchesCategory && matchesSearch;
    });
  }, [dynamicStudentModules, selectedCategory, searchQuery, hasPermission]);

  return (
    <MainLayout>
      <div className="w-full max-w-7xl mx-auto space-y-5 sm:space-y-6 py-2 px-1 sm:px-2 min-w-0 overflow-x-hidden">
        {/* ==================== 1. HERO BANNER ==================== */}
        <motion.section
          initial={{ opacity: 0, y: 12 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ duration: 0.35, ease: "easeOut" }}
          className="w-full max-w-full min-w-0 overflow-hidden bg-gradient-to-r from-white via-blue-50/50 to-indigo-50/40 dark:from-slate-900 dark:via-slate-900 dark:to-indigo-950/30 rounded-2xl border border-slate-200/90 dark:border-slate-800 p-4 sm:p-6 md:p-7 shadow-xs"
        >
          {/* Hàng Badge Tinh Gọn */}
          <div className="flex flex-wrap items-center gap-2 mb-3">
            <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-semibold bg-emerald-50 dark:bg-emerald-950/60 border border-emerald-200 dark:border-emerald-800 text-emerald-700 dark:text-emerald-300">
              <span className="w-1.5 h-1.5 rounded-full bg-emerald-500"></span>
              {formatRoleLabel(activeRole ?? "STUDENT")} (STUDENT)
            </span>
            <span className="inline-flex items-center px-2.5 py-1 rounded-full text-xs font-medium bg-indigo-50 dark:bg-indigo-950/60 border border-indigo-200 dark:border-indigo-800 text-indigo-700 dark:text-indigo-300">
              Fresher Academy Student Portal
            </span>
          </div>

          {/* Tiêu đề chào mừng */}
          <h1 className="text-xl sm:text-2xl md:text-3xl font-bold text-slate-900 dark:text-white tracking-tight mb-2 break-words">
            {greetingTitle}
          </h1>

          {/* Đoạn mô tả ngắn gọn */}
          <p className="text-xs sm:text-sm text-slate-600 dark:text-slate-400 leading-relaxed max-w-3xl mb-4 sm:mb-5">
            Chào mừng bạn đến với Cổng tra cứu FPT Academy System (FAS). Với vai trò{" "}
            <strong className="text-slate-800 dark:text-white font-semibold">STUDENT</strong>
            , bạn được phép tra cứu dữ liệu Điểm danh, Bảng điểm, Đồ án Mock Project, Luyện phỏng vấn và Tài liệu giáo trình theo danh sách quyền đọc (Read-Only) được cấp.
          </p>

          {/* 2 Nút Hành Động Nhanh (Quick CTA Buttons) */}
          <div className="flex flex-col sm:flex-row items-stretch sm:items-center gap-2.5 sm:gap-3 w-full">
            <Link
              to="/attendances"
              className="inline-flex items-center justify-center gap-2 px-4 py-2.5 bg-indigo-600 hover:bg-indigo-700 text-white rounded-lg text-xs font-semibold shadow-xs transition-colors shadow-indigo-200 dark:shadow-none"
            >
              <CalendarCheck className="size-4 shrink-0" />
              Điểm danh hôm nay
            </Link>
            <Link
              to="/student-scores"
              className="inline-flex items-center justify-center gap-2 px-4 py-2.5 bg-white dark:bg-slate-800 hover:bg-slate-50 dark:hover:bg-slate-700 text-slate-700 dark:text-slate-200 border border-slate-200/90 dark:border-slate-700 rounded-lg text-xs font-semibold transition-colors"
            >
              <Award className="size-4 text-slate-500 dark:text-slate-400 shrink-0" />
              Xem Bảng điểm
            </Link>
          </div>
        </motion.section>

        {/* ==================== 2. THANH BỘ LỌC & TÌM KIẾM NHANH ==================== */}
        <section className="flex flex-col md:flex-row items-stretch md:items-center justify-between gap-3 w-full min-w-0">
          {/* Category Tabs */}
          <div className="hidden lg:flex items-center gap-1.5 sm:gap-2 overflow-x-auto pb-1 sm:pb-0 no-scrollbar max-w-full">
            {categoryTabs.map((tab) => {
              const isActive = selectedCategory === tab.id;
              return (
                <button
                  key={tab.id}
                  type="button"
                  onClick={() => setSelectedCategory(tab.id)}
                  className={`px-3 py-1.5 sm:px-3.5 sm:py-2 rounded-lg text-xs whitespace-nowrap transition-colors shrink-0 ${
                    isActive
                      ? "font-semibold bg-indigo-600 text-white shadow-xs"
                      : "font-medium bg-white dark:bg-slate-900 hover:bg-slate-50 dark:hover:bg-slate-800 text-slate-600 dark:text-slate-400 border border-slate-200/90 dark:border-slate-800 cursor-pointer"
                  }`}
                >
                  {tab.label} ({tab.count})
                </button>
              );
            })}
          </div>

          {/* Quick Search Bar */}
          <div className="relative w-full lg:w-80 shrink-0 min-w-0">
            <Search className="size-4 absolute left-3 top-1/2 -translate-y-1/2 text-slate-400 pointer-events-none" />
            <input
              type="text"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              placeholder="Tìm nhanh phân hệ hoặc dữ liệu..."
              className="w-full pl-9 pr-3 py-2 bg-white dark:bg-slate-900 border border-slate-200/90 dark:border-slate-800 rounded-lg text-xs text-slate-800 dark:text-slate-200 placeholder-slate-400 focus:outline-none focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 transition-colors shadow-2xs"
            />
          </div>
        </section>

        {/* ==================== 3. LƯỚI THẺ CHỨC NĂNG (BENTO GRID) ==================== */}
        <section className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-5 gap-3.5 sm:gap-4 md:gap-5 w-full min-w-0">
          {filteredModules.map((item, index) => {
            const Icon = item.icon;
            const enabled =
              item.isAlwaysEnabled ||
              (item.key ? hasPermission(item.key) : false);

            return (
              <motion.div
                key={item.path}
                initial={{ opacity: 0, y: 14 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{
                  duration: 0.3,
                  delay: index * 0.04,
                  ease: "easeOut",
                }}
                className={`bg-white dark:bg-slate-900 rounded-xl border border-slate-200/90 dark:border-slate-800 p-4 sm:p-5 shadow-xs flex flex-col justify-between ${item.hoverBorder} hover:shadow-sm transition-all group`}
              >
                <div>
                  <div
                    className={`w-10 h-10 rounded-xl ${item.iconBg} ${item.iconColor} flex items-center justify-center mb-3.5`}
                  >
                    <Icon className="size-5" />
                  </div>
                  <h3
                    className={`font-bold text-slate-900 dark:text-white text-sm mb-1.5 ${item.hoverTitle} transition-colors`}
                  >
                    {item.title}
                  </h3>
                  <p className="text-xs text-slate-600 dark:text-slate-400 leading-relaxed mb-4">
                    {item.description}
                  </p>
                </div>

                <div className="pt-3 border-t border-slate-100 dark:border-slate-800 flex items-center justify-between">
                  {item.path === "#curriculum" ? (
                    <button
                      type="button"
                      onClick={() => setShowCurriculumModal(true)}
                      className="inline-flex items-center gap-1 text-xs font-semibold text-slate-700 dark:text-slate-300 hover:text-indigo-600 dark:hover:text-indigo-400 transition-colors cursor-pointer"
                    >
                      <span>{item.buttonText}</span>
                      <ArrowRight className="size-3.5" />
                    </button>
                  ) : enabled ? (
                    <Link
                      to={item.path}
                      className="inline-flex items-center gap-1 text-xs font-semibold text-slate-700 dark:text-slate-300 hover:text-indigo-600 dark:hover:text-indigo-400 transition-colors"
                    >
                      <span>{item.buttonText}</span>
                      <ArrowRight className="size-3.5" />
                    </Link>
                  ) : (
                    <span className="text-slate-400 font-semibold text-xs flex items-center gap-1 cursor-not-allowed">
                      <span>Chưa cấp quyền</span>
                    </span>
                  )}
                </div>
              </motion.div>
            );
          })}
        </section>
      </div>

      <AcademicPolicyModal
        open={showCurriculumModal}
        onOpenChange={setShowCurriculumModal}
        policyType="CURRICULUM"
      />
    </MainLayout>
  );
}

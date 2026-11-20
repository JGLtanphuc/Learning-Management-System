import { MainLayout } from "@/components/layout/MainLayout";
import { usePermissions } from "@/hooks/usePermissions";
import { formatRoleLabel } from "@/utils/rbac.utils";
import { formatGreetingTitle, formatUserDisplayName } from "@/utils/user.utils";
import { motion } from "motion/react";
import {
  ArrowRight,
  CalendarCheck,
  ClipboardCheck,
  FileQuestion,
  GraduationCap,
  HelpCircle,
  Kanban,
  Layers,
  Search,
  TableProperties,
  Users,
} from "lucide-react";
import { useMemo, useState } from "react";
import { Link } from "react-router-dom";

type CategoryKey = "all" | "teaching" | "grading" | "project" | "interview";

interface TeacherCapability {
  key: string;
  category: Exclude<CategoryKey, "all">;
  label: string;
  description: string;
  path: string;
  buttonText: string;
  icon: React.ComponentType<{ className?: string }>;
  featured?: boolean;
  iconBg: string;
  iconColor: string;
  hoverBorder: string;
  hoverTitle: string;
}

const TEACHER_CAPABILITIES: TeacherCapability[] = [
  {
    key: "ATTENDANCE_READ",
    category: "teaching",
    label: "Điểm danh chuyên cần",
    description:
      "Điểm danh học viên từng buổi học (sáng/chiều), theo dõi chuyên cần và lý do vắng/muộn.",
    path: "/attendances",
    buttonText: "Vào điểm danh",
    icon: CalendarCheck,
    featured: true,
    iconBg: "bg-indigo-600 text-white",
    iconColor: "text-white",
    hoverBorder: "hover:border-indigo-600",
    hoverTitle: "group-hover:text-indigo-600",
  },
  {
    key: "STUDENT_SCORE_READ",
    category: "grading",
    label: "Sổ điểm môn học (Gradebook)",
    description:
      "Nhập điểm Quiz, Assignment, Midterm, Final và tra cứu sổ điểm tổng hợp của lớp.",
    path: "/topic-marks/gradebook-demo",
    buttonText: "Xem sổ điểm",
    icon: TableProperties,
    iconBg: "bg-emerald-50 dark:bg-emerald-950/50",
    iconColor: "text-emerald-600 dark:text-emerald-400",
    hoverBorder: "hover:border-emerald-300 dark:hover:border-emerald-700",
    hoverTitle: "group-hover:text-emerald-600 dark:group-hover:text-emerald-400",
  },
  {
    key: "CLASS_READ",
    category: "teaching",
    label: "Quản lý Lớp đào tạo",
    description:
      "Xem thông tin các lớp học phụ trách, lịch trình đào tạo, phòng lab và sĩ số.",
    path: "/classes",
    buttonText: "Danh sách lớp",
    icon: GraduationCap,
    iconBg: "bg-amber-50 dark:bg-amber-950/50",
    iconColor: "text-amber-600 dark:text-amber-400",
    hoverBorder: "hover:border-amber-300 dark:hover:border-amber-700",
    hoverTitle: "group-hover:text-amber-600 dark:group-hover:text-amber-400",
  },
  {
    key: "STUDENT_READ",
    category: "teaching",
    label: "Danh sách Học viên Fresher",
    description:
      "Theo dõi hồ sơ, thông tin liên lạc, chuyên ngành đại học và tình trạng học tập của học viên.",
    path: "/students",
    buttonText: "Xem học viên",
    icon: Users,
    iconBg: "bg-purple-50 dark:bg-purple-950/50",
    iconColor: "text-purple-600 dark:text-purple-400",
    hoverBorder: "hover:border-purple-300 dark:hover:border-purple-700",
    hoverTitle: "group-hover:text-purple-600 dark:group-hover:text-purple-400",
  },
  {
    key: "MOCK_PROJECT_READ",
    category: "project",
    label: "Đồ án Mock Project",
    description:
      "Theo dõi tiến độ đồ án thực tế, hướng dẫn nhóm và chấm điểm đánh giá đồ án tốt nghiệp.",
    path: "/mock-projects",
    buttonText: "Quản lý đồ án",
    icon: Kanban,
    iconBg: "bg-rose-50 dark:bg-rose-950/50",
    iconColor: "text-rose-600 dark:text-rose-400",
    hoverBorder: "hover:border-rose-300 dark:hover:border-rose-700",
    hoverTitle: "group-hover:text-rose-600 dark:group-hover:text-rose-400",
  },
  {
    key: "STUDENT_SCORE_CREATE",
    category: "grading",
    label: "Bảng điểm cá nhân",
    description:
      "Xem bảng xếp hạng điểm, tìm kiếm theo mã học viên và lớp học.",
    path: "/student-scores",
    buttonText: "Vào bảng điểm",
    icon: ClipboardCheck,
    iconBg: "bg-sky-50 dark:bg-sky-950/50",
    iconColor: "text-sky-600 dark:text-sky-400",
    hoverBorder: "hover:border-sky-300 dark:hover:border-sky-700",
    hoverTitle: "group-hover:text-sky-600 dark:group-hover:text-sky-400",
  },
  {
    key: "INTERVIEW_QUESTION_READ",
    category: "interview",
    label: "Ngân hàng Câu hỏi Phỏng vấn",
    description:
      "Thêm, sửa, xóa câu hỏi, phân loại độ khó và soạn gợi ý đáp án chuẩn cho từng mục phỏng vấn.",
    path: "/interview-questions",
    buttonText: "Quản lý câu hỏi",
    icon: HelpCircle,
    iconBg: "bg-slate-900 text-white dark:bg-slate-100 dark:text-slate-900",
    iconColor: "text-white dark:text-slate-900",
    hoverBorder: "hover:border-slate-900 dark:hover:border-slate-200",
    hoverTitle: "group-hover:text-slate-900 dark:group-hover:text-white",
  },
  {
    key: "INTERVIEW_TOPIC_READ",
    category: "interview",
    label: "Mục / Lĩnh vực Phỏng vấn",
    description:
      "Tạo và quản lý các chủ đề phỏng vấn (Java, .NET, React, SQL...) để học viên lựa chọn thực hành.",
    path: "/interview-topics",
    buttonText: "Quản lý mục PV",
    icon: Layers,
    iconBg: "bg-slate-100 dark:bg-slate-800",
    iconColor: "text-slate-800 dark:text-slate-200",
    hoverBorder: "hover:border-slate-400 dark:hover:border-slate-600",
    hoverTitle: "group-hover:text-slate-900 dark:group-hover:text-white",
  },
  {
    key: "INTERVIEW_ASSIGNMENT_READ",
    category: "interview",
    label: "Đợt Đánh Giá & Bài Thi Phỏng Vấn",
    description:
      "Tạo đợt thi cho lớp, thiết lập ma trận đề (Dễ/TB/Khó), theo dõi học viên nộp bài và chấm điểm.",
    path: "/interview-assignments",
    buttonText: "Quản lý đợt thi",
    icon: FileQuestion,
    featured: true,
    iconBg: "bg-slate-900 text-white dark:bg-slate-100 dark:text-slate-900",
    iconColor: "text-white dark:text-slate-900",
    hoverBorder: "hover:border-slate-900 dark:hover:border-slate-200",
    hoverTitle: "group-hover:text-slate-900 dark:group-hover:text-white",
  },
];

const CATEGORY_TABS: { key: CategoryKey; label: string; count: number }[] = [
  { key: "all", label: "Tất cả chức năng", count: 9 },
  { key: "teaching", label: "Giảng dạy & Điểm danh", count: 3 },
  { key: "grading", label: "Điểm số & Sổ điểm", count: 2 },
  { key: "project", label: "Đồ án Mock Project", count: 1 },
  { key: "interview", label: "Phỏng vấn & Đánh giá", count: 3 },
];

export default function TeacherLandingPage() {
  const { user, activeRole, hasPermission } = usePermissions();
  const [selectedCategory, setSelectedCategory] = useState<CategoryKey>("all");
  const [searchQuery, setSearchQuery] = useState("");

  const displayName = useMemo(() => formatUserDisplayName(user, "Giảng viên"), [user]);
  const greetingTitle = useMemo(
    () => formatGreetingTitle(displayName, "TEACHER"),
    [displayName]
  );

  const filteredCapabilities = useMemo(() => {
    const query = searchQuery.trim().toLowerCase();
    return TEACHER_CAPABILITIES.filter((item) => {
      // Chỉ hiển thị module nếu Giảng viên có quyền READ (hoặc quyền tương ứng do Admin cấp)
      if (item.key && !hasPermission(item.key)) return false;

      const matchesCategory =
        selectedCategory === "all" || item.category === selectedCategory;
      const matchesSearch =
        !query ||
        item.label.toLowerCase().includes(query) ||
        item.description.toLowerCase().includes(query);
      return matchesCategory && matchesSearch;
    });
  }, [selectedCategory, searchQuery, hasPermission]);

  return (
    <MainLayout>
      <div className="w-full max-w-7xl mx-auto space-y-5 sm:space-y-6 py-2 px-1 sm:px-2 min-w-0 overflow-x-hidden">
        {/* ==================== 1. HERO BANNER (Tối Giản, Chuẩn Mực) ==================== */}
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
              {formatRoleLabel(activeRole ?? "TEACHER")} (TEACHER)
            </span>
            <span className="inline-flex items-center px-2.5 py-1 rounded-full text-xs font-medium bg-indigo-50 dark:bg-indigo-950/60 border border-indigo-200 dark:border-indigo-800 text-indigo-700 dark:text-indigo-300">
              Fresher Academy Trainer Portal
            </span>
          </div>

          {/* Tiêu đề chào mừng */}
          <h1 className="text-xl sm:text-2xl md:text-3xl font-bold text-slate-900 dark:text-white tracking-tight mb-2 break-words">
            {greetingTitle}
          </h1>

          {/* Đoạn mô tả ngắn gọn */}
          <p className="text-xs sm:text-sm text-slate-600 dark:text-slate-400 leading-relaxed max-w-3xl mb-4 sm:mb-5">
            Đây là cổng làm việc dành cho Giảng viên / Trainer tại Fresher Academy. Bạn có thể điểm danh hàng ngày, quản lý sổ điểm, theo dõi học viên và hướng dẫn đồ án Mock Project.
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
              to="/topic-marks/gradebook-demo"
              className="inline-flex items-center justify-center gap-2 px-4 py-2.5 bg-white dark:bg-slate-800 hover:bg-slate-50 dark:hover:bg-slate-700 text-slate-700 dark:text-slate-200 border border-slate-200/90 dark:border-slate-700 rounded-lg text-xs font-semibold transition-colors"
            >
              <TableProperties className="size-4 text-slate-500 dark:text-slate-400 shrink-0" />
              Mở Sổ điểm môn học
            </Link>
          </div>
        </motion.section>

        {/* ==================== 2. THANH BỘ LỌC & TÌM KIẾM NHANH ==================== */}
        <section className="flex flex-col md:flex-row items-stretch md:items-center justify-between gap-3 w-full min-w-0">
          {/* Category Tabs - Ẩn trên mobile/tablet để giao diện gọn gàng, chỉ hiện từ lg trở lên */}
          <div className="hidden lg:flex items-center gap-1.5 sm:gap-2 overflow-x-auto pb-1 sm:pb-0 no-scrollbar max-w-full">
            {CATEGORY_TABS.map((tab) => {
              const isActive = selectedCategory === tab.key;
              return (
                <button
                  key={tab.key}
                  type="button"
                  onClick={() => setSelectedCategory(tab.key)}
                  className={`px-3 py-1.5 sm:px-3.5 sm:py-2 rounded-lg text-xs whitespace-nowrap transition-colors shrink-0 ${
                    isActive
                      ? "font-semibold bg-indigo-600 text-white shadow-xs"
                      : "font-medium bg-white dark:bg-slate-900 hover:bg-slate-50 dark:hover:bg-slate-800 text-slate-600 dark:text-slate-400 border border-slate-200/90 dark:border-slate-800"
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
              placeholder="Tìm nhanh chức năng giảng viên..."
              className="w-full pl-9 pr-3 py-2 bg-white dark:bg-slate-900 border border-slate-200/90 dark:border-slate-800 rounded-lg text-xs text-slate-800 dark:text-slate-200 placeholder-slate-400 focus:outline-none focus:border-indigo-500 focus:ring-1 focus:ring-indigo-500 transition-colors shadow-2xs"
            />
          </div>
        </section>

        {/* ==================== 3. LƯỚI 7 THẺ CHỨC NĂNG (BENTO GRID) ==================== */}
        <section className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-3.5 sm:gap-4 md:gap-5 w-full min-w-0">
          {filteredCapabilities.map((item, index) => {
            const Icon = item.icon;

            if (item.featured) {
              return (
                <motion.div
                  key={item.key}
                  initial={{ opacity: 0, y: 14 }}
                  animate={{ opacity: 1, y: 0 }}
                  transition={{ duration: 0.3, delay: index * 0.04 }}
                  className="bg-gradient-to-b from-indigo-50/70 via-white to-white dark:from-indigo-950/40 dark:via-slate-900 dark:to-slate-900 rounded-xl border-2 border-indigo-500/80 p-4 sm:p-5 shadow-xs flex flex-col justify-between hover:border-indigo-600 transition-all group"
                >
                  <div>
                    <div className="flex items-center justify-between mb-3.5">
                      <div className="w-10 h-10 rounded-xl bg-indigo-600 text-white flex items-center justify-center shadow-xs">
                        <Icon className="size-5" />
                      </div>
                    </div>
                    <h3 className="font-bold text-slate-900 dark:text-white text-sm mb-1.5 group-hover:text-indigo-600 dark:group-hover:text-indigo-400 transition-colors">
                      {item.label}
                    </h3>
                    <p className="text-xs text-slate-600 dark:text-slate-400 leading-relaxed mb-4">
                      {item.description}
                    </p>
                  </div>
                  <div>
                    <Link
                      to={item.path}
                      className="w-full py-2 bg-indigo-600 hover:bg-indigo-700 text-white rounded-lg text-xs font-semibold transition-colors flex items-center justify-center gap-1.5 shadow-xs"
                    >
                      <span>{item.buttonText}</span>
                      <ArrowRight className="size-3.5" />
                    </Link>
                  </div>
                </motion.div>
              );
            }

            return (
              <motion.div
                key={item.key}
                initial={{ opacity: 0, y: 14 }}
                animate={{ opacity: 1, y: 0 }}
                transition={{ duration: 0.3, delay: index * 0.04 }}
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
                    {item.label}
                  </h3>
                  <p className="text-xs text-slate-600 dark:text-slate-400 leading-relaxed mb-4">
                    {item.description}
                  </p>
                </div>
                <div className="pt-3 border-t border-slate-100 dark:border-slate-800 flex items-center justify-between">
                  <Link
                    to={item.path}
                    className="inline-flex items-center gap-1 text-xs font-semibold text-slate-700 dark:text-slate-300 hover:text-indigo-600 dark:hover:text-indigo-400 transition-colors"
                  >
                    {item.buttonText}
                    <ArrowRight className="size-3.5" />
                  </Link>
                </div>
              </motion.div>
            );
          })}
        </section>
      </div>
    </MainLayout>
  );
}

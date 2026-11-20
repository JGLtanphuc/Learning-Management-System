import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import {
  CheckCircle2,
  CalendarCheck,
  Award,
  ShieldCheck,
  HelpCircle,
  BookOpen,
  Mail,
  Phone,
  Clock,
  AlertTriangle,
  MessageSquare,
} from "lucide-react";

export type PolicyType =
  | "TERMS"
  | "ATTENDANCE"
  | "HELP_DESK"
  | "FEEDBACK"
  | "TEACHING"
  | "GRADING"
  | "ACADEMIC"
  | "RBAC"
  | "CURRICULUM";

interface AcademicPolicyModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  policyType: PolicyType | null;
}

export function AcademicPolicyModal({
  open,
  onOpenChange,
  policyType,
}: AcademicPolicyModalProps) {
  if (!policyType) return null;

  const getPolicyContent = () => {
    switch (policyType) {
      case "ATTENDANCE":
        return {
          icon: <CalendarCheck className="size-5 text-slate-900 dark:text-slate-100" />,
          title: "Quy chế Chuyên cần & Kỷ luật Đào tạo (FAS)",
          subtitle: "Quy chuẩn đánh giá ý thức học tập và điều kiện tốt nghiệp của Fresher",
          body: (
            <div className="space-y-4 text-sm text-slate-700 dark:text-slate-300 leading-relaxed">
              <div className="p-3.5 bg-slate-100 dark:bg-slate-800/80 rounded-xl border border-slate-200 dark:border-slate-700 flex items-start gap-3">
                <AlertTriangle className="size-5 text-slate-900 dark:text-slate-100 shrink-0 mt-0.5" />
                <div className="text-xs">
                  <strong className="text-slate-900 dark:text-white block mb-0.5">
                    Yêu cầu bắt buộc:
                  </strong>
                  Học viên phải đạt tỷ lệ có mặt tối thiểu <strong>80%</strong> tổng thời lượng khóa đào tạo. Nếu vắng quá <strong>20%</strong>, học viên sẽ bị đình chỉ thi tốt nghiệp và không được phân bổ về BU sản xuất.
                </div>
              </div>

              <div>
                <h4 className="font-semibold text-slate-900 dark:text-white mb-1.5">
                  1. Quy định thời gian & Ca học
                </h4>
                <ul className="list-disc pl-5 space-y-1 text-xs">
                  <li>Ca sáng: 08:30 – 12:00 (Đi muộn sau 08:45 tính là muộn ca sáng).</li>
                  <li>Ca chiều: 13:30 – 17:30 (Đi muộn sau 13:45 tính là muộn ca chiều).</li>
                  <li>Đi muộn quá 30 phút mà không có lý do chính đáng sẽ tính là 0.5 buổi vắng.</li>
                </ul>
              </div>

              <div>
                <h4 className="font-semibold text-slate-900 dark:text-white mb-1.5">
                  2. Thủ tục xin nghỉ có phép
                </h4>
                <ul className="list-disc pl-5 space-y-1 text-xs">
                  <li>Đơn xin nghỉ phải được gửi tới Giảng viên phụ trách trước tối thiểu <strong>24 giờ</strong>.</li>
                  <li>Trường hợp ốm đau đột xuất cần bổ sung giấy xác nhận y tế hợp lệ trong vòng 48 giờ.</li>
                  <li>Các buổi nghỉ có phép vẫn tính vào tổng tỷ lệ vắng mặt chung của khóa học.</li>
                </ul>
              </div>

              <div>
                <h4 className="font-semibold text-slate-900 dark:text-white mb-1.5">
                  3. Xử lý vi phạm chuyên cần
                </h4>
                <ul className="list-disc pl-5 space-y-1 text-xs">
                  <li>Vắng không phép lần 1: Giảng viên nhắc nhở và ghi nhận hồ sơ học tập.</li>
                  <li>Vắng không phép lần 2: Trừ 20% phụ cấp đào tạo tháng tương ứng.</li>
                  <li>Vắng không phép từ lần 3 trở lên: Chuyển Hội đồng Kỷ luật Học viện xem xét thôi học.</li>
                </ul>
              </div>
            </div>
          ),
        };

      case "GRADING":
        return {
          icon: <Award className="size-5 text-slate-900 dark:text-slate-100" />,
          title: "Quy chuẩn Chấm điểm & Xếp loại Tốt nghiệp",
          subtitle: "Cấu trúc trọng số điểm và tiêu chuẩn đánh giá kết quả học tập tại FPT Software Academy",
          body: (
            <div className="space-y-4 text-sm text-slate-700 dark:text-slate-300 leading-relaxed">
              <div className="overflow-hidden rounded-xl border border-slate-200 dark:border-slate-800">
                <table className="w-full text-left text-xs">
                  <thead className="bg-slate-100 dark:bg-slate-800 text-slate-900 dark:text-white font-semibold">
                    <tr>
                      <th className="p-2.5">Đầu điểm thành phần</th>
                      <th className="p-2.5">Trọng số</th>
                      <th className="p-2.5">Điểm tối thiểu</th>
                      <th className="p-2.5">Mô tả</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-200 dark:divide-slate-800">
                    <tr>
                      <td className="p-2.5 font-medium">Quiz & Bài tập hàng ngày</td>
                      <td className="p-2.5">20%</td>
                      <td className="p-2.5">5.0 / 10</td>
                      <td className="p-2.5">Kiểm tra trắc nghiệm & coding ngắn sau từng module</td>
                    </tr>
                    <tr>
                      <td className="p-2.5 font-medium">Practical Exam (Thực hành)</td>
                      <td className="p-2.5">30%</td>
                      <td className="p-2.5">6.0 / 10</td>
                      <td className="p-2.5">Thi code trực tiếp trên hệ thống đánh giá kỹ thuật</td>
                    </tr>
                    <tr>
                      <td className="p-2.5 font-medium">Đồ án Mock Project</td>
                      <td className="p-2.5">40%</td>
                      <td className="p-2.5">6.5 / 10</td>
                      <td className="p-2.5">Đồ án thực tế, chấm theo Sprint và bảo vệ trước Hội đồng</td>
                    </tr>
                    <tr>
                      <td className="p-2.5 font-medium">Điểm Chuyên cần & Kỷ luật</td>
                      <td className="p-2.5">10%</td>
                      <td className="p-2.5">8.0 / 10</td>
                      <td className="p-2.5">Ý thức tham gia lớp học, thái độ làm việc nhóm và tuân thủ nội quy</td>
                    </tr>
                  </tbody>
                </table>
              </div>

              <div>
                <h4 className="font-semibold text-slate-900 dark:text-white mb-1.5">
                  Xếp loại Tốt nghiệp Khóa đào tạo:
                </h4>
                <div className="grid grid-cols-2 sm:grid-cols-4 gap-2 text-xs text-center font-medium">
                  <div className="p-2.5 rounded-lg border border-slate-200 dark:border-slate-800 bg-slate-50 dark:bg-slate-800/50">
                    <span className="font-bold text-slate-900 dark:text-white block text-sm">Xuất sắc (A)</span>
                    <span className="text-slate-500 text-[11px]">GPA &ge; 8.5</span>
                  </div>
                  <div className="p-2.5 rounded-lg border border-slate-200 dark:border-slate-800 bg-slate-50 dark:bg-slate-800/50">
                    <span className="font-bold text-slate-900 dark:text-white block text-sm">Giỏi (B)</span>
                    <span className="text-slate-500 text-[11px]">7.0 &le; GPA &lt; 8.5</span>
                  </div>
                  <div className="p-2.5 rounded-lg border border-slate-200 dark:border-slate-800 bg-slate-50 dark:bg-slate-800/50">
                    <span className="font-bold text-slate-900 dark:text-white block text-sm">Khá (C)</span>
                    <span className="text-slate-500 text-[11px]">6.0 &le; GPA &lt; 7.0</span>
                  </div>
                  <div className="p-2.5 rounded-lg border border-slate-200 dark:border-slate-800 bg-slate-50 dark:bg-slate-800/50">
                    <span className="font-bold text-slate-900 dark:text-white block text-sm">Trượt (Fail)</span>
                    <span className="text-slate-500 text-[11px]">GPA &lt; 6.0</span>
                  </div>
                </div>
              </div>
            </div>
          ),
        };

      case "HELP_DESK":
        return {
          icon: <HelpCircle className="size-5 text-slate-900 dark:text-slate-100" />,
          title: "Hỗ trợ Kỹ thuật & Học vụ (Help Desk)",
          subtitle: "Kênh liên hệ giải đáp thắc mắc dành cho Học viên, Giảng viên và Quản lý Đào tạo",
          body: (
            <div className="space-y-4 text-sm text-slate-700 dark:text-slate-300 leading-relaxed">
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 text-xs">
                <div className="p-4 rounded-xl border border-slate-200 dark:border-slate-800 bg-slate-50/50 dark:bg-slate-900 flex items-start gap-3 min-w-0">
                  <div className="size-8 rounded-lg bg-slate-900 text-white dark:bg-slate-100 dark:text-slate-900 flex items-center justify-center shrink-0">
                    <Mail size={16} />
                  </div>
                  <div className="min-w-0 flex-1">
                    <h5 className="font-semibold text-slate-900 dark:text-white mb-0.5">Hộp thư hỗ trợ</h5>
                    <p className="text-slate-600 dark:text-slate-400 font-medium break-all select-all">academy@fpt.com</p>
                    <p className="text-[11px] text-slate-500 mt-1">Phản hồi trong vòng 2-4 giờ làm việc</p>
                  </div>
                </div>

                <div className="p-4 rounded-xl border border-slate-200 dark:border-slate-800 bg-slate-50/50 dark:bg-slate-900 flex items-start gap-3 min-w-0">
                  <div className="size-8 rounded-lg bg-slate-900 text-white dark:bg-slate-100 dark:text-slate-900 flex items-center justify-center shrink-0">
                    <Phone size={16} />
                  </div>
                  <div className="min-w-0 flex-1">
                    <h5 className="font-semibold text-slate-900 dark:text-white mb-0.5">Hotline Học viện</h5>
                    <p className="text-slate-600 dark:text-slate-400 font-medium">024 7300 1866 (Ext: 102)</p>
                    <p className="text-[11px] text-slate-500 mt-1">Hỗ trợ khẩn cấp trong giờ hành chính</p>
                  </div>
                </div>
              </div>

              <div className="p-3.5 rounded-xl border border-slate-200 dark:border-slate-800 text-xs space-y-1.5">
                <div className="flex items-center gap-2 font-semibold text-slate-900 dark:text-white">
                  <Clock className="size-4 text-slate-500" />
                  <span>Thời gian tiếp nhận và xử lý yêu cầu:</span>
                </div>
                <p className="text-slate-600 dark:text-slate-400 pl-6">
                  Từ Thứ Hai đến Thứ Sáu: 08:30 – 12:00 và 13:30 – 17:30 (Nghỉ Thứ Bảy, Chủ Nhật và các ngày Lễ, Tết theo quy định nhà nước).
                </p>
              </div>
            </div>
          ),
        };

      case "FEEDBACK":
        return {
          icon: <MessageSquare className="size-5 text-slate-900 dark:text-slate-100" />,
          title: "Hòm thư Đóng góp Ý kiến & Cải tiến Đào tạo",
          subtitle: "Kênh tiếp nhận phản hồi từ Học viên, Giảng viên và Quản lý nhằm nâng cao chất lượng học tập",
          body: (
            <div className="space-y-4 text-sm text-slate-700 dark:text-slate-300 leading-relaxed">
              <div className="p-3.5 bg-slate-100 dark:bg-slate-800/80 rounded-xl border border-slate-200 dark:border-slate-700 flex items-start gap-3">
                <CheckCircle2 className="size-5 text-slate-900 dark:text-slate-100 shrink-0 mt-0.5" />
                <div className="text-xs">
                  <strong className="text-slate-900 dark:text-white block mb-0.5">
                    Lắng nghe & Không ngừng cải tiến:
                  </strong>
                  FPT Software Academy luôn trân trọng mọi ý kiến đóng góp về chương trình giảng dạy, cơ sở vật chất, thái độ hỗ trợ và quy trình học vụ để hoàn thiện trải nghiệm đào tạo tốt nhất cho Fresher.
                </div>
              </div>

              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 text-xs">
                <div className="p-4 rounded-xl border border-slate-200 dark:border-slate-800 bg-slate-50/50 dark:bg-slate-900 flex items-start gap-3 min-w-0">
                  <div className="size-8 rounded-lg bg-slate-900 text-white dark:bg-slate-100 dark:text-slate-900 flex items-center justify-center shrink-0">
                    <Mail size={16} />
                  </div>
                  <div className="min-w-0 flex-1">
                    <h5 className="font-semibold text-slate-900 dark:text-white mb-0.5">Email hòm thư góp ý</h5>
                    <p className="text-slate-600 dark:text-slate-400 font-medium break-all select-all">feedback.fas@fpt.com</p>
                    <p className="text-[11px] text-slate-500 mt-1">Gửi trực tiếp tới Ban Đảm bảo Chất lượng Đào tạo</p>
                  </div>
                </div>

                <div className="p-4 rounded-xl border border-slate-200 dark:border-slate-800 bg-slate-50/50 dark:bg-slate-900 flex items-start gap-3 min-w-0">
                  <div className="size-8 rounded-lg bg-slate-900 text-white dark:bg-slate-100 dark:text-slate-900 flex items-center justify-center shrink-0">
                    <ShieldCheck size={16} />
                  </div>
                  <div className="min-w-0 flex-1">
                    <h5 className="font-semibold text-slate-900 dark:text-white mb-0.5">Bảo mật danh tính</h5>
                    <p className="text-slate-600 dark:text-slate-400 font-medium">100% Ẩn danh nếu yêu cầu</p>
                    <p className="text-[11px] text-slate-500 mt-1">Thông tin phản ánh hoàn toàn được giữ bí mật</p>
                  </div>
                </div>
              </div>

              <div>
                <h4 className="font-semibold text-slate-900 dark:text-white mb-1.5 text-xs">
                  Quy trình tiếp nhận & xử lý phản ánh:
                </h4>
                <ol className="list-decimal pl-5 space-y-1 text-xs">
                  <li>Tiếp nhận thông tin đóng góp trong vòng 24 giờ làm việc.</li>
                  <li>Xác minh nội dung cùng các bên liên quan (Giảng viên, Quản trị viên, Đảm bảo chất lượng).</li>
                  <li>Phản hồi kế hoạch khắc phục và cải tiến trực tiếp cho người đóng góp hoặc thông báo chung lớp học.</li>
                </ol>
              </div>
            </div>
          ),
        };

      case "RBAC":
        return {
          icon: <ShieldCheck className="size-5 text-slate-900 dark:text-slate-100" />,
          title: "Chính sách Bảo mật & Phân quyền RBAC",
          subtitle: "Quy định kiểm soát truy cập và bảo vệ dữ liệu hồ sơ đào tạo",
          body: (
            <div className="space-y-3.5 text-sm text-slate-700 dark:text-slate-300 leading-relaxed text-xs">
              <p>
                Hệ thống <strong>FAS_System</strong> áp dụng mô hình phân quyền dựa trên vai trò (Role-Based Access Control) kết hợp kiểm soát mã hóa JWT Bearer Token theo chuẩn ISO/IEC 27001 của Tập đoàn FPT:
              </p>
              <ul className="list-disc pl-5 space-y-1.5">
                <li><strong>Học viên (Student):</strong> Chỉ có quyền đọc (Read-Only) hồ sơ điểm danh, bảng điểm cá nhân, đồ án Mock Project của nhóm mình và phòng phỏng vấn AI. Tuyệt đối không có quyền can thiệp dữ liệu điểm số.</li>
                <li><strong>Giảng viên (Teacher):</strong> Có quyền điểm danh, quản lý và cập nhật sổ điểm, hướng dẫn đề tài Mock Project cho các lớp học được phân công phụ trách.</li>
                <li><strong>Quản trị viên (Admin):</strong> Quản lý toàn bộ danh mục lớp học, học viên, phân quyền RBAC và phê duyệt danh sách bàn giao Fresher cho các BU sản xuất.</li>
              </ul>
              <div className="p-3 bg-slate-100 dark:bg-slate-800 rounded-lg text-slate-600 dark:text-slate-400">
                Mọi hành vi can thiệp trái phép vào API hoặc cố ý làm sai lệch kết quả điểm danh, điểm thi sẽ bị hệ thống lưu vết (Audit Log) và xử lý theo quy chế an ninh thông tin FPT.
              </div>
            </div>
          ),
        };

      case "CURRICULUM":
        return {
          icon: <BookOpen className="size-5 text-slate-900 dark:text-slate-100" />,
          title: "Kho Giáo trình & Tài liệu Đào tạo Chuẩn FAS",
          subtitle: "Tài liệu kỹ thuật chuyên sâu dành cho Fresher các chuyên ngành",
          body: (
            <div className="space-y-3 text-sm text-slate-700 dark:text-slate-300 leading-relaxed text-xs">
              <p>
                Tài liệu giáo trình chính thức của <strong>FPT Software Academy</strong> được biên soạn theo định chuẩn quốc tế và yêu cầu thực tế từ các đơn vị sản xuất phần mềm:
              </p>
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-2.5">
                <div className="p-3 rounded-lg border border-slate-200 dark:border-slate-800 bg-slate-50/50 dark:bg-slate-900">
                  <h5 className="font-semibold text-slate-900 dark:text-white mb-0.5">Java & Spring Boot Professional</h5>
                  <p className="text-slate-500 text-[11px]">Core Java, Spring Framework, Spring Boot 3, Hibernate, RESTful API, Microservices architecture.</p>
                </div>
                <div className="p-3 rounded-lg border border-slate-200 dark:border-slate-800 bg-slate-50/50 dark:bg-slate-900">
                  <h5 className="font-semibold text-slate-900 dark:text-white mb-0.5">.NET Enterprise Development</h5>
                  <p className="text-slate-500 text-[11px]">C# 12, .NET 8 Web API, Entity Framework Core, SQL Server, Clean Architecture, CQRS pattern.</p>
                </div>
                <div className="p-3 rounded-lg border border-slate-200 dark:border-slate-800 bg-slate-50/50 dark:bg-slate-900">
                  <h5 className="font-semibold text-slate-900 dark:text-white mb-0.5">Frontend Modern React & TS</h5>
                  <p className="text-slate-500 text-[11px]">TypeScript, React 19, Hooks, TanStack Query, Tailwind CSS, Responsive Design, State Management.</p>
                </div>
                <div className="p-3 rounded-lg border border-slate-200 dark:border-slate-800 bg-slate-50/50 dark:bg-slate-900">
                  <h5 className="font-semibold text-slate-900 dark:text-white mb-0.5">Database & Backend Engineering</h5>
                  <p className="text-slate-500 text-[11px]">SQL Server, Indexing, T-SQL Stored Procedures, Optimization, Database Design & Normalization.</p>
                </div>
              </div>
              <p className="text-slate-500 text-[11px] italic">
                * Học viên vui lòng liên hệ Giảng viên phụ trách lớp để nhận tài khoản truy cập kho tài liệu bài giảng nội bộ và repo template dự án.
              </p>
            </div>
          ),
        };

      default:
        return {
          icon: <CheckCircle2 className="size-5 text-slate-900 dark:text-slate-100" />,
          title: "Điều khoản & Quy chế Đào tạo Học viện (FAS)",
          subtitle: "Quy định chung áp dụng cho toàn thể học viên và giảng viên tại FPT Software Academy",
          body: (
            <div className="space-y-3 text-sm text-slate-700 dark:text-slate-300 leading-relaxed text-xs">
              <p>
                Chào mừng bạn đến với <strong>FPT Academy System (FAS)</strong>. Khi tham gia các chương trình đào tạo Fresher, người học cam kết tuân thủ đầy đủ các điều khoản sau:
              </p>
              <ul className="list-disc pl-5 space-y-1">
                <li>Bảo mật tuyệt đối thông tin mã nguồn và dữ liệu khách hàng thuộc các dự án Mock Project theo thỏa thuận NDA.</li>
                <li>Tham gia đầy đủ các kỳ thi đánh giá tiến độ và tích cực đóng góp trong quá trình làm việc nhóm.</li>
                <li>Không sao chép bài làm hoặc sử dụng các công cụ gian lận trong các kỳ thi đánh giá kỹ thuật.</li>
                <li>Chấp hành nghiêm túc sự phân bổ điều động sang các đơn vị sản xuất (BU) sau khi hoàn thành khóa đào tạo.</li>
              </ul>
            </div>
          ),
        };
    }
  };

  const content = getPolicyContent();

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-2xl md:max-w-3xl w-full max-h-[85vh] flex flex-col p-0 overflow-hidden bg-white dark:bg-slate-900 border-slate-200 dark:border-slate-800">
        {/* Sticky Header */}
        <DialogHeader className="p-5 border-b border-slate-100 dark:border-slate-800 shrink-0">
          <div className="flex items-center gap-3">
            <div className="size-9 rounded-xl bg-slate-100 dark:bg-slate-800 flex items-center justify-center shrink-0">
              {content.icon}
            </div>
            <div>
              <DialogTitle className="text-base font-bold text-slate-900 dark:text-white">
                {content.title}
              </DialogTitle>
              <DialogDescription className="text-xs text-slate-500 dark:text-slate-400 mt-0.5">
                {content.subtitle}
              </DialogDescription>
            </div>
          </div>
        </DialogHeader>

        {/* Scrollable Body */}
        <div className="flex-1 overflow-y-auto p-5 sm:p-6 space-y-4">
          {content.body}
        </div>

        {/* Sticky Footer */}
        <DialogFooter className="p-4 border-t border-slate-100 dark:border-slate-800 bg-slate-50/50 dark:bg-slate-900/50 shrink-0 flex items-center justify-between sm:justify-end">
          <Button
            type="button"
            onClick={() => onOpenChange(false)}
            className="bg-slate-900 hover:bg-slate-800 text-white dark:bg-slate-100 dark:hover:bg-slate-200 dark:text-slate-900 font-medium px-5 text-xs h-9"
          >
            Đã hiểu & Đóng
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

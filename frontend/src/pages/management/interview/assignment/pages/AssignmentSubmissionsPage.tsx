import { useState, useEffect } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import {
  ArrowLeft,
  Award,
  Clock,
  FileQuestion,
  Loader2,
  RotateCw,
  Save,
  Send,
  UserCheck,
  Users,
  X,
} from "lucide-react";
import { MainLayout } from "@/components/layout/MainLayout";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogTitle,
} from "@/components/ui/dialog";
import { interviewAssignmentApi } from "@/api";
import type {
  InterviewAssignmentDto,
  InterviewSessionDto,
  InterviewSessionSubmissionSummaryDto,
  ReviewQuestionScoreItem,
} from "@/types";

export function AssignmentSubmissionsPage() {
  const { id } = useParams<{ id: string }>();
  const assignmentId = Number(id);
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const [selectedSessionId, setSelectedSessionId] = useState<number | null>(null);
  const [reviewModalOpen, setReviewModalOpen] = useState(false);

  // Review form state
  const [sessionTeacherScore, setSessionTeacherScore] = useState<number | undefined>(undefined);
  const [teacherFeedback, setTeacherFeedback] = useState<string>("");
  const [overallVerdict, setOverallVerdict] = useState<string>("PASS");
  const [questionScores, setQuestionScores] = useState<Record<number, { score?: number; note?: string }>>({});

  // 1. Fetch assignment details
  const { data: assignment, isLoading: isLoadingAssignment } = useQuery<InterviewAssignmentDto>({
    queryKey: ["interview-assignment", assignmentId],
    queryFn: () => interviewAssignmentApi.getById(String(assignmentId)),
    enabled: !isNaN(assignmentId) && assignmentId > 0,
  });

  // 2. Fetch submissions for this assignment
  const {
    data: submissions = [],
    isLoading: isLoadingSubmissions,
    refetch: refetchSubmissions,
  } = useQuery<InterviewSessionSubmissionSummaryDto[]>({
    queryKey: ["interview-assignment-submissions", assignmentId],
    queryFn: () => interviewAssignmentApi.getSubmissions(assignmentId),
    enabled: !isNaN(assignmentId) && assignmentId > 0,
  });

  // 3. Fetch detailed session when review modal opens
  const { data: activeSession, isLoading: isLoadingSession } = useQuery<InterviewSessionDto>({
    queryKey: ["interview-session-detail", selectedSessionId],
    queryFn: () => interviewAssignmentApi.getSession(selectedSessionId!),
    enabled: selectedSessionId !== null,
  });

  // Open review modal and prepopulate state
  const handleOpenReview = (submission: InterviewSessionSubmissionSummaryDto) => {
    setSelectedSessionId(submission.sessionId);
    const initialScore = submission.teacherScore ?? submission.aiAverageScore ?? undefined;
    setSessionTeacherScore(initialScore);
    const passScore = assignment?.passingScore ?? 7.0;
    const scoreVal = initialScore ?? 0;
    const initialVerdict = submission.overallVerdict || (scoreVal >= passScore ? "PASS" : "FAIL");
    setOverallVerdict(initialVerdict);
    setReviewModalOpen(true);
  };

  // Sync initial question scores when session is loaded
  useEffect(() => {
    if (activeSession) {
      const scores: Record<number, { score?: number; note?: string }> = {};
      activeSession.questions.forEach((q) => {
        scores[q.questionId] = {
          score: q.teacherScore ?? q.aiScore ?? undefined,
          note: q.teacherNote ?? "",
        };
      });
      setQuestionScores(scores);
      setTeacherFeedback(activeSession.teacherFeedback || "");
      const currentScore = activeSession.teacherScore ?? activeSession.aiAverageScore ?? activeSession.totalScore ?? undefined;
      setSessionTeacherScore(currentScore);
      const passScore = assignment?.passingScore ?? 7.0;
      const scoreVal = currentScore ?? 0;
      setOverallVerdict(
        activeSession.overallVerdict || (scoreVal >= passScore ? "PASS" : "FAIL")
      );
    }
  }, [activeSession, assignment?.passingScore]);

  const passingScore = assignment?.passingScore ?? 7.0;
  const effectiveScore = sessionTeacherScore ?? activeSession?.totalScore ?? activeSession?.aiAverageScore ?? 0;
  const isPassVerdictInvalid = overallVerdict === "PASS" && effectiveScore < passingScore;

  // Helper date and initials formatter
  const formatSubmissionTime = (dateStr: string | null | undefined) => {
    if (!dateStr) return "Đang làm bài";
    const d = new Date(dateStr);
    if (isNaN(d.getTime())) return dateStr;
    const pad = (n: number) => String(n).padStart(2, "0");
    const hours = pad(d.getHours());
    const minutes = pad(d.getMinutes());
    const seconds = pad(d.getSeconds());
    const day = pad(d.getDate());
    const month = pad(d.getMonth() + 1);
    const year = d.getFullYear();
    return `${hours}:${minutes}:${seconds} ${day}/${month}/${year}`;
  };

  const getInitials = (name?: string) => {
    if (!name) return "HV";
    const parts = name.trim().split(/\s+/);
    if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase();
    return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
  };

  const gradedSubmissions = submissions.filter(
    (s) => s.totalScore !== null && s.totalScore !== undefined
  );
  const classAvgScore =
    gradedSubmissions.length > 0
      ? (
          gradedSubmissions.reduce((acc, curr) => acc + (curr.totalScore || 0), 0) /
          gradedSubmissions.length
        ).toFixed(1)
      : "--";
  const passCount = submissions.filter((s) => s.overallVerdict === "PASS").length;
  const passRate = submissions.length > 0 ? Math.round((passCount / submissions.length) * 100) : 0;
  const pendingCount = submissions.filter((s) => s.status === "SUBMITTED").length;

  // 4. Mutation to submit review & publish results
  const reviewMutation = useMutation({
    mutationFn: (publish: boolean) => {
      const qScores: ReviewQuestionScoreItem[] = Object.entries(questionScores).map(([qId, val]) => ({
        questionId: Number(qId),
        teacherScore: val.score,
        teacherNote: val.note,
      }));

      return interviewAssignmentApi.reviewSession(selectedSessionId!, {
        sessionTeacherScore: sessionTeacherScore,
        teacherFeedback: teacherFeedback,
        overallVerdict: overallVerdict,
        publishResult: publish,
        questionScores: qScores,
      });
    },
    onSuccess: (_, publish) => {
      toast.success(publish ? "Đã xác nhận và công bố điểm thi thành công!" : "Đã lưu bản chấm thi.");
      queryClient.invalidateQueries({ queryKey: ["interview-assignment-submissions", assignmentId] });
      queryClient.invalidateQueries({ queryKey: ["interview-session-detail", selectedSessionId] });
      setReviewModalOpen(false);
    },
    onError: (err: any) => {
      toast.error(err?.response?.data?.message || "Có lỗi xảy ra khi chấm thi.");
    },
  });

  const handleSaveReview = (publish: boolean) => {
    if (isPassVerdictInvalid) {
      toast.error(
        `Không thể xếp loại ĐẠT: Điểm đánh giá (${effectiveScore.toFixed(1)}/10) chưa đạt mức chuẩn qua môn (${passingScore.toFixed(1)}/10). Vui lòng cập nhật lại điểm số hoặc chọn CHƯA ĐẠT (FAIL).`
      );
      return;
    }
    reviewMutation.mutate(publish);
  };

  if (isLoadingAssignment) {
    return (
      <MainLayout>
        <div className="flex h-72 items-center justify-center">
          <Loader2 className="h-7 w-7 animate-spin text-slate-500" />
        </div>
      </MainLayout>
    );
  }

  if (!assignment) {
    return (
      <MainLayout>
        <div className="p-6 text-center">
          <p className="text-slate-600 dark:text-slate-400">Không tìm thấy thông tin đợt phỏng vấn.</p>
          <Button variant="outline" className="mt-4" onClick={() => navigate("/interview-assignments")}>
            <ArrowLeft className="mr-2 h-4 w-4" /> Quay lại
          </Button>
        </div>
      </MainLayout>
    );
  }

  return (
    <MainLayout>
      <div className="max-w-[1520px] mx-auto space-y-6">
        {/* BEGIN: HeaderSection */}
        <header className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
          <div>
            {/* Nút quay lại danh sách */}
            <button
              type="button"
              onClick={() => navigate("/interview-assignments")}
              className="inline-flex items-center gap-2 text-sm font-medium text-slate-500 hover:text-slate-900 transition-colors mb-2.5 dark:text-slate-400 dark:hover:text-slate-100"
            >
              <ArrowLeft className="w-4 h-4" />
              Danh sách đợt đánh giá
            </button>
            {/* Tiêu đề chính */}
            <h1 className="text-2xl font-bold tracking-tight text-slate-900 dark:text-slate-100">
              {assignment.title}
            </h1>
            {/* Thông tin lớp học và chủ đề */}
            <div className="flex items-center gap-2 text-sm text-slate-500 mt-1.5 flex-wrap dark:text-slate-400">
              <span>
                Chủ đề:{" "}
                <strong className="font-semibold text-slate-700 dark:text-slate-200">
                  {assignment.topicName}
                </strong>
              </span>
              {assignment.className && (
                <>
                  <span className="text-slate-300 dark:text-slate-700">•</span>
                  <span>
                    Lớp áp dụng:{" "}
                    <strong className="font-semibold text-slate-700 dark:text-slate-200">
                      {assignment.className}
                    </strong>
                  </span>
                </>
              )}
            </div>
          </div>

          {/* Action: Nút làm mới dữ liệu */}
          <div className="flex items-center shrink-0">
            <button
              type="button"
              onClick={() => refetchSubmissions()}
              className="inline-flex items-center gap-2 px-4 py-2.5 bg-white border border-slate-200 hover:bg-slate-50 hover:border-slate-300 active:bg-slate-100 text-slate-700 text-sm font-semibold rounded-xl shadow-xs transition-all duration-150 dark:bg-slate-900 dark:border-slate-800 dark:text-slate-200 dark:hover:bg-slate-800"
            >
              <RotateCw className="w-4 h-4 text-slate-500 dark:text-slate-400" />
              Làm mới dữ liệu
            </button>
          </div>
        </header>
        {/* END: HeaderSection */}

        {/* BEGIN: OverviewMetrics */}
        <section className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
          {/* Metric Card 1: Bộ đề */}
          <div className="bg-white dark:bg-slate-900 p-5 rounded-2xl border border-slate-200/80 dark:border-slate-800 shadow-xs flex items-start gap-4 transition-shadow hover:shadow-md">
            <div className="w-12 h-12 rounded-xl bg-slate-50 dark:bg-slate-800 border border-slate-100 dark:border-slate-700 flex items-center justify-center shrink-0 text-slate-600 dark:text-slate-300">
              <FileQuestion className="w-6 h-6" strokeWidth={1.8} />
            </div>
            <div className="flex-1 min-w-0">
              <p className="text-xs font-medium text-slate-500 dark:text-slate-400 mb-0.5">
                Bộ đề bốc ngẫu nhiên
              </p>
              <p className="text-2xl font-bold text-slate-900 dark:text-slate-100 tracking-tight">
                {assignment.totalQuestions} câu hỏi
              </p>
              <p className="text-xs font-medium text-slate-500 dark:text-slate-400 mt-1 flex items-center gap-1.5 flex-wrap">
                <span>{assignment.easyCount} Dễ</span>
                <span className="text-slate-300 dark:text-slate-700">•</span>
                <span>{assignment.mediumCount} Trung bình</span>
                <span className="text-slate-300 dark:text-slate-700">•</span>
                <span>{assignment.hardCount} Khó</span>
              </p>
            </div>
          </div>

          {/* Metric Card 2: Thời lượng & Điểm chuẩn */}
          <div className="bg-white dark:bg-slate-900 p-5 rounded-2xl border border-slate-200/80 dark:border-slate-800 shadow-xs flex items-start gap-4 transition-shadow hover:shadow-md">
            <div className="w-12 h-12 rounded-xl bg-slate-50 dark:bg-slate-800 border border-slate-100 dark:border-slate-700 flex items-center justify-center shrink-0 text-slate-600 dark:text-slate-300">
              <Clock className="w-6 h-6" strokeWidth={1.8} />
            </div>
            <div className="flex-1 min-w-0">
              <p className="text-xs font-medium text-slate-500 dark:text-slate-400 mb-0.5">
                Thời lượng &amp; Điểm chuẩn
              </p>
              <p className="text-2xl font-bold text-slate-900 dark:text-slate-100 tracking-tight">
                {assignment.durationMinutes} phút
              </p>
              <p className="text-xs font-medium text-slate-500 dark:text-slate-400 mt-1">
                Điểm chuẩn qua môn:{" "}
                <span className="font-semibold text-slate-700 dark:text-slate-200">
                  {assignment.passingScore}/10
                </span>
              </p>
            </div>
          </div>

          {/* Metric Card 3: Tổng bài nộp */}
          <div className="bg-white dark:bg-slate-900 p-5 rounded-2xl border border-slate-200/80 dark:border-slate-800 shadow-xs flex items-start gap-4 transition-shadow hover:shadow-md">
            <div className="w-12 h-12 rounded-xl bg-slate-50 dark:bg-slate-800 border border-slate-100 dark:border-slate-700 flex items-center justify-center shrink-0 text-slate-600 dark:text-slate-300">
              <Users className="w-6 h-6" strokeWidth={1.8} />
            </div>
            <div className="flex-1 min-w-0">
              <p className="text-xs font-medium text-slate-500 dark:text-slate-400 mb-0.5">
                Tổng bài nộp
              </p>
              <p className="text-2xl font-bold text-slate-900 dark:text-slate-100 tracking-tight">
                {submissions.length} học viên
              </p>
              <p className="text-xs font-medium text-slate-500 dark:text-slate-400 mt-1">
                {pendingCount} bài chờ chấm
              </p>
            </div>
          </div>

          {/* Metric Card 4: Điểm trung bình */}
          <div className="bg-white dark:bg-slate-900 p-5 rounded-2xl border border-slate-200/80 dark:border-slate-800 shadow-xs flex items-start gap-4 transition-shadow hover:shadow-md">
            <div className="w-12 h-12 rounded-xl bg-slate-50 dark:bg-slate-800 border border-slate-100 dark:border-slate-700 flex items-center justify-center shrink-0 text-slate-600 dark:text-slate-300">
              <Award className="w-6 h-6" strokeWidth={1.8} />
            </div>
            <div className="flex-1 min-w-0">
              <p className="text-xs font-medium text-slate-500 dark:text-slate-400 mb-0.5">
                Điểm trung bình lớp
              </p>
              <p className="text-2xl font-bold text-slate-900 dark:text-slate-100 tracking-tight">
                {classAvgScore !== "--" ? `${classAvgScore}/10` : "--"}
              </p>
              <p className="text-xs font-medium text-slate-500 dark:text-slate-400 mt-1">
                Tỷ lệ đạt:{" "}
                <span className="font-semibold text-slate-700 dark:text-slate-200">
                  {passRate}%
                </span>
              </p>
            </div>
          </div>
        </section>
        {/* END: OverviewMetrics */}

        {/* BEGIN: SubmissionsTableSection */}
        <section className="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200/80 dark:border-slate-800 shadow-xs overflow-hidden">
          {/* Table Header Title */}
          <div className="px-6 py-5 border-b border-slate-100 dark:border-slate-800 flex items-center justify-between">
            <div>
              <h2 className="text-base font-semibold text-slate-900 dark:text-slate-100 tracking-tight">
                Danh Sách Học Viên Nộp Bài
              </h2>
              <p className="text-xs text-slate-500 dark:text-slate-400 mt-0.5">
                Quản lý và theo dõi kết quả chấm thi của các thí sinh
              </p>
            </div>
            <span className="inline-flex items-center px-2.5 py-1 rounded-full text-xs font-semibold bg-slate-100 dark:bg-slate-800 text-slate-700 dark:text-slate-300">
              {submissions.length} bài nộp
            </span>
          </div>

          {/* Data Table */}
          {isLoadingSubmissions ? (
            <div className="flex h-48 items-center justify-center">
              <Loader2 className="h-6 w-6 animate-spin text-slate-500" />
            </div>
          ) : submissions.length === 0 ? (
            <div className="p-8 text-center text-sm text-slate-500 dark:text-slate-400">
              Chưa có học viên nào bắt đầu hoặc nộp bài cho đợt phỏng vấn này.
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-left border-collapse" id="students-table">
                <thead>
                  <tr className="border-b border-slate-200/80 dark:border-slate-800 bg-slate-50/60 dark:bg-slate-800/40 text-xs font-semibold text-slate-600 dark:text-slate-300">
                    <th className="py-3.5 px-6" scope="col">
                      Học viên
                    </th>
                    <th className="py-3.5 px-4" scope="col">
                      Mã HV
                    </th>
                    <th className="py-3.5 px-4" scope="col">
                      Thời gian nộp
                    </th>
                    <th className="py-3.5 px-4" scope="col">
                      Trạng thái
                    </th>
                    <th className="py-3.5 px-4 text-center" scope="col">
                      AI Chấm
                    </th>
                    <th className="py-3.5 px-4 text-center" scope="col">
                      GV Chấm
                    </th>
                    <th className="py-3.5 px-4 text-center" scope="col">
                      Tổng điểm
                    </th>
                    <th className="py-3.5 px-4 text-center" scope="col">
                      Kết quả
                    </th>
                    <th className="py-3.5 px-6 text-center" scope="col">
                      Thao tác
                    </th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100 dark:divide-slate-800 text-sm">
                  {submissions.map((sub) => {
                    const initials = getInitials(sub.studentName);
                    return (
                      <tr
                        key={sub.sessionId}
                        className="hover:bg-slate-50/70 dark:hover:bg-slate-800/50 transition-colors group"
                      >
                        <td className="py-4 px-6 whitespace-nowrap">
                          <div className="flex items-center gap-3">
                            <div className="w-9 h-9 rounded-full bg-slate-100 dark:bg-slate-800 text-slate-600 dark:text-slate-300 font-semibold flex items-center justify-center text-xs border border-slate-200 dark:border-slate-700 shrink-0">
                              {initials}
                            </div>
                            <div>
                              <div className="font-semibold text-slate-900 dark:text-slate-100 group-hover:text-slate-700 dark:group-hover:text-slate-300 transition-colors">
                                {sub.studentName}
                              </div>
                              <div className="text-xs text-slate-500 dark:text-slate-400">
                                {sub.studentEmail}
                              </div>
                            </div>
                          </div>
                        </td>
                        <td className="py-4 px-4 whitespace-nowrap font-medium text-slate-700 dark:text-slate-300 text-xs">
                          {sub.studentCode}
                        </td>
                        <td className="py-4 px-4 whitespace-nowrap text-slate-600 dark:text-slate-400 text-xs">
                          {formatSubmissionTime(sub.submittedAt)}
                        </td>
                        <td className="py-4 px-4 whitespace-nowrap">
                          {sub.status === "IN_PROGRESS" && (
                            <span className="inline-flex items-center px-3 py-1 rounded-full text-xs font-medium border border-slate-300 dark:border-slate-700 text-slate-700 dark:text-slate-300 bg-white dark:bg-slate-800">
                              Đang làm bài
                            </span>
                          )}
                          {sub.status === "SUBMITTED" && (
                            <span className="inline-flex items-center px-3 py-1 rounded-full text-xs font-medium bg-[#111827] text-white dark:bg-slate-100 dark:text-slate-900">
                              Chờ duyệt
                            </span>
                          )}
                          {sub.status === "REVIEWED" && (
                            <span className="inline-flex items-center px-3 py-1 rounded-full text-xs font-medium border border-slate-400 text-slate-800 dark:text-slate-200 bg-white dark:bg-slate-800">
                              Đã chấm
                            </span>
                          )}
                          {sub.status === "PUBLISHED" && (
                            <span className="inline-flex items-center px-3 py-1 rounded-full text-xs font-medium bg-[#111827] text-white dark:bg-slate-100 dark:text-slate-900">
                              Đã công bố điểm
                            </span>
                          )}
                        </td>
                        <td className="py-4 px-4 whitespace-nowrap text-center text-slate-700 dark:text-slate-300 text-xs font-medium">
                          {sub.aiAverageScore !== null && sub.aiAverageScore !== undefined
                            ? `${sub.aiAverageScore.toFixed(1)}/10`
                            : "--"}
                        </td>
                        <td className="py-4 px-4 whitespace-nowrap text-center text-slate-700 dark:text-slate-300 text-xs font-medium">
                          {sub.teacherScore !== null && sub.teacherScore !== undefined
                            ? `${sub.teacherScore.toFixed(1)}/10`
                            : "--"}
                        </td>
                        <td className="py-4 px-4 whitespace-nowrap text-center font-bold text-slate-900 dark:text-slate-100 text-sm">
                          {sub.totalScore !== null && sub.totalScore !== undefined
                            ? `${sub.totalScore.toFixed(1)}/10`
                            : "--"}
                        </td>
                        <td className="py-4 px-4 whitespace-nowrap text-center">
                          {sub.overallVerdict === "PASS" ? (
                            <span className="inline-flex items-center px-3 py-0.5 rounded-full text-xs font-semibold bg-[#111827] text-white dark:bg-slate-100 dark:text-slate-900 shadow-xs">
                              ĐẠT
                            </span>
                          ) : sub.overallVerdict === "FAIL" ? (
                            <span className="inline-flex items-center px-3 py-0.5 rounded-full text-xs font-semibold border border-slate-300 dark:border-slate-700 text-slate-700 dark:text-slate-300 bg-white dark:bg-slate-800 shadow-xs">
                              CHƯA ĐẠT
                            </span>
                          ) : (
                            <span className="text-xs text-slate-400">--</span>
                          )}
                        </td>
                        <td className="py-4 px-6 whitespace-nowrap text-center">
                          <button
                            type="button"
                            onClick={() => handleOpenReview(sub)}
                            className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium text-slate-700 hover:text-slate-900 bg-white border border-slate-300 rounded-lg hover:bg-slate-50 transition-colors shadow-xs dark:bg-slate-800 dark:border-slate-700 dark:text-slate-200 dark:hover:bg-slate-700"
                          >
                            <UserCheck className="w-3.5 h-3.5 text-slate-500 dark:text-slate-400" />
                            {sub.status === "PUBLISHED" ? "Xem lại" : "Chấm bài"}
                          </button>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          )}
        </section>
        {/* END: SubmissionsTableSection */}

        {/* Polished & Spacious Review Modal */}
        <Dialog open={reviewModalOpen} onOpenChange={setReviewModalOpen}>
          <DialogContent
            showCloseButton={false}
            className="fixed top-1/2 left-1/2 z-50 flex h-[94vh] w-[95vw] -translate-x-1/2 -translate-y-1/2 flex-col gap-0 overflow-hidden rounded-2xl border border-slate-200 bg-white p-0 shadow-2xl sm:max-w-4xl md:max-w-5xl lg:max-w-[1240px] dark:border-slate-800 dark:bg-slate-900"
          >
            {/* Modal Header (Sticky) */}
            <header className="flex shrink-0 items-start justify-between border-b border-slate-100 bg-white px-6 pt-6 pb-4 sm:px-8 dark:border-slate-800 dark:bg-slate-900">
              <div>
                <div className="flex flex-wrap items-center gap-3">
                  <DialogTitle className="text-xl font-bold tracking-tight text-slate-900 sm:text-2xl dark:text-slate-100">
                    Chấm Điểm & Đánh Giá Bài Phỏng Vấn
                  </DialogTitle>
                  <span className="inline-flex items-center rounded-full border border-slate-200 bg-slate-100 px-3 py-1 text-xs font-medium text-slate-800 dark:border-slate-700 dark:bg-slate-800 dark:text-slate-200">
                    {activeSession?.status === "PUBLISHED" ? "Đã công bố điểm" : "Bản chấm thi"}
                  </span>
                </div>
                <p className="mt-1 text-xs font-normal text-slate-500 sm:text-sm dark:text-slate-400">
                  Học viên:{" "}
                  <span className="font-semibold text-slate-800 dark:text-slate-200">
                    {activeSession?.studentName}
                  </span>
                  <span className="mx-1.5 text-slate-300 dark:text-slate-600">•</span>
                  Lớp:{" "}
                  <span className="text-slate-600 dark:text-slate-400">
                    {activeSession?.className || assignment.className || "Chung"}
                  </span>
                  {activeSession?.submittedAt && (
                    <>
                      <span className="mx-1.5 text-slate-300 dark:text-slate-600">•</span>
                      Nộp lúc:{" "}
                      <span className="text-slate-600 dark:text-slate-400">
                        {new Date(activeSession.submittedAt).toLocaleTimeString("vi-VN", {
                          hour: "2-digit",
                          minute: "2-digit",
                          second: "2-digit",
                        })}{" "}
                        {new Date(activeSession.submittedAt).toLocaleDateString("vi-VN", {
                          day: "2-digit",
                          month: "2-digit",
                          year: "numeric",
                        })}
                      </span>
                    </>
                  )}
                </p>
              </div>
              <button
                type="button"
                onClick={() => setReviewModalOpen(false)}
                aria-label="Đóng cửa sổ"
                className="-mr-2 -mt-1 rounded-lg p-1.5 text-slate-400 transition-colors hover:bg-slate-100 hover:text-slate-600 dark:hover:bg-slate-800 dark:hover:text-slate-200"
              >
                <X className="h-5 w-5" />
              </button>
            </header>

            {/* Modal Body (Scrollable) */}
            <main className="flex-1 space-y-6 overflow-y-auto px-6 py-6 text-slate-800 sm:px-8 dark:bg-slate-950/60 dark:text-slate-200">
              {isLoadingSession || !activeSession ? (
                <div className="flex h-64 items-center justify-center">
                  <Loader2 className="h-7 w-7 animate-spin text-slate-500" />
                </div>
              ) : (
                <>
                  {/* BEGIN: ScoreAndConclusionSection */}
                  <section className="grid grid-cols-1 gap-6 rounded-2xl border border-slate-200 bg-white p-5 shadow-xs md:grid-cols-3 dark:border-slate-800 dark:bg-slate-900">
                    {/* Col 1: AI Suggested Score */}
                    <div className="flex flex-col justify-between border-slate-200 md:border-r md:pr-6 dark:border-slate-800">
                      <span className="text-xs font-medium text-slate-500 sm:text-sm dark:text-slate-400">
                        Điểm AI chấm gợi ý:
                      </span>
                      <div className="my-2">
                        <div className="flex items-baseline space-x-1">
                          <span className="text-4xl font-extrabold tracking-tight text-slate-900 dark:text-slate-100">
                            {activeSession.aiAverageScore !== null && activeSession.aiAverageScore !== undefined
                              ? activeSession.aiAverageScore.toFixed(1)
                              : "--"}
                          </span>
                          <span className="text-base font-medium text-slate-400">/ 10</span>
                        </div>
                      </div>
                      <span className="text-xs text-slate-400 dark:text-slate-500">
                        Tính từ toàn bộ câu trả lời của học viên
                      </span>
                    </div>

                    {/* Col 2: Lecturer Score Input */}
                    <div className="flex flex-col justify-between border-slate-200 md:border-r md:pr-6 dark:border-slate-800">
                      <div className="flex items-center justify-between">
                        <span className="text-xs font-medium text-slate-700 sm:text-sm dark:text-slate-300">
                          Điểm Giảng viên chấm:
                        </span>
                        {activeSession.aiAverageScore !== null && activeSession.aiAverageScore !== undefined && (
                          <button
                            type="button"
                            onClick={() => {
                              const aiScore = activeSession.aiAverageScore!;
                              setSessionTeacherScore(aiScore);
                              setOverallVerdict(aiScore >= passingScore ? "PASS" : "FAIL");
                            }}
                            className="rounded-md border border-slate-200 px-2.5 py-1 text-xs font-medium text-slate-700 transition-colors hover:bg-slate-50 dark:border-slate-700 dark:text-slate-300 dark:hover:bg-slate-800"
                          >
                            Lấy điểm AI
                          </button>
                        )}
                      </div>
                      <div className="my-2">
                        <input
                          type="number"
                          step="0.5"
                          min="0"
                          max="10"
                          value={sessionTeacherScore ?? ""}
                          onChange={(e) => {
                            const val = e.target.value ? Number(e.target.value) : undefined;
                            setSessionTeacherScore(val);
                            if (val !== undefined) {
                              setOverallVerdict(val >= passingScore ? "PASS" : "FAIL");
                            }
                          }}
                          placeholder="VD: 8.5"
                          className="w-full rounded-lg border border-slate-200 bg-white px-3.5 py-2 text-base font-semibold text-slate-800 transition-shadow focus:border-slate-900 focus:outline-none focus:ring-1 focus:ring-slate-900 dark:border-slate-700 dark:bg-slate-950 dark:text-slate-100 dark:focus:border-slate-100 dark:focus:ring-slate-100"
                        />
                      </div>
                      <span className="text-xs text-slate-400 dark:text-slate-500">
                        Điểm này sẽ là điểm số chính thức công bố
                      </span>
                    </div>

                    {/* Col 3: Final Conclusion */}
                    <div className="flex flex-col justify-between">
                      <span className="text-xs font-medium text-slate-700 sm:text-sm dark:text-slate-300">
                        Kết luận toàn bài:
                      </span>
                      <div className="my-2">
                        <select
                          value={overallVerdict}
                          onChange={(e) => setOverallVerdict(e.target.value)}
                          className={`w-full cursor-pointer rounded-lg border px-3.5 py-2.5 text-sm font-semibold transition-shadow focus:border-slate-900 focus:outline-none focus:ring-1 focus:ring-slate-900 dark:border-slate-700 dark:bg-slate-950 dark:text-slate-100 dark:focus:border-slate-100 dark:focus:ring-slate-100 ${
                            isPassVerdictInvalid
                              ? "border-slate-900 bg-slate-100 text-slate-900 dark:border-slate-300 dark:bg-slate-800 dark:text-slate-100"
                              : "border-slate-200 bg-white text-slate-800 dark:border-slate-700 dark:bg-slate-950 dark:text-slate-100"
                          }`}
                        >
                          <option value="FAIL">CHƯA ĐẠT (FAIL)</option>
                          <option value="PASS">ĐẠT (PASS)</option>
                        </select>
                      </div>
                      {isPassVerdictInvalid ? (
                        <p className="text-[11px] font-medium text-slate-900 dark:text-slate-100">
                          ⚠️ Điểm số ({effectiveScore.toFixed(1)}/10) chưa đạt chuẩn ({passingScore}/10). Không thể lưu ĐẠT.
                        </p>
                      ) : (
                        <span className="text-xs text-slate-400 dark:text-slate-500">
                          Mức chuẩn qua môn: {assignment.passingScore}/10
                        </span>
                      )}
                    </div>
                  </section>

                  {/* BEGIN: OverallLecturerFeedbackSection */}
                  <section className="rounded-2xl border border-slate-200 bg-white p-5 shadow-xs dark:border-slate-800 dark:bg-slate-900">
                    <label
                      htmlFor="overall-lecturer-feedback"
                      className="mb-2.5 block text-xs font-bold uppercase tracking-wider text-slate-800 dark:text-slate-200"
                    >
                      NHẬN XÉT TỔNG THỂ CỦA GIẢNG VIÊN:
                    </label>
                    <textarea
                      id="overall-lecturer-feedback"
                      rows={3}
                      value={teacherFeedback}
                      onChange={(e) => setTeacherFeedback(e.target.value)}
                      placeholder="Góp ý về tư duy, phong thái phỏng vấn, nắm bắt bản chất công nghệ và định hướng kiến thức cần bổ sung..."
                      className="w-full resize-y rounded-xl border border-slate-200 bg-white p-3.5 text-sm text-slate-700 placeholder-slate-400 transition-colors focus:border-slate-900 focus:outline-none focus:ring-1 focus:ring-slate-900 dark:border-slate-700 dark:bg-slate-950 dark:text-slate-200 dark:focus:border-slate-100 dark:focus:ring-slate-100"
                    />
                  </section>

                  {/* BEGIN: QuestionListSection */}
                  <section className="space-y-4 pt-1">
                    <h2 className="text-sm font-bold uppercase tracking-wider text-slate-900 dark:text-slate-100">
                      CHI TIẾT TỪNG CÂU HỎI ({activeSession.questions.length} CÂU)
                    </h2>

                    {activeSession.questions.map((q, idx) => {
                      const currentScore = questionScores[q.questionId]?.score ?? q.teacherScore ?? q.aiScore ?? undefined;
                      const currentNote = questionScores[q.questionId]?.note ?? q.teacherNote ?? "";

                      return (
                        <article
                          key={q.questionId}
                          className="space-y-4 rounded-2xl border border-slate-200 bg-white p-5 shadow-xs dark:border-slate-800 dark:bg-slate-900"
                        >
                          {/* Question Header */}
                          <div className="flex flex-wrap items-center justify-between gap-3 border-b border-slate-100 pb-3.5 dark:border-slate-800">
                            <div className="flex items-center gap-2">
                              <span className="text-sm font-bold text-slate-900 dark:text-slate-100">Câu {idx + 1}.</span>
                              <span className="rounded-full border border-slate-200 bg-white px-2.5 py-0.5 text-xs font-medium text-slate-600 dark:border-slate-700 dark:bg-slate-800 dark:text-slate-300">
                                {q.questionType === "MCQ" ? "Trắc nghiệm" : "Tự luận"}
                              </span>
                              <span className="rounded-full border border-slate-200 bg-white px-2.5 py-0.5 text-[10px] font-medium uppercase text-slate-600 dark:border-slate-700 dark:bg-slate-800 dark:text-slate-300">
                                {q.difficulty}
                              </span>
                            </div>

                            <div className="flex items-center gap-4 text-xs">
                              <div className="font-medium text-slate-600 dark:text-slate-400">
                                AI:{" "}
                                <span className="font-bold text-slate-900 dark:text-slate-100">
                                  {q.aiScore !== null && q.aiScore !== undefined ? `${q.aiScore}/10` : "--"}
                                </span>
                              </div>
                              <div className="flex items-center gap-1.5 font-medium text-slate-700 dark:text-slate-300">
                                <span>Điểm GV:</span>
                                <input
                                  type="number"
                                  step="0.5"
                                  min="0"
                                  max="10"
                                  value={currentScore ?? ""}
                                  onChange={(e) => {
                                    const val = e.target.value ? Number(e.target.value) : undefined;
                                    setQuestionScores((prev) => ({
                                      ...prev,
                                      [q.questionId]: { ...prev[q.questionId], score: val },
                                    }));
                                  }}
                                  className="h-8 w-16 rounded-md border border-slate-200 bg-white p-1 text-center text-xs font-semibold focus:border-slate-900 focus:outline-none focus:ring-1 focus:ring-slate-900 dark:border-slate-700 dark:bg-slate-950 dark:text-slate-100 dark:focus:border-slate-100 dark:focus:ring-slate-100"
                                />
                              </div>
                            </div>
                          </div>

                          {/* Question Text */}
                          <p className="text-sm font-medium leading-relaxed text-slate-900 dark:text-slate-100">
                            {q.questionText}
                          </p>

                          {/* Student Answer */}
                          <div className="rounded-xl border border-slate-100 bg-slate-50 p-3.5 dark:border-slate-800 dark:bg-slate-950/80">
                            <span className="mb-1 block text-xs font-medium text-slate-500 dark:text-slate-400">
                              Câu trả lời của học viên:
                            </span>
                            {q.questionType === "MCQ" ? (
                              <p className="text-xs font-semibold text-slate-800 dark:text-slate-200">
                                Đáp án đã chọn:{" "}
                                <span className="font-normal text-slate-500 dark:text-slate-400">
                                  {q.selectedOptionId || "(Chưa chọn đáp án)"}
                                </span>
                              </p>
                            ) : q.answerText ? (
                              <p className="whitespace-pre-wrap break-words text-xs leading-relaxed text-slate-800 dark:text-slate-200">
                                {q.answerText}
                              </p>
                            ) : (
                              <p className="text-xs italic text-slate-500">(Học viên bỏ trống câu này)</p>
                            )}
                          </div>

                          {/* Suggested Answer */}
                          {(q.correctOptionId || q.sampleAnswer) && (
                            <div className="rounded-xl border border-slate-200 bg-white p-3.5 text-xs leading-relaxed text-slate-700 dark:border-slate-800 dark:bg-slate-900 dark:text-slate-300">
                              {q.questionType === "MCQ" ? (
                                <>
                                  <span className="font-bold text-slate-900 dark:text-slate-100">Đáp án đúng chuẩn:</span>{" "}
                                  <span className="font-semibold">{q.correctOptionId}</span>
                                </>
                              ) : (
                                <>
                                  <span className="font-bold text-slate-900 dark:text-slate-100">Gợi ý đáp án:</span>{" "}
                                  {q.sampleAnswer}
                                </>
                              )}
                            </div>
                          )}

                          {/* AI Feedback */}
                          {q.aiFeedback && (
                            <div className="rounded-xl border border-slate-100 bg-slate-50 p-3.5 text-xs text-slate-700 dark:border-slate-800 dark:bg-slate-950/80 dark:text-slate-300">
                              <span className="font-bold text-slate-900 dark:text-slate-100">AI Nhận xét:</span>{" "}
                              {q.aiFeedback}
                            </div>
                          )}

                          {/* Lecturer Note Input */}
                          <div>
                            <input
                              type="text"
                              value={currentNote}
                              onChange={(e) => {
                                const val = e.target.value;
                                setQuestionScores((prev) => ({
                                  ...prev,
                                  [q.questionId]: { ...prev[q.questionId], note: val },
                                }));
                              }}
                              placeholder="Ghi chú thêm của giảng viên cho câu này (tùy chọn)..."
                              className="w-full rounded-xl border border-slate-200 bg-white px-3.5 py-2.5 text-xs text-slate-700 placeholder-slate-400 transition-colors focus:border-slate-900 focus:outline-none focus:ring-1 focus:ring-slate-900 dark:border-slate-700 dark:bg-slate-950 dark:text-slate-200 dark:focus:border-slate-100 dark:focus:ring-slate-100"
                            />
                          </div>
                        </article>
                      );
                    })}
                  </section>
                </>
              )}
            </main>

            {/* Modal Footer (Sticky) */}
            <footer className="flex shrink-0 flex-col items-center justify-end gap-3 border-t border-slate-200 bg-white px-6 py-3.5 sm:flex-row sm:px-8 dark:border-slate-800 dark:bg-slate-900">
              <button
                type="button"
                onClick={() => setReviewModalOpen(false)}
                className="w-full rounded-xl border border-slate-200 px-5 py-2 text-xs font-semibold text-slate-700 shadow-xs transition-all hover:bg-slate-50 hover:text-slate-900 active:scale-95 sm:w-auto sm:text-sm dark:border-slate-700 dark:text-slate-300 dark:hover:bg-slate-800 dark:hover:text-white"
              >
                Đóng
              </button>
              <button
                type="button"
                disabled={reviewMutation.isPending}
                onClick={() => handleSaveReview(false)}
                className="inline-flex w-full items-center justify-center gap-2 rounded-xl border border-slate-200 px-5 py-2 text-xs font-semibold text-slate-700 shadow-xs transition-all hover:bg-slate-50 hover:text-slate-900 active:scale-95 disabled:opacity-50 sm:w-auto sm:text-sm dark:border-slate-700 dark:text-slate-300 dark:hover:bg-slate-800 dark:hover:text-white"
              >
                <Save className="h-4 w-4 text-slate-600 dark:text-slate-400" />
                <span>Lưu bản nháp chấm</span>
              </button>
              <button
                type="button"
                disabled={reviewMutation.isPending}
                onClick={() => handleSaveReview(true)}
                className="inline-flex w-full items-center justify-center gap-2 rounded-xl bg-slate-900 px-5 py-2 text-xs font-semibold text-white shadow-md transition-all hover:bg-slate-800 active:scale-95 disabled:opacity-50 sm:w-auto sm:text-sm dark:bg-slate-100 dark:text-slate-900 dark:hover:bg-white"
              >
                {reviewMutation.isPending ? (
                  <Loader2 className="h-4 w-4 animate-spin" />
                ) : (
                  <Send className="h-4 w-4 -rotate-12 text-white dark:text-slate-900" />
                )}
                <span>Xác nhận & Công bố điểm</span>
              </button>
            </footer>
          </DialogContent>
        </Dialog>
      </div>
    </MainLayout>
  );
}

export default AssignmentSubmissionsPage;

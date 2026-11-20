import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { Link } from "react-router-dom";
import { useQuery, useMutation } from "@tanstack/react-query";
import { toast } from "sonner";
import {
  AlertCircle,
  ArrowLeft,
  Award,
  Bookmark,
  Building2,
  CheckCircle2,
  ChevronLeft,
  ChevronRight,
  Clock,
  Code,
  FileQuestion,
  FileText,
  Flag,
  GraduationCap,
  Keyboard,
  Layers,
  Loader2,
  Mic,
  MicOff,
  Printer,
  Send,
  Target,
  TrendingUp,
  UserCheck,
} from "lucide-react";
import { MainLayout } from "@/components/layout/MainLayout";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Textarea } from "@/components/ui/textarea";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { interviewAssignmentApi } from "@/api";
import type {
  InterviewAssignmentDto,
  InterviewSessionDto,
  InterviewSessionQuestionDto,
  InterviewAnswerMode,
  InterviewQuestionOption,
} from "@/types";
import type {
  SpeechRecognitionEventLike,
  SpeechRecognitionErrorEventLike,
  SpeechRecognitionInstance,
} from "./types/speech-recognition.types";

type PageViewMode = "LOBBY" | "EXAM_ROOM" | "SCORECARD";

export default function MockInterviewPage() {
  // Navigation & View Mode
  const [viewMode, setViewMode] = useState<PageViewMode>("LOBBY");
  const [activeSession, setActiveSession] = useState<InterviewSessionDto | null>(null);
  const [currentQuestionIdx, setCurrentQuestionIdx] = useState<number>(0);

  // Exam Local State for active question
  const [activeAnswerText, setActiveAnswerText] = useState<string>("");
  const [activeSelectedOption, setActiveSelectedOption] = useState<string | null>(null);
  const [activeMode, setActiveMode] = useState<InterviewAnswerMode>("TEXT");
  const [activeFlag, setActiveFlag] = useState<boolean>(false);
  const [isAutoSaving, setIsAutoSaving] = useState<boolean>(false);

  // Timer state
  const [remainingSeconds, setRemainingSeconds] = useState<number>(0);
  const timerRef = useRef<any>(null);
  const hasAutoSubmittedRef = useRef<boolean>(false);
  const handleSubmitExamRef = useRef<() => void>(() => {});

  // Submit Modal
  const [isSubmitConfirmOpen, setIsSubmitConfirmOpen] = useState<boolean>(false);
  const [isSubmitting, setIsSubmitting] = useState<boolean>(false);

  // Speech to text state
  const [isListening, setIsListening] = useState<boolean>(false);
  const [speechLang, setSpeechLang] = useState<"vi-VN" | "en-US">("vi-VN");
  const [interimTranscript, setInterimTranscript] = useState<string>("");
  const recognitionRef = useRef<SpeechRecognitionInstance | null>(null);
  const isListeningRef = useRef<boolean>(false);

  // 1. Fetch Student's Assignments
  const {
    data: assignments = [],
    isLoading: isLoadingAssignments,
    refetch: refetchAssignments,
  } = useQuery<InterviewAssignmentDto[]>({
    queryKey: ["interview-my-assignments"],
    queryFn: () => interviewAssignmentApi.getMyAssignments(),
  });

  // Reset auto-submitted flag whenever a session changes
  useEffect(() => {
    hasAutoSubmittedRef.current = false;
  }, [activeSession?.sessionId]);

  // Countdown Timer handler
  useEffect(() => {
    if (viewMode !== "EXAM_ROOM" || !activeSession || activeSession.status !== "IN_PROGRESS") {
      return;
    }

    timerRef.current = setInterval(() => {
      setRemainingSeconds((prev) => {
        if (prev <= 1) {
          clearInterval(timerRef.current!);
          return 0;
        }
        return prev - 1;
      });
    }, 1000);

    return () => {
      if (timerRef.current) clearInterval(timerRef.current);
    };
  }, [viewMode, activeSession?.sessionId, activeSession?.status]);

  // Auto-Submit Effect: Triggered automatically as soon as remainingSeconds reaches 0
  useEffect(() => {
    if (
      viewMode === "EXAM_ROOM" &&
      activeSession &&
      activeSession.status === "IN_PROGRESS" &&
      remainingSeconds <= 0 &&
      !hasAutoSubmittedRef.current &&
      !isSubmitting
    ) {
      hasAutoSubmittedRef.current = true;
      toast.warning("Hết giờ làm bài! Hệ thống đang tự động nộp bài thi của bạn.");
      handleSubmitExamRef.current();
    }
  }, [viewMode, activeSession, remainingSeconds, isSubmitting]);

  const formatTimer = (totalSeconds: number) => {
    const safeSec = Math.max(0, totalSeconds);
    const m = Math.floor(safeSec / 60);
    const s = safeSec % 60;
    return `${m.toString().padStart(2, "0")}:${s.toString().padStart(2, "0")}`;
  };

  // Sync state when moving between questions
  const currentQuestion = useMemo<InterviewSessionQuestionDto | null>(() => {
    if (!activeSession || !activeSession.questions || activeSession.questions.length === 0) {
      return null;
    }
    return activeSession.questions[currentQuestionIdx] || null;
  }, [activeSession, currentQuestionIdx]);

  useEffect(() => {
    if (currentQuestion) {
      setActiveAnswerText(currentQuestion.answerText || "");
      setActiveSelectedOption(currentQuestion.selectedOptionId || null);
      setActiveMode(currentQuestion.answerMode || "TEXT");
      setActiveFlag(currentQuestion.isFlagged || false);
      stopListening();
    }
  }, [currentQuestionIdx, currentQuestion?.questionId]);

  // Voice Speech Recognition setup
  const stopListening = useCallback(() => {
    isListeningRef.current = false;
    setIsListening(false);
    setInterimTranscript("");
    if (recognitionRef.current) {
      try {
        recognitionRef.current.stop();
      } catch {
        // ignore
      }
      recognitionRef.current = null;
    }
  }, []);

  const startListening = useCallback(() => {
    const SpeechRecognition =
      (window as any).SpeechRecognition || (window as any).webkitSpeechRecognition;

    if (!SpeechRecognition) {
      toast.error("Trình duyệt không hỗ trợ nhận diện giọng nói (Web Speech API). Vui lòng dùng Chrome hoặc Edge.");
      return;
    }

    stopListening();

    try {
      const recognition = new SpeechRecognition();
      recognition.continuous = true;
      recognition.interimResults = true;
      recognition.lang = speechLang;

      recognition.onresult = (event: SpeechRecognitionEventLike) => {
        let finalChunk = "";
        let interimChunk = "";

        for (let i = event.resultIndex; i < event.results.length; i++) {
          const item = event.results[i];
          if (item.isFinal) {
            finalChunk += item[0].transcript;
          } else {
            interimChunk += item[0].transcript;
          }
        }

        if (finalChunk.trim()) {
          setActiveAnswerText((prev) => {
            const separator = prev && !prev.endsWith(" ") ? " " : "";
            const nextText = `${prev}${separator}${finalChunk.trim()}`;
            // Auto save
            triggerAutoSave({ text: nextText });
            return nextText;
          });
        }

        setInterimTranscript(interimChunk);
      };

      recognition.onerror = (event: SpeechRecognitionErrorEventLike) => {
        if (event.error !== "no-speech") {
          toast.error(`Lỗi microphone: ${event.error}`);
        }
      };

      recognition.onend = () => {
        if (isListeningRef.current) {
          try {
            recognition.start();
          } catch {
            setIsListening(false);
            isListeningRef.current = false;
          }
        } else {
          setIsListening(false);
        }
      };

      recognitionRef.current = recognition;
      recognition.start();
      setIsListening(true);
      isListeningRef.current = true;
      toast.info(`Microphone đang ghi nhận (${speechLang === "vi-VN" ? "Tiếng Việt" : "English"}).`);
    } catch (err) {
      toast.error("Không thể khởi động microphone. Vui lòng cấp quyền truy cập thiết bị.");
      setIsListening(false);
      isListeningRef.current = false;
    }
  }, [speechLang, stopListening]);

  const toggleListening = () => {
    if (isListening) {
      stopListening();
    } else {
      setActiveMode("VOICE");
      startListening();
    }
  };

  // Auto-Save answer to backend
  const triggerAutoSave = async (override?: {
    text?: string;
    optionId?: string | null;
    flag?: boolean;
    mode?: InterviewAnswerMode;
  }) => {
    if (!activeSession || !currentQuestion || activeSession.status !== "IN_PROGRESS") return;

    const nextText = override?.text !== undefined ? override.text : activeAnswerText;
    const nextOption = override?.optionId !== undefined ? override.optionId : activeSelectedOption;
    const nextFlag = override?.flag !== undefined ? override.flag : activeFlag;
    const nextMode = override?.mode !== undefined ? override.mode : activeMode;

    // Update local state in activeSession.questions
    const updatedQuestions = [...activeSession.questions];
    updatedQuestions[currentQuestionIdx] = {
      ...updatedQuestions[currentQuestionIdx],
      answerText: nextText,
      selectedOptionId: nextOption || undefined,
      isFlagged: nextFlag,
      answerMode: nextMode,
    };
    setActiveSession({ ...activeSession, questions: updatedQuestions });

    try {
      setIsAutoSaving(true);
      await interviewAssignmentApi.saveAnswer(activeSession.sessionId, {
        questionId: currentQuestion.questionId,
        answerText: nextText,
        selectedOptionId: nextOption || undefined,
        isFlagged: nextFlag,
        answerMode: nextMode,
      });
    } catch (err) {
      console.error("Auto save failed", err);
    } finally {
      setIsAutoSaving(false);
    }
  };

  // Handle Option Select (for MCQ)
  const handleSelectOption = (optionId: string) => {
    setActiveSelectedOption(optionId);
    triggerAutoSave({ optionId });
  };

  // Handle Toggle Flag
  const handleToggleFlag = () => {
    const nextFlag = !activeFlag;
    setActiveFlag(nextFlag);
    triggerAutoSave({ flag: nextFlag });
    toast.info(nextFlag ? "Đã đánh dấu câu hỏi để xem lại." : "Đã bỏ đánh dấu câu hỏi.");
  };

  // Start Session (Random draw 10 questions)
  const startSessionMutation = useMutation({
    mutationFn: (assignmentId: number) => interviewAssignmentApi.startSession(assignmentId),
    onSuccess: async (session) => {
      setActiveSession(session);
      setCurrentQuestionIdx(0);
      const secs = session.remainingSeconds > 0 ? session.remainingSeconds : session.durationMinutes * 60;
      setRemainingSeconds(secs);
      if (session.status === "IN_PROGRESS" && session.remainingSeconds <= 0) {
        hasAutoSubmittedRef.current = true;
        toast.warning("Hết giờ làm bài! Hệ thống đang tự động nộp bài thi của bạn.");
        const resultSession = await interviewAssignmentApi.submitSession(session.sessionId).catch(() => null);
        if (resultSession) {
          setActiveSession(resultSession);
        }
        setViewMode("SCORECARD");
        refetchAssignments();
      } else {
        setViewMode(session.status === "IN_PROGRESS" ? "EXAM_ROOM" : "SCORECARD");
        toast.success(session.status === "IN_PROGRESS" ? "Đã bắt đầu bài kiểm tra phỏng vấn!" : "Đã tải kết quả bài thi.");
      }
    },
    onError: (err: any) => {
      toast.error(err?.response?.data?.message || "Không thể bắt đầu phiên làm bài.");
    },
  });

  // View Existing Session
  const loadSession = async (sessionId: number) => {
    try {
      const session = await interviewAssignmentApi.getSession(sessionId);
      setActiveSession(session);
      setCurrentQuestionIdx(0);
      setRemainingSeconds(session.remainingSeconds);
      if (session.status === "IN_PROGRESS" && session.remainingSeconds <= 0) {
        hasAutoSubmittedRef.current = true;
        toast.warning("Hết giờ làm bài! Hệ thống đang tự động nộp bài thi của bạn.");
        const resultSession = await interviewAssignmentApi.submitSession(session.sessionId).catch(() => null);
        if (resultSession) {
          setActiveSession(resultSession);
        }
        setViewMode("SCORECARD");
        refetchAssignments();
      } else {
        setViewMode(session.status === "IN_PROGRESS" ? "EXAM_ROOM" : "SCORECARD");
      }
    } catch (err: any) {
      toast.error(err?.response?.data?.message || "Không thể tải phiên làm bài.");
    }
  };

  // Submit Exam
  const handleSubmitExam = async () => {
    if (!activeSession) return;
    setIsSubmitting(true);
    stopListening();
    try {
      // Auto-save in-flight answer if student was editing
      if (currentQuestion && activeSession.status === "IN_PROGRESS") {
        await interviewAssignmentApi.saveAnswer(activeSession.sessionId, {
          questionId: currentQuestion.questionId,
          answerText: activeAnswerText,
          selectedOptionId: activeSelectedOption || undefined,
          isFlagged: activeFlag,
          answerMode: activeMode,
        }).catch(() => {});
      }
      const resultSession = await interviewAssignmentApi.submitSession(activeSession.sessionId);
      setActiveSession(resultSession);
      setViewMode("SCORECARD");
      setIsSubmitConfirmOpen(false);
      toast.success("Nộp bài thi thành công! Bài làm của bạn đang chờ Giảng viên đánh giá và công bố điểm.");
      refetchAssignments();
    } catch (err: any) {
      toast.error(err?.response?.data?.message || "Có lỗi xảy ra khi nộp bài thi.");
    } finally {
      setIsSubmitting(false);
    }
  };

  handleSubmitExamRef.current = handleSubmitExam;

  // Parse MCQ options from JSON
  const parsedOptions = useMemo<InterviewQuestionOption[]>(() => {
    if (!currentQuestion?.optionsJson) return [];
    try {
      return JSON.parse(currentQuestion.optionsJson);
    } catch {
      return [];
    }
  }, [currentQuestion?.optionsJson]);

  // Statistics for submission warning
  const answeredCount = useMemo(() => {
    if (!activeSession?.questions) return 0;
    return activeSession.questions.filter((q) => {
      if (q.questionType === "MCQ") return !!q.selectedOptionId;
      return !!q.answerText && q.answerText.trim().length > 0;
    }).length;
  }, [activeSession?.questions]);

  const flaggedCount = useMemo(() => {
    if (!activeSession?.questions) return 0;
    return activeSession.questions.filter((q) => q.isFlagged).length;
  }, [activeSession?.questions]);

  // Compute Comprehensive Candidate Assessment Report
  const reportData = useMemo(() => {
    if (!activeSession) return null;
    if (activeSession.report) return activeSession.report;

    const questions = activeSession.questions || [];
    const totalQ = questions.length;
    const answeredQ = questions.filter((q) =>
      q.questionType === "MCQ" ? !!q.selectedOptionId : !!q.answerText?.trim()
    ).length;
    const completionRate = totalQ > 0 ? Math.round((answeredQ / totalQ) * 100) : 0;

    const actualAverageFromQuestions = totalQ > 0
      ? questions.reduce((sum, q) => sum + (q.score ?? q.aiScore ?? 0), 0) / totalQ
      : 0;

    let totalScore = actualAverageFromQuestions;
    if (activeSession.totalScore !== null && activeSession.totalScore !== undefined) {
      if (answeredQ < totalQ && totalQ > 0 && activeSession.totalScore > actualAverageFromQuestions) {
        totalScore = activeSession.totalScore * (answeredQ / totalQ);
      } else {
        totalScore = activeSession.totalScore;
      }
    }
    const totalScorePercentage = Math.round(Math.min(100, Math.max(0, (totalScore / 10) * 100)));

    let rank: "ADVANCED" | "PROFICIENT" | "COMPETENT" | "DEVELOPING" | "NOVICE" = "COMPETENT";
    let rankTitle = "Khá (Competent Fresher)";
    let rankSummary = "Ứng viên đáp ứng chuẩn đầu ra, nắm chắc kiến thức cơ bản và kỹ năng lập trình thông dụng.";
    let ojtRecommendation = "Đạt chuẩn tốt nghiệp khóa đào tạo Fresher. Khuyến nghị kèm cặp 1-2 tuần đầu khi onboard dự án.";

    if (completionRate < 50) {
      rank = "NOVICE";
      rankTitle = "Chưa đạt (Không hoàn thành đủ bài thi)";
      rankSummary = `Ứng viên chỉ hoàn thành ${completionRate}% (${answeredQ}/${totalQ} câu hỏi), bỏ trống phần lớn bài thi nên chưa đủ điều kiện đánh giá chuẩn đầu ra.`;
      ojtRecommendation = "Yêu cầu thi lại toàn bộ bài phỏng vấn để có đủ dữ liệu đánh giá năng lực thực tế.";
    } else if (completionRate < 70) {
      rank = "DEVELOPING";
      rankTitle = "Trung bình (Developing - Chưa đủ khối lượng thi)";
      rankSummary = `Ứng viên mới hoàn thành ${completionRate}% (${answeredQ}/${totalQ} câu hỏi). Cần rèn luyện tính kỷ luật, quản lý thời gian và hoàn thành trọn vẹn đề thi.`;
      ojtRecommendation = "Cần tham gia thêm 1 đợt phỏng vấn bổ trợ và hoàn thành tối thiểu 70% số câu hỏi trước khi xét tiếp nhận dự án.";
    } else if (totalScorePercentage >= 90 && completionRate >= 90) {
      rank = "ADVANCED";
      rankTitle = "Xuất sắc (Ready to Deploy - Fast Track)";
      rankSummary = "Ứng viên nắm vững toàn diện kiến thức nền tảng và chuyên sâu, tư duy hệ thống và khả năng giải thích bản chất kỹ thuật xuất sắc.";
      ojtRecommendation = "Sẵn sàng tham gia trực tiếp vào các dự án phần mềm trọng điểm (Core Developer). Khuyến nghị vào các dự án công nghệ mới.";
    } else if (totalScorePercentage >= 80 && completionRate >= 80) {
      rank = "PROFICIENT";
      rankTitle = "Giỏi (Ready to Deploy - Standard)";
      rankSummary = "Ứng viên có năng lực chuyên môn tốt, hiểu sâu cơ chế hoạt động và cấu trúc hệ thống, phản xạ kỹ thuật vững vàng.";
      ojtRecommendation = "Đạt chuẩn yêu cầu phỏng vấn tuyển dụng trực tiếp vào Business Unit (BU) FPT Software. Sẵn sàng nhận dự án thực tế.";
    } else if (totalScorePercentage >= 70 && completionRate >= 70) {
      rank = "COMPETENT";
      rankTitle = "Khá (Competent Fresher)";
      rankSummary = "Ứng viên đáp ứng chuẩn đầu ra môn học, nắm chắc kiến thức cơ bản và vận dụng tốt các kỹ năng lập trình thông dụng.";
      ojtRecommendation = "Đạt chuẩn tốt nghiệp khóa đào tạo Fresher. Khuyến nghị phân công Mentor kèm cặp 1-2 tuần đầu khi onboard dự án.";
    } else if (totalScorePercentage >= 50) {
      rank = "DEVELOPING";
      rankTitle = "Trung bình (Developing - Cần bổ túc)";
      rankSummary = "Ứng viên nắm được các khái niệm cơ bản nhưng còn hạn chế ở các câu hỏi tình huống thực tế và tối ưu hóa hệ thống.";
      ojtRecommendation = "Cần tham gia thêm 1 Sprint ôn tập và làm bài tập thực hành bổ trợ trước khi tham gia phỏng vấn lại.";
    } else {
      rank = "NOVICE";
      rankTitle = "Chưa đạt (Requires Retraining)";
      rankSummary = "Ứng viên chưa nắm vững các từ khóa trọng tâm và kiến thức cốt lõi của môn học.";
      ojtRecommendation = "Cần bố trí đào tạo lại (Retrain) module chuyên môn này để củng cố nền tảng kiến thức.";
    }

    const easyQ = questions.filter((q) => q.difficulty?.toUpperCase() === "EASY");
    const mediumQ = questions.filter((q) => q.difficulty?.toUpperCase() === "MEDIUM");
    const hardQ = questions.filter((q) => q.difficulty?.toUpperCase() === "HARD");

    const easyScorePercent = easyQ.length > 0
      ? Math.round((easyQ.reduce((acc, q) => acc + (q.score ?? q.aiScore ?? 0), 0) / (easyQ.length * 10)) * 100)
      : 0;
    const mediumScorePercent = mediumQ.length > 0
      ? Math.round((mediumQ.reduce((acc, q) => acc + (q.score ?? q.aiScore ?? 0), 0) / (mediumQ.length * 10)) * 100)
      : 0;
    const hardScorePercent = hardQ.length > 0
      ? Math.round((hardQ.reduce((acc, q) => acc + (q.score ?? q.aiScore ?? 0), 0) / (hardQ.length * 10)) * 100)
      : 0;

    const mcqQ = questions.filter((q) => q.questionType?.toUpperCase() === "MCQ");
    const mcqTotal = mcqQ.length;
    const mcqCorrect = mcqQ.filter((q) => q.isCorrect === true).length;
    const mcqAccuracyRate = mcqTotal > 0 ? Math.round((mcqCorrect / mcqTotal) * 100) : 0;

    const subjQ = questions.filter((q) => q.questionType?.toUpperCase() !== "MCQ");
    const subjectiveTotal = subjQ.length;
    const subjectiveScorePercent = subjectiveTotal > 0
      ? Math.round((subjQ.reduce((acc, q) => acc + (q.score ?? q.aiScore ?? 0), 0) / (subjectiveTotal * 10)) * 100)
      : 0;

    const strengths: string[] = [];
    const areasForImprovement: string[] = [];
    const recommendations: string[] = [];

    if (mcqAccuracyRate >= 80 && mcqTotal > 0) {
      strengths.push(`Nắm chắc kiến thức trắc nghiệm lý thuyết căn bản (Độ chính xác ${mcqAccuracyRate}%).`);
    }
    if (easyScorePercent >= 80 && easyQ.length > 0) {
      strengths.push(`Nền tảng cú pháp và khái niệm cơ bản vững vàng (Mức Dễ: ${easyScorePercent}%).`);
    }
    if (mediumScorePercent >= 75 && mediumQ.length > 0) {
      strengths.push(`Hiểu đúng cơ chế hoạt động và kiến trúc giải pháp (Mức Trung bình: ${mediumScorePercent}%).`);
    }
    if (hardScorePercent >= 70 && hardQ.length > 0) {
      strengths.push(`Khả năng phân tích và xử lý bài toán kỹ thuật phức tạp tốt (Mức Khó: ${hardScorePercent}%).`);
    }
    if (strengths.length === 0) {
      strengths.push("Học viên đã nỗ lực hoàn thành đầy đủ các câu hỏi theo thời gian quy định.");
    }

    if (hardScorePercent < 70 && hardQ.length > 0) {
      areasForImprovement.push(`Cần nâng cao kỹ năng xử lý các bài toán mức Khó (Hiện đạt ${hardScorePercent}%).`);
    }
    if (mcqAccuracyRate < 80 && mcqTotal > 0) {
      areasForImprovement.push(`Cần rà soát lại các khái niệm lý thuyết cốt lõi để giảm thiểu sai sót trắc nghiệm (Đúng ${mcqCorrect}/${mcqTotal} câu).`);
    }
    if (subjectiveScorePercent < 75 && subjectiveTotal > 0) {
      areasForImprovement.push("Cần trình bày câu trả lời tự luận/thoại mạch lạc hơn, nhấn mạnh vào các từ khóa kỹ thuật trọng tâm.");
    }
    if (areasForImprovement.length === 0) {
      areasForImprovement.push("Duy trì phong độ và tiếp tục tìm hiểu các mẫu thiết kế (Design Patterns) nâng cao.");
    }

    recommendations.push("Tham khảo kho tài liệu giáo trình và thực hành Clean Code trong phân hệ Tài liệu (Books).");
    if (hardScorePercent < 70) {
      recommendations.push("Tập trung ôn luyện các chủ đề chuyên sâu: Quản lý bộ nhớ, Concurrency, Tối ưu hóa truy vấn Database và Security.");
    }
    recommendations.push("Luyện tập thêm các bài phỏng vấn thoại để cải thiện phản xạ trả lời nhanh và phong thái tự tin.");

    return {
      totalScorePercentage,
      completionRate,
      totalQuestions: totalQ,
      answeredQuestions: answeredQ,
      proficiencyRank: rank,
      proficiencyRankTitle: rankTitle,
      proficiencySummary: rankSummary,
      ojtRecommendation,
      easyCount: easyQ.length,
      easyScorePercent,
      mediumCount: mediumQ.length,
      mediumScorePercent,
      hardCount: hardQ.length,
      hardScorePercent,
      mcqTotal,
      mcqCorrect,
      mcqAccuracyRate,
      subjectiveTotal,
      subjectiveScorePercent,
      strengths,
      areasForImprovement,
      recommendations,
    };
  }, [activeSession]);

  // ==============================================================
  // RENDER 1: LOBBY VIEW (Assignment selection)
  // ==============================================================
  if (viewMode === "LOBBY") {
    return (
      <MainLayout pathName={{ "mock-interview": "Đánh giá phỏng vấn" }}>
        <div className="max-w-[1440px] mx-auto px-4 sm:px-6 py-6 lg:py-8 space-y-8">
          {/* HERO BANNER - Harmonized Monochrome */}
          <section className="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200/90 dark:border-slate-800 shadow-2xs p-6 lg:p-8 relative overflow-hidden">
            <div className="relative z-10 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
              <div>
                {/* Headline - Exactly matching StudentLandingPage font styling */}
                <h1 className="text-2xl font-bold text-slate-900 dark:text-white tracking-tight flex items-center gap-2">
                  Đánh giá phỏng vấn kỹ thuật
                </h1>

                <p className="text-slate-600 dark:text-slate-300 text-sm max-w-3xl leading-relaxed mt-2">
                  Phòng thi phỏng vấn trực tuyến. Đề thi gồm 10 câu bốc ngẫu nhiên (Dễ, Trung bình, Khó) với tính năng trả lời qua Micro hoặc gõ văn bản trực tiếp.
                </p>
              </div>

              <div className="shrink-0">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => refetchAssignments()}
                  className="bg-white dark:bg-slate-900 text-slate-700 dark:text-slate-200 border-slate-200 dark:border-slate-800 rounded-lg px-3.5 py-2 text-[13px] font-semibold hover:bg-slate-50 dark:hover:bg-slate-800 shadow-2xs"
                >
                  Làm mới danh sách
                </Button>
              </div>
            </div>
          </section>

          {/* Assignments List */}
          <div className="space-y-4">
            <div className="flex items-center justify-between">
              <h2 className="text-base font-semibold text-slate-900 dark:text-white tracking-tight">
                Các đợt đánh giá được phân công ({assignments.length})
              </h2>
            </div>

            {isLoadingAssignments ? (
              <div className="flex h-48 items-center justify-center">
                <Loader2 className="h-7 w-7 animate-spin text-slate-500" />
              </div>
            ) : assignments.length === 0 ? (
              <div className="rounded-2xl border border-dashed border-slate-300 bg-white p-12 text-center dark:border-slate-800 dark:bg-slate-900">
                <FileQuestion className="mx-auto h-12 w-12 text-slate-400" />
                <h3 className="mt-3 text-base font-semibold text-slate-800 dark:text-slate-200">
                  Hiện chưa có bài đánh giá nào
                </h3>
                <p className="mt-1 text-sm text-slate-500">
                  Lớp học của bạn hiện chưa có đợt phỏng vấn nào được mở. Vui lòng quay lại sau hoặc liên hệ giảng viên.
                </p>
              </div>
            ) : (
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-5">
                {assignments.map((item) => {
                  const status = item.mySessionStatus || "NOT_STARTED";
                  return (
                    <div
                      key={item.id}
                      className="group bg-white dark:bg-slate-900 border border-slate-200/90 dark:border-slate-800 rounded-2xl p-6 flex flex-col justify-between hover:border-slate-400 dark:hover:border-slate-600 hover:shadow-xs transition-all duration-200 hover:-translate-y-0.5"
                    >
                      <div>
                        {/* Top row: Icon Box & Badges */}
                        <div className="flex items-start justify-between gap-2 mb-3">
                          <div className="w-11 h-11 rounded-xl flex items-center justify-center bg-slate-100 text-slate-800 border border-slate-200 dark:bg-slate-800 dark:text-slate-200 dark:border-slate-700">
                            <Mic className="size-5" />
                          </div>

                          <div className="flex items-center gap-1.5 flex-wrap justify-end">
                            <span className="text-[11px] font-medium px-2 py-0.5 rounded-md border border-slate-200 bg-slate-50 text-slate-600 dark:border-slate-700 dark:bg-slate-800 dark:text-slate-300">
                              {item.topicName}
                            </span>
                            {status === "NOT_STARTED" && (
                              <span className="text-[11px] font-medium px-2 py-0.5 rounded-md border border-slate-200 bg-slate-50 text-slate-600 dark:border-slate-700 dark:bg-slate-800 dark:text-slate-400">
                                Chưa làm
                              </span>
                            )}
                            {status === "IN_PROGRESS" && (
                              <span className="text-[11px] font-semibold px-2 py-0.5 rounded-md bg-slate-900 text-white dark:bg-slate-100 dark:text-slate-900">
                                Đang làm dở
                              </span>
                            )}
                            {(status === "SUBMITTED" || status === "REVIEWED") && (
                              <span className="text-[11px] font-semibold px-2 py-0.5 rounded-md border border-slate-300 bg-slate-100 text-slate-800 dark:border-slate-700 dark:bg-slate-800 dark:text-slate-200">
                                Chờ công bố điểm
                              </span>
                            )}
                            {status === "PUBLISHED" && (
                              <span className="text-[11px] font-semibold px-2 py-0.5 rounded-md bg-slate-900 text-white dark:bg-slate-100 dark:text-slate-900">
                                Đã công bố điểm
                              </span>
                            )}
                          </div>
                        </div>

                        {/* Title - matching StudentLandingPage */}
                        <h3 className="text-base font-semibold text-slate-900 dark:text-white tracking-tight group-hover:text-slate-950 dark:group-hover:text-white transition-colors mb-2">
                          {item.title}
                        </h3>

                        {/* Description */}
                        {item.description && (
                          <p className="text-slate-600 dark:text-slate-400 text-[13px] leading-relaxed mb-4 line-clamp-2">
                            {item.description}
                          </p>
                        )}

                        {/* Metadata specs */}
                        <div className="space-y-1.5 border-t border-slate-100 dark:border-slate-800 pt-3 text-[13px] text-slate-600 dark:text-slate-400">
                          <div className="flex items-center justify-between">
                            <span className="flex items-center gap-1.5">
                              <Clock className="size-3.5 text-slate-400" /> Thời gian:
                            </span>
                            <span className="font-semibold text-slate-800 dark:text-slate-200">
                              {item.durationMinutes} phút
                            </span>
                          </div>
                          <div className="flex items-center justify-between">
                            <span className="flex items-center gap-1.5">
                              <Layers className="size-3.5 text-slate-400" /> Ma trận đề:
                            </span>
                            <span className="font-medium text-slate-800 dark:text-slate-200">
                              {item.easyCount} Dễ • {item.mediumCount} TB • {item.hardCount} Khó
                            </span>
                          </div>
                          <div className="flex items-center justify-between">
                            <span className="flex items-center gap-1.5">
                              <Award className="size-3.5 text-slate-400" /> Điểm chuẩn:
                            </span>
                            <span className="font-semibold text-slate-800 dark:text-slate-200">
                              {item.passingScore}/10
                            </span>
                          </div>
                          {item.mySessionStatus === "PUBLISHED" && item.myTotalScore !== null && item.myTotalScore !== undefined && (
                            <div className="flex items-center justify-between border-t border-slate-100 dark:border-slate-800 pt-1.5 text-slate-900 dark:text-white">
                              <span className="font-semibold text-[13px]">Điểm chính thức:</span>
                              <span className="text-sm font-bold">{item.myTotalScore.toFixed(1)}/10</span>
                            </div>
                          )}
                        </div>
                      </div>

                      {/* Card Action Footer */}
                      <div className="mt-5 pt-3 border-t border-slate-100 dark:border-slate-800">
                        {status === "NOT_STARTED" && (
                          <button
                            type="button"
                            onClick={() => startSessionMutation.mutate(item.id)}
                            disabled={startSessionMutation.isPending}
                            className="w-full bg-[#0b1220] hover:bg-slate-800 text-white dark:bg-slate-100 dark:text-slate-900 dark:hover:bg-slate-200 rounded-xl py-2.5 px-4 text-[13px] font-semibold transition-all shadow-xs flex items-center justify-center gap-1.5 cursor-pointer disabled:opacity-50"
                          >
                            {startSessionMutation.isPending ? (
                              <Loader2 className="size-4 animate-spin" />
                            ) : (
                              <Send className="size-3.5" />
                            )}
                            <span>Bắt đầu làm bài</span>
                          </button>
                        )}

                        {status === "IN_PROGRESS" && (
                          <button
                            type="button"
                            onClick={() => item.mySessionId && loadSession(item.mySessionId)}
                            className="w-full bg-[#0b1220] hover:bg-slate-800 text-white dark:bg-slate-100 dark:text-slate-900 rounded-xl py-2.5 px-4 text-[13px] font-semibold transition-all shadow-xs flex items-center justify-center gap-1.5 cursor-pointer"
                          >
                            <Clock className="size-3.5" />
                            <span>Tiếp tục làm bài</span>
                          </button>
                        )}

                        {(status === "SUBMITTED" || status === "REVIEWED") && (
                          <button
                            type="button"
                            onClick={() => item.mySessionId && loadSession(item.mySessionId)}
                            className="w-full border border-slate-200 dark:border-slate-800 text-slate-700 dark:text-slate-300 hover:bg-slate-50 dark:hover:bg-slate-800 rounded-xl py-2.5 px-4 text-[13px] font-semibold transition-all flex items-center justify-center gap-1.5 cursor-pointer"
                          >
                            <Clock className="size-3.5" />
                            <span>Xem bài làm đã nộp (Chờ điểm)</span>
                          </button>
                        )}

                        {status === "PUBLISHED" && (
                          <button
                            type="button"
                            onClick={() => item.mySessionId && loadSession(item.mySessionId)}
                            className="w-full bg-[#0b1220] hover:bg-slate-800 text-white dark:bg-slate-100 dark:text-slate-900 dark:hover:bg-slate-200 rounded-xl py-2.5 px-4 text-[13px] font-semibold transition-all shadow-xs flex items-center justify-center gap-1.5 cursor-pointer"
                          >
                            <Award className="size-3.5" />
                            <span>Xem kết quả đánh giá phỏng vấn</span>
                          </button>
                        )}
                      </div>
                    </div>
                  );
                })}
              </div>
            )}
          </div>
        </div>
      </MainLayout>
    );
  }

  // ==============================================================
  // RENDER 2: EXAM ROOM (Stitch AI Assessment Console)
  // ==============================================================
  if (viewMode === "EXAM_ROOM" && activeSession) {
    const totalQ = activeSession.questions.length;
    return (
      <div className="flex h-screen w-screen flex-col overflow-hidden bg-slate-50 text-slate-900 dark:bg-slate-950 dark:text-slate-100">
        {/* Fixed Exam Room Top Header */}
        <header className="flex h-16 shrink-0 items-center justify-between border-b border-slate-200 bg-white px-5 shadow-xs dark:border-slate-800 dark:bg-slate-900 z-30">
            {/* Left Cluster: Brand Logo (Matched with Home) + Exam Details */}
            <div className="flex items-center gap-3.5">
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

              <div className="h-6 w-px bg-slate-200 dark:bg-slate-800 hidden sm:block" />

              <div className="flex flex-col">
                <span className="text-sm font-bold tracking-tight text-slate-900 dark:text-white leading-tight">
                  {activeSession.assignmentTitle}
                </span>
                <span className="text-xs text-slate-500 dark:text-slate-400">
                  {activeSession.topicName}
                </span>
              </div>
            </div>

            {/* Center Cluster: Countdown Timer & Completion Progress */}
            <div className="flex items-center gap-3">
              <div className="flex items-center gap-2 rounded-lg border border-slate-200 bg-slate-50 px-3.5 py-1.5 shadow-inner dark:border-slate-800 dark:bg-slate-950">
                <Clock className={`h-4 w-4 ${remainingSeconds < 300 ? "animate-pulse text-slate-900 dark:text-white font-bold" : "text-slate-500"}`} />
                <div className="text-left leading-tight">
                  <div className="flex items-center gap-1.5">
                    <span className={`font-mono text-sm font-bold ${remainingSeconds < 300 ? "text-slate-950 dark:text-white" : "text-slate-900 dark:text-slate-100"}`}>
                      {formatTimer(remainingSeconds)}
                    </span>
                    <span className="text-[11px] text-slate-500">còn lại</span>
                  </div>
                  <div className="mt-1 h-1 w-28 overflow-hidden rounded-full bg-slate-200 dark:bg-slate-800">
                    <div
                      className="h-full rounded-full bg-slate-900 transition-all dark:bg-slate-100"
                      style={{ width: `${Math.round((answeredCount / totalQ) * 100)}%` }}
                    />
                  </div>
                </div>
              </div>
              <span className="hidden text-xs font-semibold text-slate-500 md:inline-block">
                {answeredCount}/{totalQ} hoàn thành ({Math.round((answeredCount / totalQ) * 100)}%)
              </span>
            </div>

            {/* Right Cluster: Actions */}
            <div className="flex items-center gap-3">
              <Button
                variant="outline"
                size="sm"
                onClick={() => {
                  if (window.confirm("Bạn có chắc muốn tạm thoát ra ngoài? Thời gian làm bài vẫn tiếp tục đếm ngược.")) {
                    stopListening();
                    setViewMode("LOBBY");
                    refetchAssignments();
                  }
                }}
                className="h-8 gap-1 border-slate-300 text-xs text-slate-700 hover:text-slate-900 dark:border-slate-700 dark:text-slate-300 dark:hover:text-white"
              >
                <ArrowLeft className="h-3.5 w-3.5" /> Tạm dừng
              </Button>

              <Button
                size="sm"
                onClick={() => setIsSubmitConfirmOpen(true)}
                className="h-8 bg-slate-900 text-xs font-semibold text-white hover:bg-slate-800 dark:bg-slate-100 dark:text-slate-900 dark:hover:bg-slate-200 shadow-xs cursor-pointer"
              >
                <Send className="mr-1.5 h-3.5 w-3.5" /> Nộp bài thi
              </Button>
            </div>
          </header>

          {/* Main Body */}
          <div className="flex flex-1 overflow-hidden relative">
            {/* Left Sidebar: Question Palette & Live Proctor Sync */}
            <aside className="w-72 shrink-0 border-r border-slate-200 bg-white flex flex-col justify-between overflow-y-auto dark:border-slate-800 dark:bg-slate-900">
              {/* Palette Header & Legend */}
              <div className="p-4 border-b border-slate-200 bg-slate-50/50 dark:border-slate-800 dark:bg-slate-900/60">
                <div className="flex items-center justify-between mb-1">
                  <span className="text-xs font-bold text-slate-900 dark:text-slate-100 uppercase tracking-wider">
                    Mục lục câu hỏi ({totalQ})
                  </span>
                  <span className="text-[11px] font-semibold px-2 py-0.5 rounded-full bg-slate-200 text-slate-800 dark:bg-slate-800 dark:text-slate-200">
                    {answeredCount}/{totalQ} Đã làm
                  </span>
                </div>
                <span className="text-[11px] text-slate-500 block">Danh mục câu hỏi bài thi phỏng vấn</span>

                {/* Status Legend (Circular Dots matching Figma) */}
                <div className="grid grid-cols-2 gap-1.5 mt-3 pt-2 text-[11px] text-slate-600 border-t border-slate-200 dark:border-slate-800 dark:text-slate-400">
                  <div className="flex items-center gap-1.5">
                    <span className="w-2.5 h-2.5 rounded-full bg-slate-900 dark:bg-slate-100 shrink-0" />
                    <span>Đã trả lời ({answeredCount})</span>
                  </div>
                  <div className="flex items-center gap-1.5">
                    <span className="w-2.5 h-2.5 rounded-full border-2 border-slate-900 bg-white dark:border-slate-100 dark:bg-slate-950 shrink-0" />
                    <span>Đang làm (1)</span>
                  </div>
                  <div className="flex items-center gap-1.5">
                    <span className="w-2.5 h-2.5 rounded-full bg-slate-400 dark:bg-slate-500 shrink-0" />
                    <span>Gắn cờ ({flaggedCount})</span>
                  </div>
                  <div className="flex items-center gap-1.5">
                    <span className="w-2.5 h-2.5 rounded-full border border-slate-300 bg-slate-100 dark:border-slate-700 dark:bg-slate-800 shrink-0" />
                    <span>Chưa làm ({totalQ - answeredCount})</span>
                  </div>
                </div>
              </div>

              {/* Question List Items */}
              <div className="flex-1 overflow-y-auto p-3 space-y-2">
                {activeSession.questions.map((q, idx) => {
                  const isAnswered = q.questionType === "MCQ" ? !!q.selectedOptionId : !!q.answerText?.trim();
                  const isCurrent = idx === currentQuestionIdx;
                  const isFlagged = q.isFlagged;

                  return (
                    <button
                      key={q.questionId}
                      type="button"
                      onClick={() => {
                        stopListening();
                        setCurrentQuestionIdx(idx);
                      }}
                      className={`w-full text-left p-2.5 rounded-xl border transition-all flex items-center justify-between group cursor-pointer ${
                        isCurrent
                          ? "border-2 border-slate-900 bg-white dark:border-slate-100 dark:bg-slate-800/90 shadow-xs"
                          : isFlagged
                          ? "border border-slate-300 bg-slate-100/70 hover:bg-slate-200/50 dark:border-slate-700 dark:bg-slate-800"
                          : isAnswered
                          ? "border border-slate-200 bg-slate-50/70 hover:bg-slate-100/80 dark:border-slate-800 dark:bg-slate-900/40 dark:hover:bg-slate-800/40"
                          : "border border-slate-200/80 bg-white hover:bg-slate-50/80 text-slate-500 dark:border-slate-800/80 dark:bg-slate-900/60 dark:hover:bg-slate-800/30"
                      }`}
                    >
                      <div className="flex items-center gap-2.5 overflow-hidden">
                        <span
                          className={`w-6 h-6 rounded-md text-xs font-mono font-bold flex items-center justify-center shrink-0 transition-colors ${
                            isCurrent
                              ? "bg-slate-900 text-white dark:bg-slate-100 dark:text-slate-900 shadow-xs"
                              : isFlagged
                              ? "bg-slate-600 text-white dark:bg-slate-400 dark:text-slate-900"
                              : isAnswered
                              ? "bg-slate-800 text-white dark:bg-slate-200 dark:text-slate-900"
                              : "text-slate-400 dark:text-slate-500 font-normal"
                          }`}
                        >
                          {isFlagged ? "⚑" : isAnswered ? "✓" : idx + 1}
                        </span>
                        <div className="truncate">
                          <span className={`text-xs block leading-tight truncate ${isCurrent ? "font-bold text-slate-900 dark:text-slate-100" : "font-semibold text-slate-700 dark:text-slate-300"}`}>
                            Câu {String(idx + 1).padStart(2, "0")}
                          </span>
                          <span className="text-[10px] text-slate-500 truncate block mt-0.5">
                            {q.questionType === "MCQ" ? "Trắc nghiệm" : "Tự luận"} • {q.difficulty}
                          </span>
                        </div>
                      </div>
                      <span className="text-[11px] font-mono font-semibold text-slate-600 dark:text-slate-400 shrink-0 ml-1">
                        1.0đ
                      </span>
                    </button>
                  );
                })}
              </div>
            </aside>

          {/* Main Workspace (Question Header + Arena + Navigation Footer) */}
          <main className="flex-1 flex flex-col overflow-hidden bg-slate-50 dark:bg-slate-950">
            {/* Top Question Header Card */}
            <div className="p-5 bg-white border-b border-slate-200 shadow-xs shrink-0 dark:bg-slate-900 dark:border-slate-800">
              <div className="flex items-center justify-between mb-2">
                <div className="flex items-center gap-2">
                  <Badge className="bg-slate-900 text-white font-mono text-xs dark:bg-slate-100 dark:text-slate-900">
                    Câu hỏi {String(currentQuestionIdx + 1).padStart(2, "0")} / {totalQ}
                  </Badge>
                  <Badge variant="outline" className="text-xs">
                    {currentQuestion?.questionType === "MCQ" ? "Trắc nghiệm (MCQ)" : "Tự luận / Trả lời thoại"}
                  </Badge>
                  <Badge variant="outline" className="text-xs">
                    Độ khó: {currentQuestion?.difficulty}
                  </Badge>
                  <Badge variant="outline" className="text-xs font-semibold">
                    1.0 Điểm
                  </Badge>
                </div>

                <Button
                  variant={activeFlag ? "default" : "outline"}
                  size="sm"
                  onClick={handleToggleFlag}
                  className={`h-7 gap-1 text-xs border-slate-300 ${activeFlag ? "bg-slate-900 text-white hover:bg-slate-800 border-slate-900 dark:bg-slate-100 dark:text-slate-900 dark:border-slate-100" : ""}`}
                >
                  <Flag className={`h-3 w-3 ${activeFlag ? "fill-white" : ""}`} />
                  <span>{activeFlag ? "Đã gắn cờ xem lại" : "Gắn cờ xem lại"}</span>
                </Button>
              </div>

              {/* Question Statement */}
              <h2 className="text-lg font-bold text-slate-900 dark:text-slate-100 leading-snug">
                {currentQuestion?.questionText}
              </h2>

              {/* Guidelines / Rubric box */}
              <div className="mt-3 rounded-lg border border-slate-200 bg-slate-50 p-3 text-xs text-slate-600 dark:border-slate-800 dark:bg-slate-950 dark:text-slate-400">
                <strong className="font-semibold text-slate-800 dark:text-slate-200">Hướng dẫn & Tiêu chuẩn đánh giá: </strong>
                {currentQuestion?.questionType === "MCQ" ? (
                  <span>Đọc kỹ câu hỏi và chọn 1 phương án chính xác nhất. Lựa chọn sẽ được hệ thống tự động lưu vào bài làm ngay khi chọn.</span>
                ) : (
                  <span>
                    Thí sinh có thể chọn <strong>Bật Micro để nói</strong> hoặc <strong>Nhập nội dung/code trực tiếp từ bàn phím</strong>. Trình bày rõ định nghĩa, nguyên lý hoạt động và ví dụ minh họa theo đúng trọng tâm câu hỏi.
                  </span>
                )}
              </div>
            </div>

            {/* Question Workspace (MCQ or Voice/Text Dual Arena) */}
            <div className="flex-1 overflow-y-auto p-5">
              {currentQuestion?.questionType === "MCQ" ? (
                /* MCQ Workspace */
                <div className="max-w-3xl space-y-3">
                  <span className="text-xs font-semibold uppercase tracking-wider text-slate-500">
                    Lựa chọn đáp án đúng:
                  </span>
                  <div className="space-y-3">
                    {parsedOptions.map((opt) => {
                      const isSelected = activeSelectedOption === opt.id;
                      return (
                        <button
                          key={opt.id}
                          type="button"
                          onClick={() => handleSelectOption(opt.id)}
                          className={`flex w-full items-start gap-3 rounded-xl border p-4 text-left transition-all ${
                            isSelected
                              ? "border-slate-900 bg-white shadow-sm ring-2 ring-slate-900 dark:border-slate-100 dark:bg-slate-900 dark:ring-slate-100"
                              : "border-slate-200 bg-white hover:border-slate-400 dark:border-slate-800 dark:bg-slate-900 dark:hover:border-slate-700"
                          }`}
                        >
                          <div
                            className={`flex h-7 w-7 shrink-0 items-center justify-center rounded-full border text-xs font-bold ${
                              isSelected
                                ? "border-slate-900 bg-slate-900 text-white dark:border-slate-100 dark:bg-slate-100 dark:text-slate-900"
                                : "border-slate-300 text-slate-600 dark:border-slate-700 dark:text-slate-400"
                            }`}
                          >
                            {opt.id}
                          </div>
                          <span className="text-sm font-medium text-slate-900 dark:text-slate-100 leading-relaxed pt-0.5">
                            {opt.text}
                          </span>
                        </button>
                      );
                    })}
                  </div>
                </div>
              ) : (
                /* Essay / Voice / Code Dual Bento Workspace */
                <div className="grid h-full grid-cols-1 lg:grid-cols-12 gap-5">
                  {/* Panel A: Voice Telemetry (5 cols) */}
                  <section className="lg:col-span-5 flex flex-col rounded-xl border border-slate-200 bg-white shadow-xs overflow-hidden dark:border-slate-800 dark:bg-slate-900">
                    <div className="px-4 py-2.5 border-b border-slate-200 bg-slate-50 flex items-center justify-between dark:border-slate-800 dark:bg-slate-900/80">
                      <div className="flex items-center gap-2">
                        <Mic className="h-4 w-4 text-slate-800 dark:text-slate-200" />
                        <h3 className="text-xs font-bold text-slate-900 dark:text-slate-100">
                          Voice Telemetry (Ghi âm)
                        </h3>
                      </div>
                      <Badge
                        variant={isListening ? "default" : "outline"}
                        className={`text-[10px] ${
                          isListening ? "bg-slate-900 text-white animate-pulse dark:bg-slate-100 dark:text-slate-900" : ""
                        }`}
                      >
                        {isListening ? "Đang lắng nghe..." : "Sẵn sàng"}
                      </Badge>
                    </div>

                    <div className="p-4 border-b border-slate-200 bg-slate-50/50 space-y-3 dark:border-slate-800 dark:bg-slate-950/40">
                      {/* Animated Sound Wave Visualizer */}
                      <div className="flex h-12 items-center justify-center gap-1 overflow-hidden rounded-lg border border-slate-200 bg-white px-4 shadow-inner dark:border-slate-800 dark:bg-slate-950">
                        {[25, 45, 75, 35, 90, 55, 80, 40, 100, 60, 85, 30, 70, 50, 65, 35, 95].map((h, i) => (
                          <span
                            key={i}
                            className={`w-1 rounded-full transition-all duration-200 ${
                              isListening
                                ? "bg-slate-900 dark:bg-slate-100 animate-pulse"
                                : "bg-slate-300 dark:bg-slate-700"
                            }`}
                            style={{
                              height: isListening ? `${Math.max(10, Math.min(38, h * 0.4))}px` : "6px",
                              animationDelay: `${(i % 5) * 0.12}s`,
                            }}
                          />
                        ))}
                      </div>

                      {/* Voice Action Controls */}
                      <div className="flex items-center justify-between gap-2">
                        <Button
                          type="button"
                          size="sm"
                          onClick={toggleListening}
                          className={`h-8 gap-1.5 text-xs font-semibold ${
                            isListening
                              ? "bg-slate-900 text-white hover:bg-slate-800 dark:bg-slate-100 dark:text-slate-900"
                              : "border border-slate-300 bg-white text-slate-800 hover:bg-slate-100 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200"
                          }`}
                        >
                          {isListening ? <MicOff className="h-3.5 w-3.5" /> : <Mic className="h-3.5 w-3.5" />}
                          {isListening ? "Dừng ghi âm" : "Nói qua Micro"}
                        </Button>

                        <div className="flex items-center gap-1.5">
                          <select
                            value={speechLang}
                            onChange={(e) => setSpeechLang(e.target.value as any)}
                            disabled={isListening}
                            aria-label="Ngôn ngữ nhận diện giọng nói"
                            className="h-8 rounded-md border border-slate-200 bg-white px-2 text-xs text-slate-700 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-300"
                          >
                            <option value="vi-VN">Tiếng Việt</option>
                            <option value="en-US">English</option>
                          </select>
                        </div>
                      </div>
                    </div>

                    {/* Speech Transcript Output */}
                    <div className="flex-1 p-4 flex flex-col min-h-[140px]">
                      <div className="flex items-center justify-between mb-1.5 text-xs text-slate-500">
                        <span className="font-semibold text-slate-700 dark:text-slate-300 flex items-center gap-1">
                          <FileText className="h-3.5 w-3.5" /> Bản ghi nhận dạng thời gian thực:
                        </span>
                        {isListening && (
                          <span className="text-[10px] text-slate-900 dark:text-slate-100 font-semibold animate-pulse">
                            ● Live Transcribing
                          </span>
                        )}
                      </div>
                      <div className="flex-1 rounded-lg border border-slate-200 bg-slate-50 p-3 text-xs text-slate-700 leading-relaxed overflow-y-auto italic dark:border-slate-800 dark:bg-slate-950 dark:text-slate-300">
                        {interimTranscript ? (
                          <span>{interimTranscript}</span>
                        ) : isListening ? (
                          <span className="text-slate-400">Đang lắng nghe giọng nói... Hãy trình bày câu trả lời của bạn.</span>
                        ) : (
                          <span className="text-slate-400">Bấm nút "Nói qua Micro" để bắt đầu trả lời bằng giọng nói. Hệ thống sẽ tự động chuyển âm thanh thành văn bản.</span>
                        )}
                      </div>
                    </div>
                  </section>

                  {/* Panel B: Text / Code Editor (7 cols) */}
                  <section className="lg:col-span-7 flex flex-col rounded-xl border border-slate-200 bg-white shadow-xs overflow-hidden dark:border-slate-800 dark:bg-slate-900">
                    <div className="px-4 py-2 border-b border-slate-200 bg-slate-50 flex items-center justify-between dark:border-slate-800 dark:bg-slate-900/80">
                      <div className="flex items-center gap-2">
                        <Button
                          type="button"
                          size="sm"
                          variant={activeMode === "TEXT" ? "default" : "ghost"}
                          onClick={() => {
                            stopListening();
                            setActiveMode("TEXT");
                            triggerAutoSave({ mode: "TEXT" });
                          }}
                          className="h-7 gap-1 text-xs"
                        >
                          <Keyboard className="h-3 w-3" /> Văn bản / Trả lời
                        </Button>
                        <Button
                          type="button"
                          size="sm"
                          variant="ghost"
                          onClick={() => {
                            stopListening();
                            setActiveMode("TEXT");
                          }}
                          className="h-7 gap-1 text-xs text-slate-600 dark:text-slate-400"
                        >
                          <Code className="h-3 w-3" /> Code Snippet
                        </Button>
                      </div>

                      <div className="flex items-center gap-2 text-xs text-slate-500">
                        <span className="flex items-center gap-1 text-[11px]">
                          <span className={`h-1.5 w-1.5 rounded-full ${isAutoSaving ? "bg-slate-400 animate-ping" : "bg-slate-900 dark:bg-slate-100"}`} />
                          {isAutoSaving ? "Đang lưu..." : "Đã lưu (Cloud Sync)"}
                        </span>
                      </div>
                    </div>

                    {/* Textarea Editor */}
                    <div className="flex flex-1 flex-col p-4">
                      <Textarea
                        value={activeAnswerText}
                        onChange={(e) => {
                          const text = e.target.value;
                          setActiveAnswerText(text);
                          triggerAutoSave({ text });
                        }}
                        placeholder="Nhập chi tiết câu trả lời phỏng vấn hoặc dán đoạn mã minh họa của bạn tại đây..."
                        className="flex-1 min-h-[220px] resize-none rounded-lg border-slate-200 bg-slate-50 p-4 font-mono text-xs leading-relaxed text-slate-900 shadow-inner focus:border-slate-400 focus:bg-white dark:border-slate-800 dark:bg-slate-950 dark:text-slate-100"
                      />
                      <div className="mt-2.5 flex items-center justify-between text-[11px] text-slate-500">
                        <span>
                          {activeAnswerText.trim() ? activeAnswerText.trim().split(/\s+/).length : 0} từ • {activeAnswerText.length} ký tự
                        </span>
                        <span className="text-slate-400">
                          Hệ thống ghi nhận và lưu trữ câu trả lời trực tiếp khi nộp bài
                        </span>
                      </div>
                    </div>
                  </section>
                </div>
              )}
            </div>

            {/* Sticky Bottom Navigation Footer */}
            <footer className="h-14 shrink-0 border-t border-slate-200 bg-white px-6 flex items-center justify-between dark:border-slate-800 dark:bg-slate-900 z-20">
              <Button
                variant="outline"
                size="sm"
                disabled={currentQuestionIdx === 0}
                onClick={() => {
                  stopListening();
                  setCurrentQuestionIdx((prev) => Math.max(0, prev - 1));
                }}
                className="gap-1 border-slate-300 text-xs"
              >
                <ChevronLeft className="h-4 w-4" /> Câu trước
              </Button>

              <div className="hidden sm:flex items-center gap-2 text-xs text-slate-500">
                <span>
                  Đang xem câu <strong>{currentQuestionIdx + 1}</strong> trong tổng số <strong>{totalQ}</strong> câu
                </span>
              </div>

              {currentQuestionIdx < totalQ - 1 ? (
                <Button
                  size="sm"
                  onClick={() => {
                    stopListening();
                    setCurrentQuestionIdx((prev) => Math.min(totalQ - 1, prev + 1));
                  }}
                  className="gap-1 bg-slate-900 text-xs font-semibold text-white hover:bg-slate-800 dark:bg-slate-100 dark:text-slate-900"
                >
                  Lưu & Câu tiếp theo <ChevronRight className="h-4 w-4" />
                </Button>
              ) : (
                <Button
                  size="sm"
                  onClick={() => setIsSubmitConfirmOpen(true)}
                  className="gap-1 bg-slate-900 text-xs font-semibold text-white hover:bg-slate-800 dark:bg-slate-100 dark:text-slate-900"
                >
                  Xem lại & Nộp bài <Send className="h-3.5 w-3.5" />
                </Button>
              )}
            </footer>
          </main>
        </div>

        {/* Submit Confirmation Dialog */}
        <Dialog open={isSubmitConfirmOpen} onOpenChange={setIsSubmitConfirmOpen}>
          <DialogContent className="border-slate-200 p-6 dark:border-slate-800 dark:bg-slate-900">
            <DialogHeader>
              <DialogTitle className="text-xl font-bold text-slate-900 dark:text-slate-100">
                Xác Nhận Nộp Bài Thi Phỏng Vấn
              </DialogTitle>
              <DialogDescription className="text-slate-600 dark:text-slate-400">
                Sau khi nộp bài, hệ thống sẽ lưu kết quả và gửi bài thi để đánh giá.
              </DialogDescription>
            </DialogHeader>

            <div className="my-4 space-y-3 rounded-lg border border-slate-200 bg-slate-50 p-4 text-sm dark:border-slate-800 dark:bg-slate-950">
              <div className="flex items-center justify-between">
                <span className="text-slate-600 dark:text-slate-400">Tổng số câu hỏi:</span>
                <span className="font-bold text-slate-900 dark:text-slate-100">{totalQ} câu</span>
              </div>
              <div className="flex items-center justify-between">
                <span className="text-slate-600 dark:text-slate-400">Số câu đã trả lời:</span>
                <span className="font-bold text-slate-900 dark:text-slate-100">
                  {answeredCount} / {totalQ} câu
                </span>
              </div>
              <div className="flex items-center justify-between">
                <span className="text-slate-600 dark:text-slate-400">Thời gian còn lại:</span>
                <span className="font-mono font-bold text-slate-900 dark:text-slate-100">
                  {formatTimer(remainingSeconds)}
                </span>
              </div>

              {answeredCount < totalQ && (
                <div className="mt-2 flex items-center gap-2 rounded border border-slate-300 bg-white p-2.5 text-xs font-medium text-slate-800 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200">
                  <AlertCircle className="h-4 w-4 shrink-0 text-slate-600 dark:text-slate-400" />
                  <span>
                    Bạn còn <strong>{totalQ - answeredCount} câu hỏi</strong> chưa hoàn thành câu trả lời.
                  </span>
                </div>
              )}
            </div>

            <DialogFooter className="gap-2 sm:justify-end">
              <Button variant="outline" onClick={() => setIsSubmitConfirmOpen(false)} disabled={isSubmitting}>
                Quay lại làm tiếp
              </Button>
              <Button
                disabled={isSubmitting}
                onClick={handleSubmitExam}
                className="bg-slate-900 text-white hover:bg-slate-800 dark:bg-slate-100 dark:text-slate-900 dark:hover:bg-slate-200"
              >
                {isSubmitting ? (
                  <>
                    <Loader2 className="mr-2 h-4 w-4 animate-spin" /> Đang nộp bài...
                  </>
                ) : (
                  <>
                    <Send className="mr-2 h-4 w-4" /> Xác nhận nộp bài
                  </>
                )}
              </Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>
      </div>
    );
  }

  // ==============================================================
  // RENDER 3: SCORECARD (Candidate Results & AI Report)
  // ==============================================================
  if (viewMode === "SCORECARD" && activeSession && reportData) {
    const isPublished = activeSession.status === "PUBLISHED";
    const finalScore = reportData ? (reportData.totalScorePercentage / 10) : (activeSession.totalScore ?? activeSession.aiAverageScore ?? 0);
    const isPassed = reportData.completionRate >= 50 && finalScore >= (activeSession.passingScore || 7.0) && activeSession.overallVerdict === "PASS";

    return (
      <MainLayout pathName={{ "mock-interview": "Báo cáo đánh giá phỏng vấn" }}>
        <div className="max-w-[1400px] mx-auto space-y-6 pb-12">
          {/* BEGIN: TopNavigation */}
          <header className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4 py-2 no-print">
            <button
              type="button"
              onClick={() => {
                setViewMode("LOBBY");
                refetchAssignments();
              }}
              className="inline-flex items-center text-sm font-medium text-slate-700 hover:text-slate-900 dark:text-slate-300 dark:hover:text-white transition-colors cursor-pointer"
            >
              <ArrowLeft className="w-4 h-4 mr-2" />
              Quay lại danh sách bài thi
            </button>

            <div className="flex items-center gap-3 self-end sm:self-auto">
              <span className="text-xs font-semibold text-slate-700 dark:text-slate-300 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 px-3 py-1.5 rounded-md shadow-xs">
                Mã bài thi #{activeSession.sessionId}
              </span>
              {isPublished ? (
                <span className="text-xs font-medium text-white bg-[#0f172a] dark:bg-slate-100 dark:text-slate-900 px-3.5 py-1.5 rounded-full shadow-xs">
                  Đã công bố điểm
                </span>
              ) : (
                <span className="text-xs font-medium text-slate-700 bg-slate-100 dark:bg-slate-800 dark:text-slate-300 px-3.5 py-1.5 rounded-full border border-slate-300 dark:border-slate-700">
                  Chờ công bố điểm
                </span>
              )}
              {isPublished && (
                <button
                  type="button"
                  onClick={() => window.print()}
                  className="inline-flex items-center gap-2 text-xs font-medium text-slate-700 dark:text-slate-200 bg-white dark:bg-slate-900 border border-slate-200 dark:border-slate-800 px-3.5 py-1.5 rounded-md shadow-xs hover:bg-slate-50 dark:hover:bg-slate-800 transition-colors cursor-pointer"
                >
                  <Printer className="w-3.5 h-3.5 text-slate-600 dark:text-slate-400" />
                  In / Tải Báo Cáo PDF
                </button>
              )}
            </div>
          </header>
          {/* END: TopNavigation */}

          {!isPublished ? (
            /* ==============================================================
               CASE A: PENDING REVIEW / SCORE NOT PUBLISHED YET
               Scores and AI feedback are hidden from the student.
               ============================================================== */
            <>
              {/* Submission Pending Hero Banner */}
              <div className="rounded-2xl border border-slate-200/90 bg-white p-8 shadow-xs dark:border-slate-800 dark:bg-slate-900">
                <div className="flex flex-col items-center gap-4 text-center">
                  <div className="flex h-16 w-16 items-center justify-center rounded-full bg-slate-100 text-slate-900 dark:bg-slate-800 dark:text-slate-100">
                    <CheckCircle2 className="h-8 w-8" />
                  </div>

                  <div className="space-y-2">
                    <Badge variant="outline" className="border-slate-300 px-3 py-1 text-xs font-medium text-slate-700 dark:border-slate-700 dark:text-slate-300">
                      Bài thi đã nộp thành công
                    </Badge>
                    <h1 className="text-2xl font-bold text-slate-900 dark:text-white tracking-tight">
                      {activeSession.assignmentTitle}
                    </h1>
                    <p className="mx-auto max-w-xl text-sm leading-relaxed text-slate-600 dark:text-slate-400">
                      Hệ thống đã tiếp nhận toàn bộ câu trả lời của bạn. Điểm số chính thức và nhận xét chi tiết sẽ hiển thị tại đây ngay sau khi Giảng viên phụ trách hoàn tất chấm bài và công bố kết quả.
                    </p>
                  </div>

                  <div className="mt-4 grid w-full max-w-2xl grid-cols-1 gap-3 sm:grid-cols-3">
                    <div className="rounded-xl border border-slate-200 bg-slate-50 p-4 text-center dark:border-slate-800 dark:bg-slate-950">
                      <span className="text-xs text-slate-500">Học viên</span>
                      <p className="mt-1 font-semibold text-slate-900 dark:text-slate-100">{activeSession.studentName}</p>
                    </div>
                    <div className="rounded-xl border border-slate-200 bg-slate-50 p-4 text-center dark:border-slate-800 dark:bg-slate-950">
                      <span className="text-xs text-slate-500">Trạng thái điểm</span>
                      <p className="mt-1 font-semibold text-slate-900 dark:text-slate-100">
                        {activeSession.status === "REVIEWED" ? "Đã chấm - Chờ công bố" : "Đang chờ Giảng viên chấm"}
                      </p>
                      <p className="text-xs text-slate-500">Điểm chưa công bố</p>
                    </div>
                    <div className="rounded-xl border border-slate-200 bg-slate-50 p-4 text-center dark:border-slate-800 dark:bg-slate-950">
                      <span className="text-xs text-slate-500">Thời gian nộp</span>
                      <p className="mt-1 font-semibold text-slate-900 dark:text-slate-100">
                        {activeSession.submittedAt
                          ? new Date(activeSession.submittedAt).toLocaleTimeString("vi-VN", {
                              hour: "2-digit",
                              minute: "2-digit",
                              day: "2-digit",
                              month: "2-digit",
                            })
                          : "Vừa nộp"}
                      </p>
                      <p className="text-xs text-slate-500">{activeSession.questions.length} câu đã hoàn tất</p>
                    </div>
                  </div>
                </div>
              </div>

              {/* Readonly Review of Submitted Answers (No scores, no correct answers) */}
              <div className="space-y-4">
                <div className="flex items-center justify-between">
                  <h2 className="text-base font-semibold text-slate-900 dark:text-white tracking-tight">
                    Xem lại bài làm đã nộp ({activeSession.questions.length} câu)
                  </h2>
                  <span className="text-xs text-slate-500">Điểm và nhận xét sẽ mở khi có công bố chính thức</span>
                </div>

                <div className="space-y-4">
                  {activeSession.questions.map((q, idx) => (
                    <div
                      key={q.questionId}
                      className="rounded-2xl border border-slate-200/90 bg-white p-6 shadow-xs dark:border-slate-800 dark:bg-slate-900"
                    >
                      <div className="flex flex-wrap items-center justify-between gap-2 border-b border-slate-100 pb-3 dark:border-slate-800">
                        <div className="flex items-center gap-2">
                          <span className="text-base font-semibold text-slate-900 dark:text-slate-100">Câu {idx + 1}.</span>
                          <Badge variant="outline" className="text-xs">
                            {q.questionType === "MCQ" ? "Trắc nghiệm" : "Tự luận"}
                          </Badge>
                          <Badge variant="outline" className="text-xs">
                            {q.difficulty}
                          </Badge>
                        </div>
                        <Badge variant="outline" className="text-xs text-slate-600 dark:text-slate-400">
                          Đã lưu câu trả lời
                        </Badge>
                      </div>

                      <p className="mt-3 text-sm font-semibold text-slate-900 dark:text-slate-100">{q.questionText}</p>

                      <div className="mt-4 rounded-xl bg-slate-50 p-4 dark:bg-slate-950 border border-slate-100 dark:border-slate-800">
                        <span className="text-xs font-semibold text-slate-500">Câu trả lời bạn đã nộp:</span>
                        {q.questionType === "MCQ" ? (
                          <p className="mt-1 font-mono text-sm font-semibold text-slate-900 dark:text-slate-100">
                            Đáp án đã chọn: {q.selectedOptionId || "(Không chọn)"}
                          </p>
                        ) : (
                          <p className="mt-1 whitespace-pre-wrap text-[13px] leading-relaxed text-slate-800 dark:text-slate-200">
                            {q.answerText || "(Bỏ trống câu này)"}
                          </p>
                        )}
                      </div>
                    </div>
                  ))}
                </div>
              </div>
            </>
          ) : (
            /* ==============================================================
               CASE B: SCORES OFFICIALLY PUBLISHED BY TEACHER
               Candidate Interview Performance & Proficiency Report
               ============================================================== */
            <>
              {/* Inline Print Stylesheet */}
              <style className="hidden">{`
                @media print {
                  body { background: white !important; color: black !important; }
                  .no-print, header, footer, nav, aside { display: none !important; }
                  .max-w-\\[1440px\\] { max-width: 100% !important; padding: 0 !important; margin: 0 !important; }
                  .shadow-xs, .shadow-sm, .shadow-md, .shadow-2xs { box-shadow: none !important; }
                  .border { border-color: #cbd5e1 !important; }
                }
              `}</style>

              {/* BEGIN: Hero Assessment Card (STITCH DESIGN) */}
              <section className="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200/80 dark:border-slate-800 p-6 sm:p-8 shadow-xs">
                <div className="flex flex-col lg:flex-row justify-between items-start lg:items-center gap-6 lg:gap-8">
                  {/* Left Details */}
                  <div className="space-y-3.5 max-w-3xl">
                    <div className="flex flex-wrap items-center gap-2.5 text-xs">
                      <span className="font-medium text-slate-700 dark:text-slate-300 border border-slate-300 dark:border-slate-700 rounded-full px-3 py-0.5">
                        {isPassed ? "Kết quả: Đạt tiêu chuẩn" : "Kết quả: Chưa đạt"}
                      </span>
                      <span className="text-slate-500 dark:text-slate-400 font-normal">
                        • Điểm chuẩn qua môn: {activeSession.passingScore || 7.0}/10
                      </span>
                    </div>

                    <h1 className="text-2xl font-bold text-slate-900 dark:text-slate-100 tracking-tight leading-snug">
                      Báo Cáo Đánh Giá Năng Lực &amp; Trình Độ Phỏng Vấn
                    </h1>
                    <p className="text-sm font-semibold text-slate-800 dark:text-slate-200">
                      {activeSession.assignmentTitle}
                    </p>
                    <div className="text-xs text-slate-600 dark:text-slate-400 flex flex-wrap items-center gap-2 pt-1">
                      <span>Học viên: <strong className="text-slate-900 dark:text-white font-bold">{activeSession.studentName}</strong></span>
                      <span className="text-slate-300 dark:text-slate-700">•</span>
                      <span>Lớp: <span className="text-slate-700 dark:text-slate-300">{activeSession.className || "Fresher Java Spring Boot 01"}</span></span>
                      <span className="text-slate-300 dark:text-slate-700">•</span>
                      <span>Thẩm định ngày: <span className="text-slate-700 dark:text-slate-300">
                        {activeSession.submittedAt
                          ? new Date(activeSession.submittedAt).toLocaleDateString("vi-VN", {
                              day: "2-digit",
                              month: "2-digit",
                              year: "numeric",
                            })
                          : "16/11/2026"}
                      </span></span>
                    </div>
                  </div>

                  {/* Right: Big Total Score Card */}
                  <div className="w-full sm:w-auto self-stretch lg:self-auto flex justify-center lg:justify-end">
                    <div className="w-full sm:w-56 border border-slate-200 dark:border-slate-800 rounded-2xl p-5 text-center bg-white dark:bg-slate-950 flex flex-col justify-center items-center shadow-2xs">
                      <span className="text-xs font-medium text-slate-500 dark:text-slate-400 block mb-1">
                        Tổng điểm đạt được
                      </span>
                      <span className="text-4xl sm:text-5xl font-extrabold text-slate-900 dark:text-white tracking-tight my-1">
                        {finalScore.toFixed(1)}
                      </span>
                      <span className="text-xs text-slate-500 font-medium">
                        Thang điểm 10 ({reportData.totalScorePercentage}%)
                      </span>
                    </div>
                  </div>
                </div>
              </section>
              {/* END: Hero Assessment Card */}

              {/* BEGIN: Key Metrics Grid (3 Bento Cards - STITCH DESIGN) */}
              <section className="grid grid-cols-1 md:grid-cols-3 gap-6">
                {/* Metric 1: TỶ LỆ ĐIỂM NĂNG LỰC */}
                <article className="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200/80 dark:border-slate-800 p-6 flex flex-col justify-between shadow-xs min-h-[220px]">
                  <div>
                    <div className="flex items-center justify-between mb-4">
                      <h3 className="text-xs font-medium text-slate-600 dark:text-slate-400">
                        Tỷ lệ điểm năng lực
                      </h3>
                      <Target className="w-4 h-4 text-slate-400" />
                    </div>
                    <div className="flex items-baseline gap-2 mb-4">
                      <span className="text-2xl font-bold text-slate-900 dark:text-white">
                        {reportData.totalScorePercentage}%
                      </span>
                      <span className="text-xs text-slate-500 font-medium">
                        ({finalScore.toFixed(1)} / 10 điểm)
                      </span>
                    </div>
                    <div className="w-full bg-slate-100 dark:bg-slate-800 rounded-full h-1.5 overflow-hidden">
                      <div
                        className="bg-slate-900 dark:bg-slate-100 h-1.5 rounded-full transition-all duration-500"
                        style={{ width: `${Math.min(100, Math.max(0, reportData.totalScorePercentage))}%` }}
                      />
                    </div>
                  </div>
                  <p className="text-xs text-slate-500 dark:text-slate-400 pt-6">
                    Điểm đạt chuẩn yêu cầu: {(activeSession.passingScore || 7.0) * 10}% ({activeSession.passingScore || 7.0}/10).
                  </p>
                </article>

                {/* Metric 2: CẤP BẬC TRÌNH ĐỘ */}
                <article className="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200/80 dark:border-slate-800 p-6 flex flex-col justify-between shadow-xs min-h-[220px]">
                  <div>
                    <div className="flex items-center justify-between mb-3">
                      <h3 className="text-xs font-medium text-slate-600 dark:text-slate-400">
                        Cấp bậc trình độ
                      </h3>
                      <Bookmark className="w-4 h-4 text-slate-400" />
                    </div>
                    <div className="mb-3">
                      <span className="inline-block bg-[#0f172a] dark:bg-slate-100 text-white dark:text-slate-900 text-xs font-semibold px-3 py-1 rounded-full">
                        {reportData.proficiencyRankTitle}
                      </span>
                    </div>
                    <p className="text-xs text-slate-600 dark:text-slate-300 leading-relaxed">
                      {reportData.proficiencySummary}
                    </p>
                  </div>
                  <div className="flex items-start gap-2 pt-4 border-t border-slate-100 dark:border-slate-800 text-xs text-slate-500 dark:text-slate-400">
                    <Building2 className="w-4 h-4 text-slate-400 shrink-0 mt-0.5" />
                    <span className="leading-tight">
                      OJT: {reportData.ojtRecommendation}
                    </span>
                  </div>
                </article>

                {/* Metric 3: TỶ LỆ HOÀN THÀNH */}
                <article className="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200/80 dark:border-slate-800 p-6 flex flex-col justify-between shadow-xs min-h-[220px]">
                  <div>
                    <div className="flex items-center justify-between mb-4">
                      <h3 className="text-xs font-medium text-slate-600 dark:text-slate-400">
                        Tỷ lệ hoàn thành
                      </h3>
                      <TrendingUp className="w-4 h-4 text-slate-400" />
                    </div>
                    <div className="flex items-baseline gap-2 mb-4">
                      <span className="text-2xl font-bold text-slate-900 dark:text-white">
                        {reportData.completionRate}%
                      </span>
                      <span className="text-xs text-slate-500 font-medium">
                        ({reportData.answeredQuestions}/{reportData.totalQuestions} câu)
                      </span>
                    </div>
                    <div className="w-full bg-slate-100 dark:bg-slate-800 rounded-full h-1.5 overflow-hidden">
                      <div
                        className="bg-slate-900 dark:bg-slate-100 h-1.5 rounded-full transition-all duration-500"
                        style={{ width: `${Math.min(100, Math.max(0, reportData.completionRate))}%` }}
                      />
                    </div>
                  </div>
                  <p className="text-xs text-slate-500 dark:text-slate-400 pt-6">
                    Ứng viên đã trả lời {reportData.answeredQuestions} trên tổng số {reportData.totalQuestions} câu hỏi bốc thăm.
                  </p>
                </article>
              </section>
              {/* END: Key Metrics Grid */}

              {/* BEGIN: Detailed Breakdown Grid (2 Columns - STITCH DESIGN) */}
              <section className="grid grid-cols-1 md:grid-cols-2 gap-6">
                {/* Breakdown 1: Ma Trận Độ Khó */}
                <article className="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200/80 dark:border-slate-800 p-6 shadow-xs">
                  <div className="flex items-center justify-between mb-6">
                    <div className="flex items-center gap-2">
                      <Layers className="w-4 h-4 text-slate-500" />
                      <h2 className="text-sm font-semibold text-slate-900 dark:text-white">
                        Phân tích theo ma trận độ khó
                      </h2>
                    </div>
                    <span className="text-xs text-slate-400 font-normal">3 Cấp độ</span>
                  </div>
                  <div className="space-y-5">
                    <div>
                      <div className="flex justify-between items-center text-xs mb-1.5">
                        <span className="font-medium text-slate-700 dark:text-slate-300">Mức Dễ ({reportData.easyCount} câu)</span>
                        <span className="font-semibold text-slate-900 dark:text-white">{reportData.easyScorePercent}%</span>
                      </div>
                      <div className="w-full bg-slate-100 dark:bg-slate-800 rounded-full h-1.5 overflow-hidden">
                        <div className="bg-slate-900 dark:bg-slate-100 h-1.5 rounded-full" style={{ width: `${reportData.easyScorePercent}%` }} />
                      </div>
                    </div>
                    <div>
                      <div className="flex justify-between items-center text-xs mb-1.5">
                        <span className="font-medium text-slate-700 dark:text-slate-300">Mức Trung bình ({reportData.mediumCount} câu)</span>
                        <span className="font-semibold text-slate-900 dark:text-white">{reportData.mediumScorePercent}%</span>
                      </div>
                      <div className="w-full bg-slate-100 dark:bg-slate-800 rounded-full h-1.5 overflow-hidden">
                        <div className="bg-slate-900 dark:bg-slate-100 h-1.5 rounded-full" style={{ width: `${reportData.mediumScorePercent}%` }} />
                      </div>
                    </div>
                    <div>
                      <div className="flex justify-between items-center text-xs mb-1.5">
                        <span className="font-medium text-slate-700 dark:text-slate-300">Mức Khó ({reportData.hardCount} câu)</span>
                        <span className="font-semibold text-slate-900 dark:text-white">{reportData.hardScorePercent}%</span>
                      </div>
                      <div className="w-full bg-slate-100 dark:bg-slate-800 rounded-full h-1.5 overflow-hidden">
                        <div className="bg-slate-900 dark:bg-slate-100 h-1.5 rounded-full" style={{ width: `${reportData.hardScorePercent}%` }} />
                      </div>
                    </div>
                  </div>
                </article>

                {/* Breakdown 2: Hình Thức Khảo Thí */}
                <article className="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200/80 dark:border-slate-800 p-6 shadow-xs">
                  <div className="flex items-center justify-between mb-6">
                    <div className="flex items-center gap-2">
                      <Code className="w-4 h-4 text-slate-500" />
                      <h2 className="text-sm font-semibold text-slate-900 dark:text-white">
                        Phân bổ theo hình thức khảo thí
                      </h2>
                    </div>
                    <span className="text-xs text-slate-400 font-normal">Trắc nghiệm &amp; Tự luận</span>
                  </div>
                  <div className="space-y-5">
                    <div>
                      <div className="flex justify-between items-center text-xs mb-1.5">
                        <span className="font-medium text-slate-700 dark:text-slate-300">Trắc nghiệm (Đúng {reportData.mcqCorrect}/{reportData.mcqTotal} câu)</span>
                        <span className="font-semibold text-slate-900 dark:text-white">{reportData.mcqAccuracyRate}%</span>
                      </div>
                      <div className="w-full bg-slate-100 dark:bg-slate-800 rounded-full h-1.5 overflow-hidden">
                        <div className="bg-slate-900 dark:bg-slate-100 h-1.5 rounded-full" style={{ width: `${reportData.mcqAccuracyRate}%` }} />
                      </div>
                    </div>
                    <div>
                      <div className="flex justify-between items-center text-xs mb-1.5">
                        <span className="font-medium text-slate-700 dark:text-slate-300">Tự luận &amp; Vấn đáp kỹ thuật ({reportData.subjectiveTotal} câu)</span>
                        <span className="font-semibold text-slate-900 dark:text-white">{reportData.subjectiveScorePercent}%</span>
                      </div>
                      <div className="w-full bg-slate-100 dark:bg-slate-800 rounded-full h-1.5 overflow-hidden">
                        <div className="bg-slate-900 dark:bg-slate-100 h-1.5 rounded-full" style={{ width: `${reportData.subjectiveScorePercent}%` }} />
                      </div>
                    </div>
                  </div>
                </article>
              </section>
              {/* END: Detailed Breakdown Grid */}

              {/* BEGIN: Overall Assessment Comments (STITCH DESIGN) */}
              {(activeSession.teacherFeedback || activeSession.overallSummary) && (
                <section className="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200/80 dark:border-slate-800 p-6 sm:p-8 shadow-xs space-y-6">
                  {activeSession.teacherFeedback && (
                    <div className="space-y-2.5">
                      <div className="flex items-center gap-2 text-slate-800 dark:text-slate-200">
                        <UserCheck className="w-4 h-4 text-slate-700 dark:text-slate-300 shrink-0" />
                        <h3 className="text-sm font-semibold text-slate-900 dark:text-white">
                          Nhận xét của giảng viên phụ trách:
                        </h3>
                      </div>
                      <div className="bg-slate-50 dark:bg-slate-950 border border-slate-200/70 dark:border-slate-800 rounded-xl p-4 text-sm text-slate-700 dark:text-slate-300 leading-relaxed font-normal">
                        {activeSession.teacherFeedback}
                      </div>
                    </div>
                  )}

                  {activeSession.overallSummary && (
                    <div className="space-y-2.5">
                      <div className="flex items-center gap-2 text-slate-800 dark:text-slate-200">
                        <FileText className="w-4 h-4 text-slate-700 dark:text-slate-300 shrink-0" />
                        <h3 className="text-sm font-semibold text-slate-900 dark:text-white">
                          Đánh giá tổng hợp hệ thống:
                        </h3>
                      </div>
                      <div className="bg-slate-50 dark:bg-slate-950 border border-slate-200/70 dark:border-slate-800 rounded-xl p-4 text-sm text-slate-700 dark:text-slate-300 leading-relaxed font-normal">
                        {activeSession.overallSummary}
                      </div>
                    </div>
                  )}
                </section>
              )}
              {/* END: Overall Assessment Comments */}

              {/* BEGIN: Question Detail Section (STITCH DESIGN) */}
              <section className="space-y-4">
                <h2 className="text-lg font-bold text-slate-900 dark:text-white tracking-tight">
                  Chi tiết đánh giá từng câu hỏi ({activeSession.questions.length} câu)
                </h2>

                <div className="space-y-4">
                  {activeSession.questions.map((q, idx) => {
                    const scoreVal = q.score !== null && q.score !== undefined ? q.score : 0;
                    return (
                      <article
                        key={q.questionId}
                        className="bg-white dark:bg-slate-900 rounded-2xl border border-slate-200/80 dark:border-slate-800 p-6 sm:p-7 shadow-xs space-y-4"
                      >
                        {/* Header Info */}
                        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 pb-3 border-b border-slate-100 dark:border-slate-800">
                          <div className="flex items-center gap-2.5 flex-wrap">
                            <span className="text-base font-bold text-slate-900 dark:text-white">Câu {idx + 1}.</span>
                            <span className="text-xs font-medium px-2.5 py-0.5 rounded-md border border-slate-300 dark:border-slate-700 text-slate-700 dark:text-slate-300 bg-white dark:bg-slate-900">
                              {q.questionType === "MCQ" ? "Trắc nghiệm" : "Tự luận"}
                            </span>
                            <span className="text-xs font-medium px-2 py-0.5 rounded-md border border-slate-200 dark:border-slate-800 text-slate-600 dark:text-slate-400 bg-slate-50 dark:bg-slate-950">
                              {q.difficulty}
                            </span>
                          </div>
                          <div className="flex items-center gap-2.5 self-start sm:self-auto">
                            <span className="text-sm font-semibold text-slate-900 dark:text-white">
                              Điểm số: {scoreVal.toFixed(1)}/10
                            </span>
                            {scoreVal >= 8 ? (
                              <span className="text-xs font-medium px-2.5 py-0.5 rounded-full bg-slate-900 text-white dark:bg-slate-100 dark:text-slate-900">
                                Xuất sắc
                              </span>
                            ) : scoreVal >= (activeSession.passingScore || 7.0) ? (
                              <span className="text-xs font-medium px-2.5 py-0.5 rounded-full bg-slate-900 text-white dark:bg-slate-100 dark:text-slate-900">
                                Đạt
                              </span>
                            ) : scoreVal > 0 ? (
                              <span className="text-xs font-medium px-2.5 py-0.5 rounded-full bg-slate-200 text-slate-800 border border-slate-300 dark:bg-slate-800 dark:text-slate-200 dark:border-slate-700">
                                Cần cải thiện
                              </span>
                            ) : (
                              <span className="text-xs font-medium px-2.5 py-0.5 rounded-full bg-slate-100 text-slate-700 border border-slate-300 dark:bg-slate-800 dark:text-slate-300 dark:border-slate-700">
                                Chưa đạt
                              </span>
                            )}
                          </div>
                        </div>

                        {/* Question Prompt */}
                        <div>
                          <h3 className="text-sm sm:text-base font-semibold text-slate-900 dark:text-white leading-relaxed">
                            {q.questionText}
                          </h3>
                        </div>

                        {/* Answer Box */}
                        <div className="space-y-1.5">
                          <span className="text-xs font-semibold text-slate-600 dark:text-slate-400 block mb-1">
                            Câu trả lời của bạn:
                          </span>
                          <div className="bg-slate-50 dark:bg-slate-950 border border-slate-200/70 dark:border-slate-800 rounded-xl p-4 text-sm text-slate-800 dark:text-slate-200 leading-relaxed">
                            {q.questionType === "MCQ" ? (
                              <span className="font-medium text-slate-900 dark:text-white">
                                Đáp án đã chọn: {q.selectedOptionId || "(Không chọn)"}
                              </span>
                            ) : (
                              <span className="whitespace-pre-wrap">
                                {q.answerText || "(Bỏ trống câu này)"}
                              </span>
                            )}
                          </div>
                        </div>

                        {/* Model Answer Box */}
                        {(q.sampleAnswer || q.correctOptionId) && (
                          <div className="space-y-1.5">
                            <span className="text-xs font-semibold text-slate-600 dark:text-slate-400 block mb-1">
                              Gợi ý đáp án chuẩn:
                            </span>
                            <div className="border border-slate-200/80 dark:border-slate-800 rounded-xl p-4 text-sm text-slate-700 dark:text-slate-300 leading-relaxed bg-white dark:bg-slate-900">
                              {q.questionType === "MCQ" ? (
                                <span className="font-semibold text-slate-900 dark:text-white">Đáp án chuẩn: {q.correctOptionId}</span>
                              ) : (
                                q.sampleAnswer
                              )}
                            </div>
                          </div>
                        )}

                        {/* AI Review Box */}
                        {q.aiFeedback && (
                          <div className="space-y-1.5">
                            <span className="text-xs font-semibold text-slate-600 dark:text-slate-400 block mb-1">
                              Nhận xét:
                            </span>
                            <div className="border border-slate-200/80 dark:border-slate-800 rounded-xl p-4 text-sm text-slate-700 dark:text-slate-300 leading-relaxed bg-white dark:bg-slate-900 space-y-1">
                              {q.aiFeedback}
                            </div>
                          </div>
                        )}

                        {/* Teacher Note */}
                        {q.teacherNote && (
                          <div className="pt-1 text-xs text-slate-600 dark:text-slate-400 italic">
                            Giảng viên ghi chú: {q.teacherNote}
                          </div>
                        )}
                      </article>
                    );
                  })}
                </div>
              </section>
              {/* END: Question Detail Section */}
            </>
          )}
        </div>
      </MainLayout>
    );
  }

  return null;
}

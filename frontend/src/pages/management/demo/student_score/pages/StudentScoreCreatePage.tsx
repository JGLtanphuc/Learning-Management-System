import { classApi, studentScoreApi } from "@/api";
import { MainLayout } from "@/components/layout/MainLayout";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Switch } from "@/components/ui/switch";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import type {
  ClassDTO,
  ClassScoreItemDTO,
  SaveClassScoreBatchRequest,
  StudentScoreDTO,
} from "@/types";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  ArrowLeft,
  GraduationCap,
  Loader2,
  RefreshCw,
  Save,
  User,
  Users,
} from "lucide-react";
import { useEffect, useMemo, useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import { toast } from "sonner";

interface SingleScoreFormState {
  studentCode: string;
  studentName: string;
  className: string;
  attendance: string;
  quiz1: string;
  quiz2: string;
  assignment: string;
  practice: string;
  midterm: string;
  final: string;
  project: string;
  bonus: string;
  overallScore: string;
  isActive: boolean;
}

const initialSingleFormState: SingleScoreFormState = {
  studentCode: "",
  studentName: "",
  className: "",
  attendance: "",
  quiz1: "",
  quiz2: "",
  assignment: "",
  practice: "",
  midterm: "",
  final: "",
  project: "",
  bonus: "",
  overallScore: "",
  isActive: true,
};

function calculateRowAverage(scores: Record<string, number | null>): number | null {
  const coreKeys = ["attendance", "quiz1", "quiz2", "assignment", "practice", "midterm", "final", "project"];
  const validScores: number[] = [];
  coreKeys.forEach((k) => {
    const val = scores[k];
    if (val !== undefined && val !== null && !isNaN(val)) {
      validScores.push(val);
    }
  });

  if (validScores.length === 0) return null;
  const avg = validScores.reduce((a, b) => a + b, 0) / validScores.length;
  const bonus = scores.bonus !== undefined && scores.bonus !== null && !isNaN(scores.bonus) ? scores.bonus : 0;
  return Math.min(10, Math.round((avg + bonus) * 100) / 100);
}

export function StudentScoreCreatePage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const [activeTab, setActiveTab] = useState<string>("class-batch");
  const [selectedClassId, setSelectedClassId] = useState<string>("");
  const [studentRows, setStudentRows] = useState<ClassScoreItemDTO[]>([]);

  // State cho chế độ nhập đơn lẻ
  const [singleForm, setSingleForm] = useState<SingleScoreFormState>(initialSingleFormState);

  // Load danh sách lớp
  const classesQuery = useQuery({
    queryKey: ["classes", "dropdown"],
    queryFn: () => classApi.getPage({ page: 0, size: 100 } as any),
  });

  const classList: ClassDTO[] = useMemo(() => {
    return classesQuery.data?.content || classesQuery.data?.items || [];
  }, [classesQuery.data]);

  // Tự động chọn lớp đầu tiên nếu chưa chọn
  useEffect(() => {
    if (!selectedClassId && classList.length > 0) {
      setSelectedClassId(String(classList[0].id));
    }
  }, [classList, selectedClassId]);

  // Query sheet điểm của lớp
  const classSheetQuery = useQuery({
    queryKey: ["student-score", "class-sheet", selectedClassId],
    queryFn: () => studentScoreApi.getClassSheet(Number(selectedClassId)),
    enabled: !!selectedClassId,
  });

  // Đồng bộ hóa danh sách học viên khi API trả về
  useEffect(() => {
    if (classSheetQuery.data?.students) {
      const items = classSheetQuery.data.students.map((st) => {
        const scores = st.scores || {};
        const computedOverall = st.overallScore ?? calculateRowAverage(scores);
        return {
          ...st,
          scores,
          overallScore: computedOverall,
        };
      });
      setStudentRows(items);
    }
  }, [classSheetQuery.data]);

  // Handler cập nhật điểm 1 ô cho 1 học viên
  const handleScoreChange = (
    studentCode: string,
    key: string,
    rawVal: string
  ) => {
    setStudentRows((prev) =>
      prev.map((row) => {
        if (row.studentCode !== studentCode) return row;

        const val = rawVal.trim() === "" ? null : parseFloat(rawVal);
        const newScores = { ...row.scores, [key]: val };
        const newOverall = calculateRowAverage(newScores);

        return {
          ...row,
          scores: newScores,
          overallScore: newOverall,
        };
      })
    );
  };

  // Thống kê nhanh của lớp
  const stats = useMemo(() => {
    const total = studentRows.length;
    let gradedCount = 0;
    let passCount = 0;
    let failCount = 0;
    const overallList: number[] = [];

    studentRows.forEach((r) => {
      if (r.overallScore !== null && r.overallScore !== undefined) {
        gradedCount++;
        overallList.push(r.overallScore);
        if (r.overallScore >= 5.0) {
          passCount++;
        } else {
          failCount++;
        }
      }
    });

    const avgClass =
      overallList.length > 0
        ? Math.round((overallList.reduce((a, b) => a + b, 0) / overallList.length) * 100) / 100
        : null;

    return { total, gradedCount, passCount, failCount, avgClass };
  }, [studentRows]);

  // Mutation lưu điểm theo lớp
  const saveBatchMutation = useMutation({
    mutationFn: (payload: SaveClassScoreBatchRequest) => studentScoreApi.saveBatch(payload),
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ["student-score"] }),
        queryClient.invalidateQueries({ queryKey: ["student-score", "class-sheet", selectedClassId] }),
      ]);
      toast.success("Lưu bảng điểm lớp học thành công!");
      navigate("/student-scores");
    },
    onError: (error: any) => {
      toast.error(error?.response?.data?.message || "Lưu bảng điểm thất bại.");
    },
  });

  const handleSaveBatch = () => {
    if (!selectedClassId) {
      toast.error("Vui lòng chọn lớp học.");
      return;
    }

    if (studentRows.length === 0) {
      toast.error("Lớp này chưa có học viên nào để nhập điểm.");
      return;
    }

    const payload: SaveClassScoreBatchRequest = {
      classId: Number(selectedClassId),
      className: classSheetQuery.data?.className || classSheetQuery.data?.classCode,
      items: studentRows.map((r) => ({
        studentCode: r.studentCode,
        studentName: r.studentName,
        scores: r.scores,
        overallScore: r.overallScore,
      })),
    };

    saveBatchMutation.mutate(payload);
  };

  // Mutation lưu điểm đơn lẻ
  const createSingleMutation = useMutation({
    mutationFn: (payload: StudentScoreDTO) => studentScoreApi.create(payload),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["student-score"] });
      toast.success("Đã tạo bản ghi điểm học viên thành công.");
      navigate("/student-scores");
    },
    onError: (error: any) => {
      toast.error(error?.response?.data?.message || "Tạo bản ghi điểm thất bại.");
    },
  });

  const handleSingleSubmit = (e: FormEvent) => {
    e.preventDefault();
    if (!singleForm.studentCode.trim() || !singleForm.studentName.trim() || !singleForm.className.trim()) {
      toast.error("Vui lòng điền đủ Mã học viên, Họ tên và Lớp học.");
      return;
    }

    const scoresObj: Record<string, number | null> = {};
    const parseScore = (val: string) => (val.trim() === "" ? null : parseFloat(val));

    scoresObj.attendance = parseScore(singleForm.attendance);
    scoresObj.quiz1 = parseScore(singleForm.quiz1);
    scoresObj.quiz2 = parseScore(singleForm.quiz2);
    scoresObj.assignment = parseScore(singleForm.assignment);
    scoresObj.practice = parseScore(singleForm.practice);
    scoresObj.midterm = parseScore(singleForm.midterm);
    scoresObj.final = parseScore(singleForm.final);
    scoresObj.project = parseScore(singleForm.project);
    scoresObj.bonus = parseScore(singleForm.bonus);

    const overall = singleForm.overallScore.trim()
      ? parseFloat(singleForm.overallScore)
      : calculateRowAverage(scoresObj);

    createSingleMutation.mutate({
      studentCode: singleForm.studentCode.trim(),
      studentName: singleForm.studentName.trim(),
      className: singleForm.className.trim(),
      scores: scoresObj,
      overallScore: overall ?? undefined,
      isActive: singleForm.isActive,
    });
  };

  return (
    <MainLayout pathName={{ "student-scores": "Bảng điểm", create: "Nhập điểm" }}>
      <div className="space-y-6 max-w-7xl mx-auto pb-12">
        {/* Header Toolbar */}
        <div className="flex flex-wrap items-center justify-between gap-4 border-b pb-4">
          <div className="space-y-1">
            <h1 className="text-2xl font-bold tracking-tight">Nhập & Cập Nhật Điểm Học Viên</h1>
            <p className="text-sm text-muted-foreground">
              Giảng viên chọn lớp học để nạp danh sách học viên và nhập điểm trực tiếp theo từng cột đánh giá.
            </p>
          </div>
          <div className="flex items-center gap-2">
            <Button variant="outline" onClick={() => navigate("/student-scores")}>
              <ArrowLeft className="mr-2 h-4 w-4" /> Quay lại danh sách
            </Button>
            {activeTab === "class-batch" && (
              <Button
                onClick={handleSaveBatch}
                disabled={saveBatchMutation.isPending || studentRows.length === 0}
              >
                {saveBatchMutation.isPending ? (
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                ) : (
                  <Save className="mr-2 h-4 w-4" />
                )}
                Lưu bảng điểm ({studentRows.length} học viên)
              </Button>
            )}
          </div>
        </div>

        {/* Tabs chọn chế độ */}
        <Tabs value={activeTab} onValueChange={setActiveTab} className="space-y-6">
          <TabsList className="grid w-full max-w-md grid-cols-2">
            <TabsTrigger value="class-batch" className="flex items-center gap-2">
              <Users className="h-4 w-4" /> Nhập điểm theo lớp
            </TabsTrigger>
            <TabsTrigger value="single" className="flex items-center gap-2">
              <User className="h-4 w-4" /> Nhập học viên đơn lẻ
            </TabsTrigger>
          </TabsList>

          {/* TAB 1: NHẬP ĐIỂM THEO LỚP */}
          <TabsContent value="class-batch" className="space-y-6">
            {/* Bộ chọn lớp */}
            <Card className="border shadow-sm bg-card/60 backdrop-blur-sm">
              <CardHeader className="pb-3">
                <CardTitle className="text-base font-semibold flex items-center gap-2">
                  <GraduationCap className="h-5 w-5 text-primary" /> Chọn lớp học cần nhập điểm
                </CardTitle>
                <CardDescription>
                  Hệ thống sẽ nạp danh sách toàn bộ học viên chính thức của lớp để giảng viên chấm điểm.
                </CardDescription>
              </CardHeader>
              <CardContent>
                <div className="grid grid-cols-1 md:grid-cols-3 gap-6 items-end">
                  <div className="space-y-2 md:col-span-2">
                    <Label htmlFor="class-selector" className="text-sm font-medium">
                      Lớp học <span className="text-destructive">*</span>
                    </Label>
                    <Select
                      value={selectedClassId}
                      onValueChange={(val) => setSelectedClassId(val)}
                      disabled={classesQuery.isLoading}
                    >
                      <SelectTrigger id="class-selector" className="w-full">
                        <SelectValue
                          placeholder={
                            classesQuery.isLoading ? "Đang tải danh sách lớp..." : "Chọn lớp học"
                          }
                        />
                      </SelectTrigger>
                      <SelectContent>
                        {classList.map((cls) => (
                          <SelectItem key={cls.id} value={String(cls.id)}>
                            <span className="font-semibold">{cls.classCode}</span> — {cls.className}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>

                  <div>
                    <Button
                      variant="outline"
                      className="w-full"
                      onClick={() => classSheetQuery.refetch()}
                      disabled={classSheetQuery.isFetching || !selectedClassId}
                    >
                      <RefreshCw
                        className={`mr-2 h-4 w-4 ${classSheetQuery.isFetching ? "animate-spin" : ""}`}
                      />
                      Tải lại danh sách
                    </Button>
                  </div>
                </div>
              </CardContent>
            </Card>

            {/* Thống kê nhanh */}
            <div className="grid grid-cols-2 sm:grid-cols-5 gap-3">
              <Card className="p-3 bg-muted/40 border">
                <div className="text-xs font-medium text-muted-foreground">Sĩ số lớp</div>
                <div className="text-2xl font-bold mt-1">{stats.total}</div>
              </Card>
              <Card className="p-3 bg-blue-500/10 border-blue-500/20">
                <div className="text-xs font-medium text-blue-600 dark:text-blue-400">Đã chấm điểm</div>
                <div className="text-2xl font-bold mt-1 text-blue-700 dark:text-blue-300">
                  {stats.gradedCount} / {stats.total}
                </div>
              </Card>
              <Card className="p-3 bg-emerald-500/10 border-emerald-500/20">
                <div className="text-xs font-medium text-emerald-600 dark:text-emerald-400">Đạt (≥ 5.0)</div>
                <div className="text-2xl font-bold mt-1 text-emerald-700 dark:text-emerald-300">
                  {stats.passCount}
                </div>
              </Card>
              <Card className="p-3 bg-rose-500/10 border-rose-500/20">
                <div className="text-xs font-medium text-rose-600 dark:text-rose-400">Cần cố gắng (&lt; 5.0)</div>
                <div className="text-2xl font-bold mt-1 text-rose-700 dark:text-rose-300">
                  {stats.failCount}
                </div>
              </Card>
              <Card className="p-3 bg-purple-500/10 border-purple-500/20">
                <div className="text-xs font-medium text-purple-600 dark:text-purple-400">ĐTB cả lớp</div>
                <div className="text-2xl font-bold mt-1 text-purple-700 dark:text-purple-300">
                  {stats.avgClass !== null ? stats.avgClass : "-"}
                </div>
              </Card>
            </div>

            {/* Bảng nhập điểm chi tiết */}
            <Card>
              <CardHeader className="flex flex-row items-center justify-between pb-4">
                <div>
                  <CardTitle className="text-lg">
                    Bảng điểm chi tiết{" "}
                    {classSheetQuery.data
                      ? `(${classSheetQuery.data.classCode} - ${classSheetQuery.data.className})`
                      : ""}
                  </CardTitle>
                  <CardDescription>
                    Nhập điểm từ thang 0.0 - 10.0 cho từng cột. Điểm tổng kết (Overall) sẽ tự động tính ngay lập tức.
                  </CardDescription>
                </div>
              </CardHeader>

              <CardContent className="p-0">
                {classSheetQuery.isLoading ? (
                  <div className="flex flex-col items-center justify-center p-12 text-muted-foreground gap-3">
                    <Loader2 className="h-8 w-8 animate-spin text-primary" />
                    <p>Đang tải danh sách học viên...</p>
                  </div>
                ) : studentRows.length === 0 ? (
                  <div className="flex flex-col items-center justify-center p-12 text-center text-muted-foreground">
                    <Users className="h-10 w-10 text-muted-foreground/50 mb-2" />
                    <p className="font-semibold">Lớp học này chưa có học viên</p>
                    <p className="text-xs text-muted-foreground mt-1">
                      Vui lòng phân bổ học viên vào lớp hoặc chọn lớp học khác.
                    </p>
                  </div>
                ) : (
                  <div className="overflow-x-auto">
                    <table className="w-full text-sm text-left">
                      <thead className="text-xs uppercase bg-muted/60 border-y text-muted-foreground">
                        <tr>
                          <th className="px-2 py-3 w-10 text-center">STT</th>
                          <th className="px-2 py-3 w-24">Mã HV</th>
                          <th className="px-2 py-3 min-w-[130px]">Họ và tên</th>
                          <th className="px-1 py-3 w-20 text-center text-blue-600 dark:text-blue-400">
                            Chuyên cần
                          </th>
                          <th className="px-1 py-3 w-16 text-center">Quiz 1</th>
                          <th className="px-1 py-3 w-16 text-center">Quiz 2</th>
                          <th className="px-1 py-3 w-16 text-center">Assignment</th>
                          <th className="px-1 py-3 w-16 text-center text-indigo-600 dark:text-indigo-400 font-bold">
                            Practice
                          </th>
                          <th className="px-1 py-3 w-16 text-center">Midterm</th>
                          <th className="px-1 py-3 w-16 text-center">Final</th>
                          <th className="px-1 py-3 w-20 text-center text-teal-600 dark:text-teal-400">
                            Mock Project
                          </th>
                          <th className="px-1 py-3 w-16 text-center text-amber-600 dark:text-amber-400 font-bold">
                            Điểm cộng
                          </th>
                          <th className="px-2 py-3 w-20 text-center bg-primary/5 font-bold text-primary">
                            Overall
                          </th>
                          <th className="px-2 py-3 w-20 text-center">Kết quả</th>
                        </tr>
                      </thead>
                      <tbody className="divide-y">
                        {studentRows.map((row, idx) => {
                          const isPassed =
                            row.overallScore !== null &&
                            row.overallScore !== undefined &&
                            row.overallScore >= 5.0;
                          const hasScore =
                            row.overallScore !== null && row.overallScore !== undefined;

                          return (
                            <tr key={row.studentCode} className="hover:bg-muted/30 transition-colors">
                              <td className="px-2 py-3 text-center text-muted-foreground font-mono text-xs">
                                {idx + 1}
                              </td>
                              <td className="px-2 py-3 font-semibold text-primary font-mono text-xs">
                                {row.studentCode}
                              </td>
                              <td className="px-2 py-3">
                                <div className="font-medium text-foreground text-xs">{row.studentName}</div>
                                {row.email && (
                                  <div className="text-[10px] text-muted-foreground">{row.email}</div>
                                )}
                              </td>

                              {/* Chuyên cần (Attendance - Tự động từ bảng Điểm danh) */}
                              <td className="px-1 py-3 text-center">
                                <Input
                                  type="number"
                                  min="0"
                                  max="10"
                                  step="0.1"
                                  title="Tự động tính từ điểm danh (có thể điều chỉnh)"
                                  value={row.scores?.attendance !== null && row.scores?.attendance !== undefined ? row.scores.attendance : ""}
                                  onChange={(e) =>
                                    handleScoreChange(row.studentCode, "attendance", e.target.value)
                                  }
                                  className="h-8 text-center text-xs font-mono font-semibold bg-blue-500/5 border-blue-400/30"
                                />
                              </td>

                              {/* Quiz 1 */}
                              <td className="px-1 py-3 text-center">
                                <Input
                                  type="number"
                                  min="0"
                                  max="10"
                                  step="0.1"
                                  placeholder="-"
                                  value={row.scores?.quiz1 !== null && row.scores?.quiz1 !== undefined ? row.scores.quiz1 : ""}
                                  onChange={(e) =>
                                    handleScoreChange(row.studentCode, "quiz1", e.target.value)
                                  }
                                  className="h-8 text-center text-xs font-mono font-semibold"
                                />
                              </td>

                              {/* Quiz 2 */}
                              <td className="px-1 py-3 text-center">
                                <Input
                                  type="number"
                                  min="0"
                                  max="10"
                                  step="0.1"
                                  placeholder="-"
                                  value={row.scores?.quiz2 !== null && row.scores?.quiz2 !== undefined ? row.scores.quiz2 : ""}
                                  onChange={(e) =>
                                    handleScoreChange(row.studentCode, "quiz2", e.target.value)
                                  }
                                  className="h-8 text-center text-xs font-mono font-semibold"
                                />
                              </td>

                              {/* Assignment */}
                              <td className="px-1 py-3 text-center">
                                <Input
                                  type="number"
                                  min="0"
                                  max="10"
                                  step="0.1"
                                  placeholder="-"
                                  value={row.scores?.assignment !== null && row.scores?.assignment !== undefined ? row.scores.assignment : ""}
                                  onChange={(e) =>
                                    handleScoreChange(row.studentCode, "assignment", e.target.value)
                                  }
                                  className="h-8 text-center text-xs font-mono font-semibold"
                                />
                              </td>

                              {/* Practice (Thực hành) */}
                              <td className="px-1 py-3 text-center">
                                <Input
                                  type="number"
                                  min="0"
                                  max="10"
                                  step="0.1"
                                  placeholder="-"
                                  title="Điểm bài tập thực hành (Practice)"
                                  value={row.scores?.practice !== null && row.scores?.practice !== undefined ? row.scores.practice : ""}
                                  onChange={(e) =>
                                    handleScoreChange(row.studentCode, "practice", e.target.value)
                                  }
                                  className="h-8 text-center text-xs font-mono font-semibold bg-indigo-500/5 border-indigo-400/30"
                                />
                              </td>

                              {/* Midterm */}
                              <td className="px-1 py-3 text-center">
                                <Input
                                  type="number"
                                  min="0"
                                  max="10"
                                  step="0.1"
                                  placeholder="-"
                                  value={row.scores?.midterm !== null && row.scores?.midterm !== undefined ? row.scores.midterm : ""}
                                  onChange={(e) =>
                                    handleScoreChange(row.studentCode, "midterm", e.target.value)
                                  }
                                  className="h-8 text-center text-xs font-mono font-semibold"
                                />
                              </td>

                              {/* Final */}
                              <td className="px-1 py-3 text-center">
                                <Input
                                  type="number"
                                  min="0"
                                  max="10"
                                  step="0.1"
                                  placeholder="-"
                                  value={row.scores?.final !== null && row.scores?.final !== undefined ? row.scores.final : ""}
                                  onChange={(e) =>
                                    handleScoreChange(row.studentCode, "final", e.target.value)
                                  }
                                  className="h-8 text-center text-xs font-mono font-semibold"
                                />
                              </td>

                              {/* Mock Project (Tự động từ bảng Mock Project) */}
                              <td className="px-1 py-3 text-center">
                                <Input
                                  type="number"
                                  min="0"
                                  max="10"
                                  step="0.1"
                                  title="Tự động đồng bộ từ điểm đồ án Mock Project của lớp"
                                  value={row.scores?.project !== null && row.scores?.project !== undefined ? row.scores.project : ""}
                                  onChange={(e) =>
                                    handleScoreChange(row.studentCode, "project", e.target.value)
                                  }
                                  className="h-8 text-center text-xs font-mono font-semibold bg-teal-500/5 border-teal-400/30"
                                />
                              </td>

                              {/* Điểm cộng (Bonus) */}
                              <td className="px-1 py-3 text-center">
                                <Input
                                  type="number"
                                  min="0"
                                  max="10"
                                  step="0.1"
                                  placeholder="-"
                                  title="Điểm cộng khuyến khích (cộng trực tiếp vào Overall)"
                                  value={row.scores?.bonus !== null && row.scores?.bonus !== undefined ? row.scores.bonus : ""}
                                  onChange={(e) =>
                                    handleScoreChange(row.studentCode, "bonus", e.target.value)
                                  }
                                  className="h-8 text-center text-xs font-mono font-semibold bg-amber-500/5 border-amber-400/30"
                                />
                              </td>

                              {/* Overall Score */}
                              <td className="px-2 py-3 text-center bg-primary/5 font-mono font-bold text-xs">
                                {hasScore ? (
                                  <span
                                    className={
                                      isPassed
                                        ? "text-emerald-600 dark:text-emerald-400"
                                        : "text-rose-600 dark:text-rose-400"
                                    }
                                  >
                                    {row.overallScore}
                                  </span>
                                ) : (
                                  <span className="text-muted-foreground">-</span>
                                )}
                              </td>

                              {/* Kết quả badge */}
                              <td className="px-2 py-3 text-center">
                                {hasScore ? (
                                  isPassed ? (
                                    <Badge className="bg-emerald-500/20 text-emerald-700 dark:text-emerald-300 border-emerald-500/30 text-[10px] px-1.5 py-0.5">
                                      Đạt
                                    </Badge>
                                  ) : (
                                    <Badge className="bg-rose-500/20 text-rose-700 dark:text-rose-300 border-rose-500/30 text-[10px] px-1.5 py-0.5">
                                      Trượt
                                    </Badge>
                                  )
                                ) : (
                                  <Badge variant="outline" className="text-[10px] text-muted-foreground px-1 py-0.5">
                                    Chưa chấm
                                  </Badge>
                                )}
                              </td>
                            </tr>
                          );
                        })}
                      </tbody>
                    </table>
                  </div>
                )}
              </CardContent>

              {studentRows.length > 0 && (
                <CardFooter className="flex items-center justify-between border-t py-4">
                  <div className="text-sm text-muted-foreground">
                    Đã hiển thị <span className="font-semibold text-foreground">{studentRows.length}</span> học viên
                  </div>
                  <div className="flex items-center gap-2">
                    <Button variant="outline" onClick={() => navigate("/student-scores")}>
                      Hủy bỏ
                    </Button>
                    <Button onClick={handleSaveBatch} disabled={saveBatchMutation.isPending}>
                      {saveBatchMutation.isPending ? (
                        <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                      ) : (
                        <Save className="mr-2 h-4 w-4" />
                      )}
                      Lưu bảng điểm
                    </Button>
                  </div>
                </CardFooter>
              )}
            </Card>
          </TabsContent>

          {/* TAB 2: NHẬP ĐIỂM ĐƠN LẺ */}
          <TabsContent value="single">
            <form onSubmit={handleSingleSubmit} className="max-w-2xl mx-auto">
              <Card>
                <CardHeader>
                  <CardTitle className="text-base flex items-center gap-2">
                    <User className="h-5 w-5 text-primary" /> Nhập điểm cho một học viên cụ thể
                  </CardTitle>
                  <CardDescription>
                    Sử dụng biểu mẫu này khi bạn chỉ muốn bổ sung hoặc cập nhật điểm cho một học viên riêng lẻ.
                  </CardDescription>
                </CardHeader>
                <CardContent className="space-y-4">
                  <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                    <div className="space-y-2">
                      <Label htmlFor="single-student-code">
                        Mã học viên <span className="text-destructive">*</span>
                      </Label>
                      <Input
                        id="single-student-code"
                        placeholder="VD: SE160101"
                        value={singleForm.studentCode}
                        onChange={(e) =>
                          setSingleForm((prev) => ({ ...prev, studentCode: e.target.value }))
                        }
                        required
                      />
                    </div>
                    <div className="space-y-2">
                      <Label htmlFor="single-student-name">
                        Họ và tên <span className="text-destructive">*</span>
                      </Label>
                      <Input
                        id="single-student-name"
                        placeholder="VD: Nguyen Van An"
                        value={singleForm.studentName}
                        onChange={(e) =>
                          setSingleForm((prev) => ({ ...prev, studentName: e.target.value }))
                        }
                        required
                      />
                    </div>
                  </div>

                  <div className="space-y-2">
                    <Label htmlFor="single-class-name">
                      Lớp học <span className="text-destructive">*</span>
                    </Label>
                    <Input
                      id="single-class-name"
                      placeholder="VD: HN26_FR_JAVA_01"
                      value={singleForm.className}
                      onChange={(e) =>
                        setSingleForm((prev) => ({ ...prev, className: e.target.value }))
                      }
                      required
                    />
                  </div>

                  <div className="grid grid-cols-3 sm:grid-cols-4 gap-3 pt-2">
                    <div className="space-y-1">
                      <Label className="text-xs text-blue-600 dark:text-blue-400">Chuyên cần</Label>
                      <Input
                        type="number"
                        min="0"
                        max="10"
                        step="0.1"
                        placeholder="-"
                        value={singleForm.attendance}
                        onChange={(e) => setSingleForm((prev) => ({ ...prev, attendance: e.target.value }))}
                      />
                    </div>
                    <div className="space-y-1">
                      <Label className="text-xs">Quiz 1</Label>
                      <Input
                        type="number"
                        min="0"
                        max="10"
                        step="0.1"
                        placeholder="-"
                        value={singleForm.quiz1}
                        onChange={(e) => setSingleForm((prev) => ({ ...prev, quiz1: e.target.value }))}
                      />
                    </div>
                    <div className="space-y-1">
                      <Label className="text-xs">Quiz 2</Label>
                      <Input
                        type="number"
                        min="0"
                        max="10"
                        step="0.1"
                        placeholder="-"
                        value={singleForm.quiz2}
                        onChange={(e) => setSingleForm((prev) => ({ ...prev, quiz2: e.target.value }))}
                      />
                    </div>
                    <div className="space-y-1">
                      <Label className="text-xs">Assignment</Label>
                      <Input
                        type="number"
                        min="0"
                        max="10"
                        step="0.1"
                        placeholder="-"
                        value={singleForm.assignment}
                        onChange={(e) =>
                          setSingleForm((prev) => ({ ...prev, assignment: e.target.value }))
                        }
                      />
                    </div>
                    <div className="space-y-1">
                      <Label className="text-xs text-indigo-600 dark:text-indigo-400 font-semibold">Practice</Label>
                      <Input
                        type="number"
                        min="0"
                        max="10"
                        step="0.1"
                        placeholder="-"
                        value={singleForm.practice}
                        onChange={(e) => setSingleForm((prev) => ({ ...prev, practice: e.target.value }))}
                      />
                    </div>
                    <div className="space-y-1">
                      <Label className="text-xs">Midterm</Label>
                      <Input
                        type="number"
                        min="0"
                        max="10"
                        step="0.1"
                        placeholder="-"
                        value={singleForm.midterm}
                        onChange={(e) => setSingleForm((prev) => ({ ...prev, midterm: e.target.value }))}
                      />
                    </div>
                    <div className="space-y-1">
                      <Label className="text-xs">Final</Label>
                      <Input
                        type="number"
                        min="0"
                        max="10"
                        step="0.1"
                        placeholder="-"
                        value={singleForm.final}
                        onChange={(e) => setSingleForm((prev) => ({ ...prev, final: e.target.value }))}
                      />
                    </div>
                    <div className="space-y-1">
                      <Label className="text-xs text-teal-600 dark:text-teal-400">Mock Project</Label>
                      <Input
                        type="number"
                        min="0"
                        max="10"
                        step="0.1"
                        placeholder="-"
                        value={singleForm.project}
                        onChange={(e) => setSingleForm((prev) => ({ ...prev, project: e.target.value }))}
                      />
                    </div>
                    <div className="space-y-1">
                      <Label className="text-xs text-amber-600 dark:text-amber-400 font-semibold">Điểm cộng</Label>
                      <Input
                        type="number"
                        min="0"
                        max="10"
                        step="0.1"
                        placeholder="-"
                        value={singleForm.bonus}
                        onChange={(e) => setSingleForm((prev) => ({ ...prev, bonus: e.target.value }))}
                      />
                    </div>
                  </div>

                  <div className="space-y-2 pt-2">
                    <Label htmlFor="single-overall">
                      Điểm tổng kết (Overall) <span className="text-xs text-muted-foreground">(Để trống nếu muốn tự tính)</span>
                    </Label>
                    <Input
                      id="single-overall"
                      type="number"
                      min="0"
                      max="10"
                      step="0.1"
                      placeholder="Tự động tính theo trung bình"
                      value={singleForm.overallScore}
                      onChange={(e) =>
                        setSingleForm((prev) => ({ ...prev, overallScore: e.target.value }))
                      }
                    />
                  </div>

                  <div className="flex items-center justify-between p-3 border rounded-lg">
                    <Label htmlFor="single-active" className="text-sm font-medium">
                      Kích hoạt bản ghi (Is Active)
                    </Label>
                    <Switch
                      id="single-active"
                      checked={singleForm.isActive}
                      onCheckedChange={(checked) =>
                        setSingleForm((prev) => ({ ...prev, isActive: checked }))
                      }
                    />
                  </div>
                </CardContent>

                <CardFooter className="flex items-center justify-between border-t py-4">
                  <Button variant="outline" type="button" onClick={() => navigate("/student-scores")}>
                    Hủy bỏ
                  </Button>
                  <Button type="submit" disabled={createSingleMutation.isPending}>
                    {createSingleMutation.isPending ? (
                      <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                    ) : (
                      <Save className="mr-2 h-4 w-4" />
                    )}
                    Lưu học viên
                  </Button>
                </CardFooter>
              </Card>
            </form>
          </TabsContent>
        </Tabs>
      </div>
    </MainLayout>
  );
}

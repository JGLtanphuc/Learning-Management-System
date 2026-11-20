import { studentScoreApi } from "@/api";
import { MainLayout } from "@/components/layout/MainLayout";
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
import { Switch } from "@/components/ui/switch";
import type { StudentScoreDTO } from "@/types";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ArrowLeft, Calculator, Save } from "lucide-react";
import { type FormEvent, useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { toast } from "sonner";

interface ScoreFormState {
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

const initialFormState: ScoreFormState = {
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

function getErrorMessage(error: any, fallback: string): string {
    return error?.response?.data?.message || fallback;
}

export function StudentScoreEditPage() {
    const navigate = useNavigate();
    const queryClient = useQueryClient();
    const { id } = useParams<{ id: string }>();

    const [form, setForm] = useState<ScoreFormState>(initialFormState);

    const scoreQuery = useQuery({
        queryKey: ["student-score", "detail", id],
        queryFn: () => studentScoreApi.getById(id as string),
        enabled: !!id,
    });

    useEffect(() => {
        if (!scoreQuery.data) return;

        const data = scoreQuery.data as any;
        const scoresObj: Record<string, any> = {};

        if (Array.isArray(data.scoreColumns)) {
            data.scoreColumns.forEach((col: any) => {
                const key = col.scoreKey || col.key || col.name;
                if (key) scoresObj[key] = col.scoreValue ?? col.value;
            });
        } else if (data.scores && typeof data.scores === "object") {
            Object.assign(scoresObj, data.scores);
        }

        const toStr = (val: any) => (val !== undefined && val !== null ? String(val) : "");

        setForm({
            studentCode: data.studentCode || "",
            studentName: data.studentName || "",
            className: data.className || "",
            attendance: toStr(scoresObj.attendance),
            quiz1: toStr(scoresObj.quiz1),
            quiz2: toStr(scoresObj.quiz2),
            assignment: toStr(scoresObj.assignment),
            practice: toStr(scoresObj.practice),
            midterm: toStr(scoresObj.midterm),
            final: toStr(scoresObj.final),
            project: toStr(scoresObj.project),
            bonus: toStr(scoresObj.bonus),
            overallScore: toStr(data.overallScore),
            isActive: data.isActive ?? true,
        });
    }, [scoreQuery.data]);

    const calculatedAverage = useMemo(() => {
        const core = [
            form.attendance,
            form.quiz1,
            form.quiz2,
            form.assignment,
            form.practice,
            form.midterm,
            form.final,
            form.project,
        ]
            .map((val) => parseFloat(val))
            .filter((num) => !isNaN(num));

        if (core.length === 0) return null;
        const avg = core.reduce((a, b) => a + b, 0) / core.length;
        const bonusVal = parseFloat(form.bonus) || 0;
        return Math.min(10, Math.round((avg + bonusVal) * 100) / 100);
    }, [
        form.attendance,
        form.quiz1,
        form.quiz2,
        form.assignment,
        form.practice,
        form.midterm,
        form.final,
        form.project,
        form.bonus,
    ]);

    const updateMutation = useMutation({
        mutationFn: (payload: StudentScoreDTO) => studentScoreApi.update(id as string, payload),
        onSuccess: async () => {
            await queryClient.invalidateQueries({ queryKey: ["student-score"] });
            toast.success("Đã cập nhật điểm học viên thành công.");
            navigate("/student-scores");
        },
        onError: (error: any) => {
            toast.error(getErrorMessage(error, "Cập nhật điểm thất bại."));
        },
    });

    const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();

        const studentCode = form.studentCode?.trim();
        const studentName = form.studentName?.trim();
        const className = form.className?.trim();

        if (!studentCode) {
            toast.error("Vui lòng nhập mã học viên.");
            return;
        }
        if (!studentName) {
            toast.error("Vui lòng nhập họ tên học viên.");
            return;
        }
        if (!className) {
            toast.error("Vui lòng nhập tên lớp học.");
            return;
        }

        const scoresMap: Record<string, number | null> = {};
        if (form.attendance !== "") scoresMap.attendance = parseFloat(form.attendance);
        if (form.quiz1 !== "") scoresMap.quiz1 = parseFloat(form.quiz1);
        if (form.quiz2 !== "") scoresMap.quiz2 = parseFloat(form.quiz2);
        if (form.assignment !== "") scoresMap.assignment = parseFloat(form.assignment);
        if (form.practice !== "") scoresMap.practice = parseFloat(form.practice);
        if (form.midterm !== "") scoresMap.midterm = parseFloat(form.midterm);
        if (form.final !== "") scoresMap.final = parseFloat(form.final);
        if (form.project !== "") scoresMap.project = parseFloat(form.project);
        if (form.bonus !== "") scoresMap.bonus = parseFloat(form.bonus);

        const overall = form.overallScore !== ""
            ? parseFloat(form.overallScore)
            : (calculatedAverage ?? undefined);

        updateMutation.mutate({
            studentCode,
            studentName,
            className,
            scores: scoresMap,
            overallScore: overall,
            isActive: form.isActive,
        });
    };

    if (scoreQuery.isLoading) {
        return (
            <MainLayout pathName={{ "student-scores": "Student Scores", edit: "Chỉnh sửa điểm" }}>
                <div className="p-8 text-center text-muted-foreground">Đang tải thông tin điểm...</div>
            </MainLayout>
        );
    }

    return (
        <MainLayout pathName={{ "student-scores": "Student Scores", edit: "Chỉnh sửa điểm" }}>
            <Card className="max-w-4xl mx-auto">
                <CardHeader>
                    <CardTitle className="text-xl">Chỉnh sửa điểm học viên: {form.studentName || `#${id}`}</CardTitle>
                    <CardDescription>
                        Cập nhật lại điểm các bài đánh giá, phúc khảo hoặc điều chỉnh kết quả học tập.
                    </CardDescription>
                </CardHeader>

                <CardContent>
                    <form className="grid gap-6" onSubmit={handleSubmit}>
                        {/* Thông tin chung */}
                        <div className="rounded-lg border p-4 bg-slate-50/50 dark:bg-slate-900/50">
                            <h3 className="text-sm font-semibold mb-3 text-slate-700 dark:text-slate-200">
                                1. Thông tin học viên & Lớp đào tạo
                            </h3>
                            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                                <div className="grid gap-2">
                                    <Label htmlFor="student-code">Mã học viên <span className="text-red-500">*</span></Label>
                                    <Input
                                        id="student-code"
                                        value={form.studentCode}
                                        onChange={(e) => setForm((prev) => ({ ...prev, studentCode: e.target.value }))}
                                        placeholder="Ví dụ: SV001"
                                    />
                                </div>

                                <div className="grid gap-2">
                                    <Label htmlFor="student-name">Họ và tên <span className="text-red-500">*</span></Label>
                                    <Input
                                        id="student-name"
                                        value={form.studentName}
                                        onChange={(e) => setForm((prev) => ({ ...prev, studentName: e.target.value }))}
                                        placeholder="Ví dụ: Nguyễn Văn A"
                                    />
                                </div>

                                <div className="grid gap-2">
                                    <Label htmlFor="class-name">Lớp học <span className="text-red-500">*</span></Label>
                                    <Input
                                        id="class-name"
                                        value={form.className}
                                        onChange={(e) => setForm((prev) => ({ ...prev, className: e.target.value }))}
                                        placeholder="Ví dụ: HN26_FR_JAVA_01"
                                    />
                                </div>
                            </div>
                        </div>

                        {/* Điểm thành phần */}
                        <div className="rounded-lg border p-4 bg-slate-50/50 dark:bg-slate-900/50">
                            <div className="flex items-center justify-between mb-3">
                                <h3 className="text-sm font-semibold text-slate-700 dark:text-slate-200">
                                    2. Điểm số thành phần (Thang điểm 0 - 10)
                                </h3>
                                {calculatedAverage !== null && (
                                    <div className="flex items-center gap-1.5 text-xs text-blue-700 bg-blue-50 px-2.5 py-1 rounded-full border border-blue-200">
                                        <Calculator className="h-3.5 w-3.5" />
                                        Điểm TB tự tính: <span className="font-bold text-sm">{calculatedAverage}</span>
                                    </div>
                                )}
                            </div>
                            <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-5 gap-3">
                                <div className="grid gap-1.5">
                                    <Label htmlFor="attendance" className="text-xs text-blue-600 dark:text-blue-400 font-semibold">Chuyên cần</Label>
                                    <Input
                                        id="attendance"
                                        type="number"
                                        step="0.1"
                                        min="0"
                                        max="10"
                                        value={form.attendance}
                                        onChange={(e) => setForm((prev) => ({ ...prev, attendance: e.target.value }))}
                                        placeholder="0.0"
                                        className="bg-blue-50/30 dark:bg-blue-950/20"
                                    />
                                </div>

                                <div className="grid gap-1.5">
                                    <Label htmlFor="quiz1" className="text-xs">Quiz 1</Label>
                                    <Input
                                        id="quiz1"
                                        type="number"
                                        step="0.1"
                                        min="0"
                                        max="10"
                                        value={form.quiz1}
                                        onChange={(e) => setForm((prev) => ({ ...prev, quiz1: e.target.value }))}
                                        placeholder="0.0"
                                    />
                                </div>

                                <div className="grid gap-1.5">
                                    <Label htmlFor="quiz2" className="text-xs">Quiz 2</Label>
                                    <Input
                                        id="quiz2"
                                        type="number"
                                        step="0.1"
                                        min="0"
                                        max="10"
                                        value={form.quiz2}
                                        onChange={(e) => setForm((prev) => ({ ...prev, quiz2: e.target.value }))}
                                        placeholder="0.0"
                                    />
                                </div>

                                <div className="grid gap-1.5">
                                    <Label htmlFor="assignment" className="text-xs">Assignment</Label>
                                    <Input
                                        id="assignment"
                                        type="number"
                                        step="0.1"
                                        min="0"
                                        max="10"
                                        value={form.assignment}
                                        onChange={(e) => setForm((prev) => ({ ...prev, assignment: e.target.value }))}
                                        placeholder="0.0"
                                    />
                                </div>

                                <div className="grid gap-1.5">
                                    <Label htmlFor="practice" className="text-xs text-indigo-600 dark:text-indigo-400 font-semibold">Practice</Label>
                                    <Input
                                        id="practice"
                                        type="number"
                                        step="0.1"
                                        min="0"
                                        max="10"
                                        value={form.practice}
                                        onChange={(e) => setForm((prev) => ({ ...prev, practice: e.target.value }))}
                                        placeholder="0.0"
                                        className="bg-indigo-50/30 dark:bg-indigo-950/20"
                                    />
                                </div>

                                <div className="grid gap-1.5">
                                    <Label htmlFor="midterm" className="text-xs">Midterm</Label>
                                    <Input
                                        id="midterm"
                                        type="number"
                                        step="0.1"
                                        min="0"
                                        max="10"
                                        value={form.midterm}
                                        onChange={(e) => setForm((prev) => ({ ...prev, midterm: e.target.value }))}
                                        placeholder="0.0"
                                    />
                                </div>

                                <div className="grid gap-1.5">
                                    <Label htmlFor="final" className="text-xs">Final</Label>
                                    <Input
                                        id="final"
                                        type="number"
                                        step="0.1"
                                        min="0"
                                        max="10"
                                        value={form.final}
                                        onChange={(e) => setForm((prev) => ({ ...prev, final: e.target.value }))}
                                        placeholder="0.0"
                                    />
                                </div>

                                <div className="grid gap-1.5">
                                    <Label htmlFor="project" className="text-xs text-teal-600 dark:text-teal-400 font-semibold">Mock Project</Label>
                                    <Input
                                        id="project"
                                        type="number"
                                        step="0.1"
                                        min="0"
                                        max="10"
                                        value={form.project}
                                        onChange={(e) => setForm((prev) => ({ ...prev, project: e.target.value }))}
                                        placeholder="0.0"
                                        className="bg-teal-50/30 dark:bg-teal-950/20"
                                    />
                                </div>

                                <div className="grid gap-1.5">
                                    <Label htmlFor="bonus" className="text-xs text-amber-600 dark:text-amber-400 font-semibold">Điểm cộng</Label>
                                    <Input
                                        id="bonus"
                                        type="number"
                                        step="0.1"
                                        min="0"
                                        max="10"
                                        value={form.bonus}
                                        onChange={(e) => setForm((prev) => ({ ...prev, bonus: e.target.value }))}
                                        placeholder="0.0"
                                        className="bg-amber-50/30 dark:bg-amber-950/20"
                                    />
                                </div>
                            </div>

                            <div className="mt-4 pt-3 border-t grid grid-cols-1 md:grid-cols-2 gap-4 items-center">
                                <div className="grid gap-1.5">
                                    <Label htmlFor="overall" className="text-xs font-semibold">
                                        Điểm tổng kết (Overall Score)
                                    </Label>
                                    <Input
                                        id="overall"
                                        type="number"
                                        step="0.1"
                                        min="0"
                                        max="10"
                                        value={form.overallScore}
                                        onChange={(e) => setForm((prev) => ({ ...prev, overallScore: e.target.value }))}
                                        placeholder={calculatedAverage !== null ? `Mặc định tính: ${calculatedAverage}` : "Nhập hoặc để tự tính"}
                                    />
                                    <span className="text-xs text-muted-foreground">
                                        * Nếu để trống, hệ thống sẽ tự động tính từ trung bình các đầu điểm thành phần.
                                    </span>
                                </div>

                                <div className="flex items-center gap-3 pt-2 md:pt-0">
                                    <Switch
                                        id="is-active"
                                        checked={form.isActive}
                                        onCheckedChange={(checked) => setForm((prev) => ({ ...prev, isActive: checked }))}
                                    />
                                    <Label htmlFor="is-active" className="cursor-pointer text-sm">
                                        Kích hoạt trạng thái điểm (Active)
                                    </Label>
                                </div>
                            </div>
                        </div>

                        <CardFooter className="flex justify-between px-0 pt-2">
                            <Button type="button" variant="outline" onClick={() => navigate("/student-scores")}>
                                <ArrowLeft className="h-4 w-4 mr-2" />
                                Quay lại danh sách
                            </Button>

                            <Button type="submit" disabled={updateMutation.isPending}>
                                <Save className="h-4 w-4 mr-2" />
                                {updateMutation.isPending ? "Đang lưu..." : "Cập nhật điểm"}
                            </Button>
                        </CardFooter>
                    </form>
                </CardContent>
            </Card>
        </MainLayout>
    );
}


import { studentScoreApi } from "@/api";
import { MainLayout } from "@/components/layout/MainLayout";
import { PermissionGate } from "@/components/PermissionGate";
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
import { useQuery } from "@tanstack/react-query";
import { ArrowLeft, Award, CheckCircle, Edit, GraduationCap, XCircle } from "lucide-react";
import { Link, useNavigate, useParams } from "react-router-dom";

export function StudentScoreDetailPage() {
    const navigate = useNavigate();
    const { id } = useParams<{ id: string }>();

    const scoreQuery = useQuery({
        queryKey: ["student-score", "detail", id],
        queryFn: () => studentScoreApi.getById(id as string),
        enabled: !!id,
    });

    if (scoreQuery.isLoading) {
        return (
            <MainLayout pathName={{ "student-scores": "Student Scores", detail: "Chi tiết bảng điểm" }}>
                <div className="p-8 text-center text-muted-foreground">Đang tải thông tin điểm...</div>
            </MainLayout>
        );
    }

    const data = scoreQuery.data as any;
    if (!data) {
        return (
            <MainLayout pathName={{ "student-scores": "Student Scores", detail: "Chi tiết bảng điểm" }}>
                <Card className="max-w-2xl mx-auto">
                    <CardHeader>
                        <CardTitle>Không tìm thấy bản ghi</CardTitle>
                        <CardDescription>Bản ghi điểm với ID #{id} không tồn tại hoặc đã bị xóa.</CardDescription>
                    </CardHeader>
                    <CardFooter>
                        <Button variant="outline" onClick={() => navigate("/student-scores")}>
                            <ArrowLeft className="h-4 w-4 mr-2" /> Quay lại
                        </Button>
                    </CardFooter>
                </Card>
            </MainLayout>
        );
    }

    const scoresObj: Record<string, any> = {};
    if (Array.isArray(data.scoreColumns)) {
        data.scoreColumns.forEach((col: any) => {
            const key = col.scoreKey || col.key || col.name;
            if (key) scoresObj[key] = col.scoreValue ?? col.value;
        });
    } else if (data.scores && typeof data.scores === "object") {
        Object.assign(scoresObj, data.scores);
    }

    const isPassed = (data.overallScore ?? 0) >= 5.0;

    return (
        <MainLayout pathName={{ "student-scores": "Student Scores", detail: "Chi tiết bảng điểm" }}>
            <Card className="max-w-3xl mx-auto">
                <CardHeader>
                    <div className="flex items-center justify-between">
                        <div>
                            <CardTitle className="text-xl flex items-center gap-2">
                                <GraduationCap className="h-6 w-6 text-indigo-600" />
                                {data.studentName} ({data.studentCode})
                            </CardTitle>
                            <CardDescription className="mt-1">
                                Lớp: <span className="font-semibold text-slate-800 dark:text-slate-200">{data.className || "Chưa phân lớp"}</span>
                            </CardDescription>
                        </div>
                        <Badge variant={isPassed ? "default" : "destructive"} className="text-sm px-3 py-1 flex items-center gap-1">
                            {isPassed ? <CheckCircle className="h-4 w-4" /> : <XCircle className="h-4 w-4" />}
                            {isPassed ? "ĐẠT (Passed)" : "CHƯA ĐẠT (Failed)"}
                        </Badge>
                    </div>
                </CardHeader>

                <CardContent className="grid gap-6">
                    {/* Bảng điểm thành phần */}
                    <div className="rounded-lg border p-4">
                        <h4 className="text-sm font-semibold mb-3 flex items-center gap-2 text-slate-700 dark:text-slate-200">
                            <Award className="h-4 w-4 text-amber-500" />
                            Kết quả các bài kiểm tra & thi
                        </h4>
                        <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-5 gap-3 text-center">
                            <div className="p-3 bg-emerald-50/50 dark:bg-emerald-950/40 rounded-lg border border-emerald-200">
                                <div className="text-xs text-emerald-700 font-semibold">Chuyên cần</div>
                                <div className="text-lg font-bold text-emerald-700 mt-1">
                                    {scoresObj.attendance ?? "-"}
                                </div>
                            </div>
                            <div className="p-3 bg-slate-50 dark:bg-slate-900 rounded-lg border">
                                <div className="text-xs text-muted-foreground">Quiz 1</div>
                                <div className="text-lg font-bold text-slate-800 dark:text-slate-100 mt-1">
                                    {scoresObj.quiz1 ?? "-"}
                                </div>
                            </div>
                            <div className="p-3 bg-slate-50 dark:bg-slate-900 rounded-lg border">
                                <div className="text-xs text-muted-foreground">Quiz 2</div>
                                <div className="text-lg font-bold text-slate-800 dark:text-slate-100 mt-1">
                                    {scoresObj.quiz2 ?? "-"}
                                </div>
                            </div>
                            <div className="p-3 bg-slate-50 dark:bg-slate-900 rounded-lg border">
                                <div className="text-xs text-muted-foreground">Assignment</div>
                                <div className="text-lg font-bold text-slate-800 dark:text-slate-100 mt-1">
                                    {scoresObj.assignment ?? "-"}
                                </div>
                            </div>
                            <div className="p-3 bg-slate-50 dark:bg-slate-900 rounded-lg border">
                                <div className="text-xs text-muted-foreground">Practice</div>
                                <div className="text-lg font-bold text-slate-800 dark:text-slate-100 mt-1">
                                    {scoresObj.practice ?? "-"}
                                </div>
                            </div>
                            <div className="p-3 bg-slate-50 dark:bg-slate-900 rounded-lg border">
                                <div className="text-xs text-muted-foreground">Giữa kỳ</div>
                                <div className="text-lg font-bold text-slate-800 dark:text-slate-100 mt-1">
                                    {scoresObj.midterm ?? "-"}
                                </div>
                            </div>
                            <div className="p-3 bg-purple-50/50 dark:bg-purple-950/40 rounded-lg border border-purple-200">
                                <div className="text-xs text-purple-700 font-semibold">Mock Project</div>
                                <div className="text-lg font-bold text-purple-700 mt-1">
                                    {scoresObj.project ?? "-"}
                                </div>
                            </div>
                            <div className="p-3 bg-indigo-50/50 dark:bg-indigo-950/40 rounded-lg border border-indigo-200">
                                <div className="text-xs text-indigo-700 font-semibold">Cuối kỳ (Final)</div>
                                <div className="text-lg font-extrabold text-indigo-700 mt-1">
                                    {scoresObj.final ?? "-"}
                                </div>
                            </div>
                            <div className="p-3 bg-amber-50/50 dark:bg-amber-950/40 rounded-lg border border-amber-200">
                                <div className="text-xs text-amber-700 font-semibold">Điểm cộng (Bonus)</div>
                                <div className="text-lg font-bold text-amber-700 mt-1">
                                    {scoresObj.bonus !== undefined && scoresObj.bonus !== null && scoresObj.bonus !== "" ? `+${scoresObj.bonus}` : "-"}
                                </div>
                            </div>
                        </div>

                        <div className="mt-4 pt-3 border-t flex items-center justify-between">
                            <span className="text-sm font-semibold">Điểm tổng kết (Overall Score):</span>
                            <span className="text-2xl font-black text-blue-800 dark:text-blue-400">
                                {data.overallScore !== null && data.overallScore !== undefined ? data.overallScore : "-"}
                            </span>
                        </div>
                    </div>
                </CardContent>

                <CardFooter className="flex justify-between">
                    <Button variant="outline" onClick={() => navigate("/student-scores")}>
                        <ArrowLeft className="h-4 w-4 mr-2" />
                        Quay lại danh sách
                    </Button>

                    <PermissionGate permission="STUDENT_SCORE_UPDATE">
                        <Button asChild>
                            <Link to={`/student-scores/${id}/edit`}>
                                <Edit className="h-4 w-4 mr-2" />
                                Chỉnh sửa điểm
                            </Link>
                        </Button>
                    </PermissionGate>
                </CardFooter>
            </Card>
        </MainLayout>
    );
}


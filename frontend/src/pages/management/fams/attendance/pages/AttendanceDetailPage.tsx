import { attendanceApi } from "@/api";
import { MainLayout } from "@/components/layout/MainLayout";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { useQuery } from "@tanstack/react-query";
import { ArrowLeft, Edit3, Loader2 } from "lucide-react";
import { useNavigate, useParams } from "react-router-dom";

function formatDateTime(val?: string) {
  if (!val) return "-";
  const d = new Date(val);
  return isNaN(d.getTime()) ? val : d.toLocaleString("vi-VN");
}

function formatDate(val?: string) {
  if (!val) return "-";
  const d = new Date(val);
  return isNaN(d.getTime()) ? val : d.toLocaleDateString("vi-VN");
}

export function AttendanceDetailPage() {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();

  const attendanceQuery = useQuery({
    queryKey: ["attendance", "detail", id],
    queryFn: () => attendanceApi.getById(id as string),
    enabled: !!id,
  });

  const data = attendanceQuery.data;

  const renderStatusBadge = (status?: string) => {
    switch (status) {
      case "PRESENT":
        return <Badge className="bg-slate-900 text-white dark:bg-slate-100 dark:text-slate-900 font-medium">Có mặt (PRESENT)</Badge>;
      case "ABSENT":
        return <Badge className="bg-slate-900 text-white dark:bg-slate-100 dark:text-slate-900 font-medium">Vắng mặt (ABSENT)</Badge>;
      case "LATE":
        return <Badge className="bg-slate-900 text-white dark:bg-slate-100 dark:text-slate-900 font-medium">Đi muộn (LATE)</Badge>;
      case "EXCUSED":
        return <Badge className="bg-slate-900 text-white dark:bg-slate-100 dark:text-slate-900 font-medium">Có phép (EXCUSED)</Badge>;
      default:
        return <Badge variant="outline">{status || "-"}</Badge>;
    }
  };

  return (
    <MainLayout pathName={{ attendances: "Điểm danh", detail: "Chi tiết" }}>
      <div className="max-w-3xl mx-auto space-y-6 pb-12">
        <div className="flex items-center justify-between border-b pb-4">
          <div className="space-y-1">
            <h1 className="text-2xl font-bold tracking-tight">Chi Tiết Bản Ghi Điểm Danh</h1>
            <p className="text-sm text-muted-foreground">
              Thông tin chi tiết về phiên điểm danh của học viên.
            </p>
          </div>
          <div className="flex items-center gap-2">
            <Button variant="outline" onClick={() => navigate("/attendances")}>
              <ArrowLeft className="mr-2 h-4 w-4" /> Quay lại danh sách
            </Button>
            {id && (
              <Button onClick={() => navigate(`/attendances/${id}/edit`)}>
                <Edit3 className="mr-2 h-4 w-4" /> Chỉnh sửa
              </Button>
            )}
          </div>
        </div>

        {attendanceQuery.isLoading ? (
          <div className="flex items-center justify-center p-12 text-muted-foreground gap-3">
            <Loader2 className="h-8 w-8 animate-spin text-primary" />
            <p>Đang tải thông tin...</p>
          </div>
        ) : !data ? (
          <Card>
            <CardHeader>
              <CardTitle>Không tìm thấy</CardTitle>
              <CardDescription>Bản ghi điểm danh này không tồn tại trong hệ thống.</CardDescription>
            </CardHeader>
          </Card>
        ) : (
          <Card>
            <CardHeader>
              <CardTitle className="text-base">Thông tin buổi điểm danh</CardTitle>
            </CardHeader>
            <CardContent className="space-y-6">
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                <div className="p-3 bg-muted/30 rounded border">
                  <span className="text-xs text-muted-foreground block">Mã học viên</span>
                  <span className="font-semibold text-primary font-mono">{data.studentCode || "-"}</span>
                </div>
                <div className="p-3 bg-muted/30 rounded border">
                  <span className="text-xs text-muted-foreground block">Họ và tên học viên</span>
                  <span className="font-semibold">{data.studentName || "-"}</span>
                </div>
                <div className="p-3 bg-muted/30 rounded border">
                  <span className="text-xs text-muted-foreground block">Lớp học</span>
                  <span className="font-semibold">{data.className || "-"}</span>
                </div>
                <div className="p-3 bg-muted/30 rounded border">
                  <span className="text-xs text-muted-foreground block">Ngày điểm danh</span>
                  <span className="font-semibold">{formatDate(data.attendanceDate)}</span>
                </div>
                <div className="p-3 bg-muted/30 rounded border">
                  <span className="text-xs text-muted-foreground block">Ca học (Slot)</span>
                  <span className="font-semibold">{data.slot || "-"}</span>
                </div>
                <div className="p-3 bg-muted/30 rounded border">
                  <span className="text-xs text-muted-foreground block mb-1">Trạng thái</span>
                  {renderStatusBadge(data.status)}
                </div>
              </div>

              <div className="p-3 bg-muted/30 rounded border space-y-1">
                <span className="text-xs text-muted-foreground block">Ghi chú</span>
                <p className="text-sm italic">{data.note ? data.note : "Không có ghi chú"}</p>
              </div>

              <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 text-xs text-muted-foreground pt-4 border-t">
                <div>
                  <span>Ngày tạo: </span>
                  <span className="font-medium text-foreground">{formatDateTime(data.createdAt)}</span>
                </div>
                <div>
                  <span>Ngày cập nhật gần nhất: </span>
                  <span className="font-medium text-foreground">{formatDateTime(data.updatedAt)}</span>
                </div>
              </div>
            </CardContent>
          </Card>
        )}
      </div>
    </MainLayout>
  );
}

import { attendanceApi } from "@/api";
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
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Switch } from "@/components/ui/switch";
import { Textarea } from "@/components/ui/textarea";
import type { AttendanceDTO } from "@/types";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ArrowLeft, Loader2, Save, UserCheck } from "lucide-react";
import { useEffect, useState, type FormEvent } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { toast } from "sonner";

export function AttendanceEditPage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { id } = useParams<{ id: string }>();

  const [form, setForm] = useState<Partial<AttendanceDTO>>({
    status: "PRESENT",
    slot: "ALL_DAY",
    attendanceDate: "",
    note: "",
    isActive: true,
  });

  const attendanceQuery = useQuery({
    queryKey: ["attendance", "detail", id],
    queryFn: () => attendanceApi.getById(id as string),
    enabled: !!id,
  });

  useEffect(() => {
    if (!attendanceQuery.data) return;
    const data = attendanceQuery.data;
    // Cắt chuỗi ISO yyyy-mm-dd
    const dateFormatted = data.attendanceDate ? data.attendanceDate.split("T")[0] : "";
    setForm({
      ...data,
      attendanceDate: dateFormatted,
      status: data.status || "PRESENT",
      slot: data.slot || "ALL_DAY",
      note: data.note || "",
      isActive: data.isActive ?? true,
    });
  }, [attendanceQuery.data]);

  const updateMutation = useMutation({
    mutationFn: (payload: AttendanceDTO) => attendanceApi.update(id as string, payload),
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ["attendance"] }),
        queryClient.invalidateQueries({ queryKey: ["attendance", "detail", id] }),
      ]);
      toast.success("Cập nhật bản ghi điểm danh thành công!");
      navigate("/attendances");
    },
    onError: (error: any) => {
      toast.error(error?.response?.data?.message || "Cập nhật điểm danh thất bại.");
    },
  });

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault();
    if (!form.attendanceDate) {
      toast.error("Vui lòng chọn ngày điểm danh.");
      return;
    }

    updateMutation.mutate(form as AttendanceDTO);
  };

  if (!id) {
    return (
      <MainLayout pathName={{ attendances: "Điểm danh", edit: "Chỉnh sửa" }}>
        <Card className="max-w-xl mx-auto mt-10">
          <CardHeader>
            <CardTitle>Không tìm thấy ID</CardTitle>
            <CardDescription>Bản ghi điểm danh cần chỉnh sửa không tồn tại.</CardDescription>
          </CardHeader>
          <CardFooter>
            <Button variant="outline" onClick={() => navigate("/attendances")}>
              <ArrowLeft className="mr-2 h-4 w-4" /> Quay lại danh sách
            </Button>
          </CardFooter>
        </Card>
      </MainLayout>
    );
  }

  return (
    <MainLayout pathName={{ attendances: "Điểm danh", edit: "Chỉnh sửa" }}>
      <div className="max-w-3xl mx-auto space-y-6 pb-12">
        <div className="flex items-center justify-between border-b pb-4">
          <div className="space-y-1">
            <h1 className="text-2xl font-bold tracking-tight">Chỉnh Sửa Bản Ghi Điểm Danh</h1>
            <p className="text-sm text-muted-foreground">
              Cập nhật ngày, ca học, trạng thái hoặc lý do xin nghỉ cho học viên.
            </p>
          </div>
          <Button variant="outline" onClick={() => navigate("/attendances")}>
            <ArrowLeft className="mr-2 h-4 w-4" /> Quay lại danh sách
          </Button>
        </div>

        {attendanceQuery.isLoading ? (
          <div className="flex items-center justify-center p-12 text-muted-foreground gap-3">
            <Loader2 className="h-8 w-8 animate-spin text-primary" />
            <p>Đang tải thông tin điểm danh...</p>
          </div>
        ) : (
          <form onSubmit={handleSubmit}>
            <Card>
              <CardHeader>
                <CardTitle className="text-base flex items-center gap-2">
                  <UserCheck className="h-5 w-5 text-primary" /> Thông tin học viên & lớp học
                </CardTitle>
                <CardDescription>
                  Học viên và lớp học được gắn cố định với bản ghi này.
                </CardDescription>
              </CardHeader>

              <CardContent className="space-y-6">
                {/* Thông tin cố định */}
                <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 p-4 rounded-lg bg-muted/40 border">
                  <div>
                    <span className="text-xs text-muted-foreground block">Mã học viên</span>
                    <span className="font-semibold text-primary font-mono text-sm">
                      {form.studentCode || "-"}
                    </span>
                  </div>
                  <div>
                    <span className="text-xs text-muted-foreground block">Họ và tên</span>
                    <span className="font-medium text-foreground text-sm">
                      {form.studentName || "-"}
                    </span>
                  </div>
                  <div>
                    <span className="text-xs text-muted-foreground block">Lớp học</span>
                    <span className="font-medium text-foreground text-sm">
                      {form.className || "-"}
                    </span>
                  </div>
                </div>

                {/* Các trường có thể sửa */}
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-6">
                  <div className="space-y-2">
                    <Label htmlFor="edit-date" className="text-sm font-medium">
                      Ngày điểm danh <span className="text-destructive">*</span>
                    </Label>
                    <Input
                      id="edit-date"
                      type="date"
                      value={form.attendanceDate || ""}
                      onChange={(e) => setForm((prev) => ({ ...prev, attendanceDate: e.target.value }))}
                      required
                    />
                  </div>

                  <div className="space-y-2">
                    <Label htmlFor="edit-slot" className="text-sm font-medium">
                      Ca học <span className="text-destructive">*</span>
                    </Label>
                    <Select
                      value={form.slot || "ALL_DAY"}
                      onValueChange={(val) => setForm((prev) => ({ ...prev, slot: val }))}
                    >
                      <SelectTrigger id="edit-slot" className="w-full">
                        <SelectValue placeholder="Chọn ca học" />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="ALL_DAY">Cả ngày (ALL_DAY)</SelectItem>
                        <SelectItem value="MORNING">Ca sáng (MORNING)</SelectItem>
                        <SelectItem value="AFTERNOON">Ca chiều (AFTERNOON)</SelectItem>
                      </SelectContent>
                    </Select>
                  </div>
                </div>

                <div className="space-y-2">
                  <Label htmlFor="edit-status" className="text-sm font-medium">
                    Trạng thái điểm danh <span className="text-destructive">*</span>
                  </Label>
                  <Select
                    value={form.status || "PRESENT"}
                    onValueChange={(val) => setForm((prev) => ({ ...prev, status: val }))}
                  >
                    <SelectTrigger id="edit-status" className="w-full">
                      <SelectValue placeholder="Chọn trạng thái" />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="PRESENT">
                        <span className="text-slate-900 dark:text-slate-100 font-medium">Có mặt (PRESENT)</span>
                      </SelectItem>
                      <SelectItem value="ABSENT">
                        <span className="text-slate-900 dark:text-slate-100 font-medium">Vắng mặt (ABSENT)</span>
                      </SelectItem>
                      <SelectItem value="LATE">
                        <span className="text-slate-900 dark:text-slate-100 font-medium">Đi muộn (LATE)</span>
                      </SelectItem>
                      <SelectItem value="EXCUSED">
                        <span className="text-slate-900 dark:text-slate-100 font-medium">Có phép (EXCUSED)</span>
                      </SelectItem>
                    </SelectContent>
                  </Select>
                </div>

                <div className="space-y-2">
                  <Label htmlFor="edit-note" className="text-sm font-medium">
                    Ghi chú / Lý do
                  </Label>
                  <Textarea
                    id="edit-note"
                    placeholder="Ghi chú lý do vắng mặt, đến muộn hoặc bổ sung tài liệu minh chứng..."
                    rows={3}
                    value={form.note || ""}
                    onChange={(e) => setForm((prev) => ({ ...prev, note: e.target.value }))}
                  />
                </div>

                <div className="flex items-center justify-between p-4 border rounded-lg">
                  <div className="space-y-0.5">
                    <Label htmlFor="edit-is-active" className="text-sm font-medium">
                      Trạng thái kích hoạt (Is Active)
                    </Label>
                    <p className="text-xs text-muted-foreground">
                      Bản ghi điểm danh có hiệu lực trong việc tính toán tỷ lệ tham gia lớp học.
                    </p>
                  </div>
                  <Switch
                    id="edit-is-active"
                    checked={form.isActive ?? true}
                    onCheckedChange={(checked) => setForm((prev) => ({ ...prev, isActive: checked }))}
                  />
                </div>
              </CardContent>

              <CardFooter className="flex items-center justify-between border-t py-4">
                <Button variant="outline" type="button" onClick={() => navigate("/attendances")}>
                  Hủy bỏ
                </Button>
                <Button type="submit" disabled={updateMutation.isPending}>
                  {updateMutation.isPending ? (
                    <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  ) : (
                    <Save className="mr-2 h-4 w-4" />
                  )}
                  Lưu thay đổi
                </Button>
              </CardFooter>
            </Card>
          </form>
        )}
      </div>
    </MainLayout>
  );
}

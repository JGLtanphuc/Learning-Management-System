import { attendanceApi, classApi } from "@/api";
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
import type { ClassAttendanceItemDTO, ClassDTO, SaveClassAttendanceRequest } from "@/types";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  ArrowLeft,
  CheckCircle2,
  CheckCheck,
  Clock,
  HelpCircle,
  Loader2,
  Save,
  Users,
  XCircle,
} from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { toast } from "sonner";

const STATUS_OPTIONS = [
  { value: "PRESENT", label: "Có mặt", icon: CheckCircle2 },
  { value: "ABSENT", label: "Vắng mặt", icon: XCircle },
  { value: "LATE", label: "Đi muộn", icon: Clock },
  { value: "EXCUSED", label: "Có phép", icon: HelpCircle },
];

function getTodayIsoString() {
  const today = new Date();
  const year = today.getFullYear();
  const month = String(today.getMonth() + 1).padStart(2, "0");
  const day = String(today.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

export function AttendanceCheckInPage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();

  const [selectedClassId, setSelectedClassId] = useState<string>("");
  const [selectedDate, setSelectedDate] = useState<string>(getTodayIsoString());
  const [selectedSlot, setSelectedSlot] = useState<string>("ALL_DAY");
  const [items, setItems] = useState<ClassAttendanceItemDTO[]>([]);

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

  // Query sheet điểm danh của lớp được chọn
  const sheetQuery = useQuery({
    queryKey: ["attendance", "class-sheet", selectedClassId, selectedDate, selectedSlot],
    queryFn: () => attendanceApi.getClassSheet(Number(selectedClassId), selectedDate, selectedSlot),
    enabled: !!selectedClassId,
  });

  // Cập nhật state items khi dữ liệu sheet trả về
  useEffect(() => {
    if (sheetQuery.data?.students) {
      setItems(sheetQuery.data.students);
    }
  }, [sheetQuery.data]);

  // Mutation lưu điểm danh hàng loạt
  const saveMutation = useMutation({
    mutationFn: (payload: SaveClassAttendanceRequest) => attendanceApi.saveBatch(payload),
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ["attendance"] }),
        queryClient.invalidateQueries({ queryKey: ["attendance", "class-sheet", selectedClassId, selectedDate, selectedSlot] }),
      ]);
      toast.success("Lưu kết quả điểm danh thành công!");
      navigate("/attendances");
    },
    onError: (error: any) => {
      toast.error(error?.response?.data?.message || "Lưu điểm danh thất bại.");
    },
  });

  // Handler cập nhật trạng thái điểm danh cho 1 học viên
  const handleStatusChange = (studentId: number, status: string) => {
    setItems((prev) =>
      prev.map((it) => (it.studentId === studentId ? { ...it, status } : it))
    );
  };

  // Handler cập nhật ghi chú cho 1 học viên
  const handleNoteChange = (studentId: number, note: string) => {
    setItems((prev) =>
      prev.map((it) => (it.studentId === studentId ? { ...it, note } : it))
    );
  };

  // Đánh dấu tất cả có mặt
  const handleMarkAll = (status: string) => {
    setItems((prev) => prev.map((it) => ({ ...it, status })));
  };

  // Thống kê nhanh
  const stats = useMemo(() => {
    const total = items.length;
    const present = items.filter((i) => i.status === "PRESENT").length;
    const absent = items.filter((i) => i.status === "ABSENT").length;
    const late = items.filter((i) => i.status === "LATE").length;
    const excused = items.filter((i) => i.status === "EXCUSED").length;
    return { total, present, absent, late, excused };
  }, [items]);

  const handleSave = () => {
    if (!selectedClassId) {
      toast.error("Vui lòng chọn lớp học.");
      return;
    }

    if (items.length === 0) {
      toast.error("Lớp này chưa có học viên nào để điểm danh.");
      return;
    }

    saveMutation.mutate({
      classId: Number(selectedClassId),
      attendanceDate: selectedDate,
      slot: selectedSlot,
      items: items.map((i) => ({
        studentId: i.studentId,
        status: i.status,
        note: i.note,
      })),
    });
  };

  return (
    <MainLayout pathName={{ attendances: "Điểm danh", create: "Điểm danh theo lớp" }}>
      <div className="space-y-6 max-w-7xl mx-auto pb-12">
        {/* Header toolbar */}
        <div className="flex flex-wrap items-center justify-between gap-4 border-b pb-4">
          <div className="space-y-1">
            <h1 className="text-2xl font-bold tracking-tight">Sổ Điểm Danh Học Viên Theo Lớp</h1>
            <p className="text-muted-foreground text-sm">
              Chọn lớp học, ngày và ca học để theo dõi và cập nhật trạng thái có mặt của từng học viên.
            </p>
          </div>
          <div className="flex flex-wrap items-center gap-2 w-full sm:w-auto">
            <Button variant="outline" onClick={() => navigate("/attendances")} className="flex-1 sm:flex-initial">
              <ArrowLeft className="mr-2 h-4 w-4" /> Quay lại
            </Button>
            <Button onClick={handleSave} disabled={saveMutation.isPending || items.length === 0} className="flex-1 sm:flex-initial">
              {saveMutation.isPending ? (
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
              ) : (
                <Save className="mr-2 h-4 w-4" />
              )}
              Lưu điểm danh ({items.length})
            </Button>
          </div>
        </div>

        {/* Bộ lọc chọn Lớp, Ngày, Ca */}
        <Card className="bg-card/50 backdrop-blur-sm border shadow-sm">
          <CardHeader className="pb-3">
            <CardTitle className="text-base font-semibold flex items-center gap-2">
              <Users className="h-5 w-5 text-primary" /> Thiết lập phiên điểm danh
            </CardTitle>
            <CardDescription>
              Thay đổi lớp hoặc ngày sẽ tự động tải dữ liệu điểm danh đã lưu trước đó nếu có.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
              {/* Chọn lớp */}
              <div className="space-y-2">
                <Label htmlFor="class-select" className="font-medium text-sm">
                  Lớp học <span className="text-destructive">*</span>
                </Label>
                <Select
                  value={selectedClassId}
                  onValueChange={(val) => setSelectedClassId(val)}
                  disabled={classesQuery.isLoading}
                >
                  <SelectTrigger id="class-select" className="w-full">
                    <SelectValue placeholder={classesQuery.isLoading ? "Đang tải lớp..." : "Chọn lớp học"} />
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

              {/* Chọn ngày */}
              <div className="space-y-2">
                <Label htmlFor="attendance-date" className="font-medium text-sm">
                  Ngày điểm danh <span className="text-destructive">*</span>
                </Label>
                <Input
                  id="attendance-date"
                  type="date"
                  value={selectedDate}
                  onChange={(e) => setSelectedDate(e.target.value)}
                  className="w-full"
                />
              </div>

              {/* Chọn ca / slot */}
              <div className="space-y-2">
                <Label htmlFor="slot-select" className="font-medium text-sm">
                  Ca học <span className="text-destructive">*</span>
                </Label>
                <Select value={selectedSlot} onValueChange={(val) => setSelectedSlot(val)}>
                  <SelectTrigger id="slot-select" className="w-full">
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
          </CardContent>
        </Card>

        {/* Thanh thống kê nhanh & phím tắt */}
        <div className="grid grid-cols-2 sm:grid-cols-5 gap-3">
          <Card className="p-3 bg-muted/40 border border-slate-200 dark:border-slate-800">
            <div className="text-xs font-medium text-slate-500 dark:text-slate-400">Tổng học viên</div>
            <div className="text-2xl font-bold mt-1 text-slate-900 dark:text-slate-100">{stats.total}</div>
          </Card>
          <Card className="p-3 bg-muted/40 border border-slate-200 dark:border-slate-800">
            <div className="text-xs font-medium text-slate-500 dark:text-slate-400">Có mặt (PRESENT)</div>
            <div className="text-2xl font-bold mt-1 text-slate-900 dark:text-slate-100">{stats.present}</div>
          </Card>
          <Card className="p-3 bg-muted/40 border border-slate-200 dark:border-slate-800">
            <div className="text-xs font-medium text-slate-500 dark:text-slate-400">Vắng mặt (ABSENT)</div>
            <div className="text-2xl font-bold mt-1 text-slate-900 dark:text-slate-100">{stats.absent}</div>
          </Card>
          <Card className="p-3 bg-muted/40 border border-slate-200 dark:border-slate-800">
            <div className="text-xs font-medium text-slate-500 dark:text-slate-400">Đi muộn (LATE)</div>
            <div className="text-2xl font-bold mt-1 text-slate-900 dark:text-slate-100">{stats.late}</div>
          </Card>
          <Card className="p-3 bg-muted/40 border border-slate-200 dark:border-slate-800">
            <div className="text-xs font-medium text-slate-500 dark:text-slate-400">Có phép (EXCUSED)</div>
            <div className="text-2xl font-bold mt-1 text-slate-900 dark:text-slate-100">{stats.excused}</div>
          </Card>
        </div>

        {/* Danh sách học viên */}
        <Card>
          <CardHeader className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3 pb-4">
            <div>
              <CardTitle className="text-lg">
                Danh sách học viên {sheetQuery.data ? `(${sheetQuery.data.classCode} - ${sheetQuery.data.className})` : ""}
              </CardTitle>
              <CardDescription>
                Nhấp vào trạng thái tương ứng để điểm danh từng học viên hoặc sử dụng nút thao tác nhanh.
              </CardDescription>
            </div>
            {items.length > 0 && (
              <div className="flex flex-wrap items-center gap-2">
                <Button
                  size="sm"
                  variant="outline"
                  className="border-slate-300 text-slate-800 hover:bg-slate-100 dark:border-slate-700 dark:text-slate-200 dark:hover:bg-slate-800 flex-1 sm:flex-initial"
                  onClick={() => handleMarkAll("PRESENT")}
                >
                  <CheckCheck className="h-4 w-4 mr-1" /> Tất cả có mặt
                </Button>
                <Button
                  size="sm"
                  variant="outline"
                  className="border-slate-300 text-slate-800 hover:bg-slate-100 dark:border-slate-700 dark:text-slate-200 dark:hover:bg-slate-800 flex-1 sm:flex-initial"
                  onClick={() => handleMarkAll("ABSENT")}
                >
                  <XCircle className="h-4 w-4 mr-1" /> Tất cả vắng
                </Button>
              </div>
            )}
          </CardHeader>
          <CardContent className="p-0">
            {sheetQuery.isLoading ? (
              <div className="flex flex-col items-center justify-center p-12 text-muted-foreground gap-3">
                <Loader2 className="h-8 w-8 animate-spin text-primary" />
                <p>Đang tải danh sách học viên...</p>
              </div>
            ) : items.length === 0 ? (
              <div className="flex flex-col items-center justify-center p-12 text-center text-muted-foreground">
                <Users className="h-10 w-10 text-muted-foreground/50 mb-2" />
                <p className="font-semibold">Không tìm thấy học viên trong lớp này</p>
                <p className="text-xs text-muted-foreground mt-1">
                  Vui lòng kiểm tra lại cấu hình phân bổ học viên vào lớp học.
                </p>
              </div>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full text-sm text-left">
                  <thead className="text-xs uppercase bg-muted/50 border-y text-muted-foreground">
                    <tr>
                      <th className="px-4 py-3 w-12 text-center">STT</th>
                      <th className="px-4 py-3 w-32">Mã HV</th>
                      <th className="px-4 py-3">Họ và tên</th>
                      <th className="px-4 py-3 w-80 text-center">Trạng thái điểm danh</th>
                      <th className="px-4 py-3 min-w-[200px]">Ghi chú</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y">
                    {items.map((student, idx) => (
                      <tr key={student.studentId} className="hover:bg-muted/30 transition-colors">
                        <td className="px-4 py-3 text-center text-muted-foreground font-mono text-xs">
                          {idx + 1}
                        </td>
                        <td className="px-4 py-3 font-semibold text-primary font-mono text-sm">
                          {student.studentCode}
                        </td>
                        <td className="px-4 py-3">
                          <div className="font-medium text-foreground">{student.studentName}</div>
                          {student.email && (
                            <div className="text-xs text-muted-foreground">{student.email}</div>
                          )}
                        </td>
                        <td className="px-4 py-3">
                          <div className="flex items-center justify-center gap-1.5 flex-wrap">
                            {STATUS_OPTIONS.map((opt) => {
                              const isSelected = student.status === opt.value;
                              return (
                                <button
                                  key={opt.value}
                                  type="button"
                                  onClick={() => handleStatusChange(student.studentId, opt.value)}
                                  className={`px-2.5 py-1.5 rounded-md text-xs font-semibold border transition-all flex items-center gap-1 ${
                                    isSelected
                                      ? "bg-slate-900 text-white dark:bg-slate-100 dark:text-slate-900 border-slate-900 dark:border-slate-100 shadow-xs ring-2 ring-slate-900/10 scale-105"
                                      : "bg-white text-slate-600 dark:bg-slate-900 dark:text-slate-400 border-slate-200 dark:border-slate-700 hover:bg-slate-100 dark:hover:bg-slate-800"
                                  }`}
                                >
                                  <opt.icon className="h-3.5 w-3.5" />
                                  {opt.label}
                                </button>
                              );
                            })}
                          </div>
                        </td>
                        <td className="px-4 py-3">
                          <Input
                            placeholder="Ghi chú lý do nếu vắng/muộn..."
                            value={student.note || ""}
                            onChange={(e) => handleNoteChange(student.studentId, e.target.value)}
                            className="h-8 text-xs bg-background"
                          />
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </CardContent>
          {items.length > 0 && (
            <CardFooter className="flex items-center justify-between border-t py-4">
              <div className="text-sm text-muted-foreground">
                Đã hiển thị <span className="font-semibold text-foreground">{items.length}</span> học viên
              </div>
              <div className="flex items-center gap-2">
                <Button variant="outline" onClick={() => navigate("/attendances")}>
                  Hủy bỏ
                </Button>
                <Button onClick={handleSave} disabled={saveMutation.isPending}>
                  {saveMutation.isPending ? (
                    <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  ) : (
                    <Save className="mr-2 h-4 w-4" />
                  )}
                  Lưu điểm danh
                </Button>
              </div>
            </CardFooter>
          )}
        </Card>
      </div>
    </MainLayout>
  );
}

/**
 * ============================================================================
 * BARREL EXPORT FILE (Cổng gom và tái xuất khẩu API tập trung)
 * ============================================================================
 * Mục đích:
 * Gom tất cả các API module riêng lẻ trong thư mục `features/` vào một nơi duy nhất.
 * Giúp các Component ở tầng giao diện có thể import bất kỳ API nào chỉ từ `@/api`.
 */

// 1. Nhóm API Xác thực & Phân quyền cơ bản
export * from "./features/auth.api";

// 2. Nhóm API Demo mẫu (Sách, Điểm số, Bảng điểm)
export * from "./features/demo/book.api";
export * from "./features/demo/student_score.api";
export * from "./features/demo/topic_mark_gradebook.api";

// 3. Nhóm API Quản lý Menu & Module hiển thị động
export * from "./features/menu/module.api";
export * from "./features/menu/module_group.api";

// 4. Nhóm API Phân quyền người dùng (RBAC - Role-Based Access Control)
export * from "./features/rbac/permission.api";
export * from "./features/rbac/role.api";
export * from "./features/rbac/user.api";

// 5. Nhóm API Nghiệp vụ Quản lý đào tạo (FAMS - Training Management)
export * from "./features/fams/class.api";
export * from "./features/fams/teacher.api";
export * from "./features/fams/student.api";
export * from "./features/fams/attendance.api";
export * from "./features/fams/mock_project.api";
export * from "./features/fams/allocation.api";

// 6. Nhóm API Module Phỏng vấn (Interview Module)
export * from "./features/interview/interview.api";

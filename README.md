# Fresher Academy System (FAS System)

> **Hệ thống Quản trị Đào tạo, Học tập, Điểm số và Đánh giá Phỏng vấn Kỹ thuật Hợp nhất**  
> Dự án được xây dựng trên nền tảng công nghệ hiện đại: **.NET 8 Web API + React 19 + TypeScript + Tailwind CSS + SQL Server 2022 + Google Gemini AI**.

---

## 📖 Mục lục

1. [Tổng quan Kiến trúc Hệ thống](#-tổng-quan-kiến-trúc-hệ-thống)
2. [Các Phân hệ Tính năng Nổi bật](#-các-phân-hệ-tính-năng-nổi-bật)
3. [Hướng dẫn Khởi động Nhanh](#-hướng-dẫn-khởi-động-nhanh)
   - [Cách 1: Đóng gói & Chạy bằng Docker Compose (Khuyến nghị)](#cách-1-đóng-gói--chạy-bằng-docker-compose-khuyến-nghị)
4. [Cơ sở Dữ liệu & Script Khởi tạo](#-cơ-sở-dữ-liệu--script-khởi-tạo)
5. [Tài khoản Kiểm thử Mặc định (Credentials)](#-tài-khoản-kiểm-thử-mặc-định-credentials)
6. [Quy chuẩn Thiết kế & Phát triển (AI Guidelines)](#-quy-chuẩn-thiết-kế--phát-triển-ai-guidelines)

---

## 🏛️ Tổng quan Kiến trúc Hệ thống

Hệ thống tuân thủ mô hình **Layered Monolith** với một dự án Backend duy nhất, đảm bảo tính nhất quán, hiệu năng cao và phân tách trách nhiệm rõ ràng:

```
                            ┌─────────────────────────────────┐
                            │    Client Browser / End User    │
                            └────────────────┬────────────────┘
                                             │ HTTP/REST (Port 5172)
                                             ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│ FAS Backend Web API (.NET 8 - ASP.NET Core)                                 │
│                                                                             │
│ ┌───────────────────────┐   Controllers     (Tiếp nhận & Validate HTTP)     │
│ └──────────┬────────────┘                                                   │
│            ▼                                                                │
│ ┌───────────────────────┐   Services        (Nghiệp vụ, RBAC, Gemini AI)    │
│ └──────────┬────────────┘                                                   │
│            ▼                                                                │
│ ┌───────────────────────┐   Repositories    (Truy vấn EF Core Asynchronous) │
│ └──────────┬────────────┘                                                   │
│            ▼                                                                │
│ ┌───────────────────────┐   Data & Entities (Fluent API Mappings)           │
│ └───────────────────────┘                                                   │
└───────────────────────┬───────────────────────────────┬─────────────────────┘
                        │ SQL Connection (Port 1433)    │ Gemini API (HTTPS)
                        ▼                               ▼
        ┌───────────────────────────────┐       ┌─────────────────────────────┐
        │ Microsoft SQL Server 2022     │       │ Google Gemini 3.8 Live API  │
        │ Database: [StarterProjectDb]  │       │ (Auto-Grading & Assessment) │
        └───────────────────────────────┘       └─────────────────────────────┘
```

### Công nghệ sử dụng:

- **Frontend**: React 19, Vite, TypeScript, Tailwind CSS, TanStack React Query, Lucide Icons, Sonner Toast, Web Speech API.
- **Backend**: ASP.NET Core 8 Web API, C#, Entity Framework Core 8, JWT Authentication & RBAC, Mapperly.
- **AI Engine**: Google Gemini API (`models/gemini-3.8-live` / `gemini-1.5-flash`).
- **Database**: Microsoft SQL Server 2022 (22 Tables, 1,596 Seed Records).
- **Containerization**: Docker Multi-stage Builds, Docker Compose, Nginx Reverse Proxy & Static Server.

---

## 🚀 Các Phân hệ Tính năng Nổi bật

### 1. Quản lý Đào tạo Academy (FAS Core)

- **Lớp đào tạo (`/classes`)**: Quản lý các lớp học Fresher, mã lớp, chuyên ngành đào tạo, ngày bắt đầu/kết thúc, phòng Lab, chỉ tiêu sĩ số, giảng viên chủ nhiệm và vòng đời lớp (`PLANNING` ➔ `ONGOING` ➔ `COMPLETED`).
- **Học viên Fresher (`/students`)**: Quản lý danh sách học viên, mã HV, thông tin liên lạc, trường đại học, chuyên ngành, điểm GPA tích lũy, trạng thái học tập (`ACTIVE`, `RESERVE`, `DROP_OUT`, `COMPLETED`).
- **Điểm danh chuyên cần (`/attendances`)**: Điểm danh 2 buổi sáng/chiều mỗi ngày, theo dõi các trạng thái chuyên cần (`PRESENT`, `ABSENT`, `LATE`, `EXCUSED`), ghi chú lý do và tính tỷ lệ chuyên cần.

### 2. Sổ Điểm Môn Học Động (Dynamic Gradebook)

- **Sổ điểm & Cột điểm linh hoạt (`/student-scores`)**: Tự động sinh cấu trúc cột điểm theo từng môn học (Quiz, Assignment, Midterm, Final, Capstone).
- **Tự động xếp loại**: Tính điểm trung bình tổng kết (Overall Score) và xếp hạng học lực.
- **Phân quyền theo lớp**: Giảng viên chỉ được xem và nhập điểm cho các lớp học do chính mình phụ trách.

### 3. Đồ án Tốt nghiệp (Mock Project)

- **Mock Projects (`/mock-projects`)**: Quản lý các đồ án tốt nghiệp mô phỏng dự án thực tế tại FPT Software, phân công Mentor hướng dẫn, công nghệ ứng dụng (Tech Stack: Java/Spring, .NET, React, Cloud), liên kết GitHub Repository và chấm điểm Sprint/Final.

### 4. Phân bổ Dự án Sản xuất (Production Allocation)

- **Điều phối & Bàn giao BU (`/allocations`)**: Đề xuất học viên tốt nghiệp sang các Đơn vị Sản xuất phần mềm (BU: FSU1, DXL, GAM...), vai trò mục tiêu (Backend, Frontend, Fullstack), thuật toán tính điểm phù hợp (Match Score), theo dõi phỏng vấn và trạng thái tiếp nhận (`PROPOSED` ➔ `INTERVIEWING` ➔ `ALLOCATED`).

### 5. Phòng Phỏng vấn Kỹ thuật AI & Voice Telemetry (AI Technical Interview Engine)

- **Ngân hàng câu hỏi (`/interview-questions`)**: Phân loại theo chủ đề (`Java`, `.NET`, `React`, `Database`, `Soft Skills`), hỗ trợ cả dạng câu hỏi **Trắc nghiệm (MCQ)** kèm JSON phương án và câu hỏi **Tự luận kỹ thuật**.
- **Đợt thi tuyển (`/interview-assignments`)**: Cấu hình thời gian làm bài, ma trận độ khó (Dễ/Trung bình/Khó), điểm sàn đạt chuẩn (Passing Score).
- **Phòng thi Dual Arena (`/mock-interview`)**:
  - **Voice Telemetry**: Nhận diện giọng nói thời gian thực (Speech-to-Text), sóng âm trực quan và ghi nhận câu trả lời thoại.
  - **Text & Code Editor**: Hỗ trợ nhập liệu văn bản tự luận và đoạn mã minh họa.
  - **Giám sát thi cử (Proctoring Telemetry)**: Tự động đếm và ghi nhận số lần học viên chuyển tab/rời màn hình thi.
  - **Đồng hồ đếm ngược & Tự nộp khi hết giờ**: Tự động khóa và gửi bài về máy chủ khi thời gian về `00:00`.
  - **Chấm điểm tự động qua AI**: Tự động so sánh với đáp án chuẩn, đánh giá qua Gemini AI, đồng thời Giảng viên có thể chấm điểm và gửi phản hồi chính thức.

### 6. Cách ly Dữ liệu & Xuất Báo cáo CSV

- **Cách ly phạm vi xuất dữ liệu (Data Scope Isolation)**: Giảng viên chỉ được phép xuất dữ liệu của học viên và lớp học mình phụ trách. Quản trị viên (Admin) có toàn quyền xuất dữ liệu toàn viện.
- **Chuẩn xuất CSV**: Toàn bộ 12 dịch vụ xuất dữ liệu đều tích hợp chuẩn mã hóa UTF-8 kèm BOM (`\uFEFF`), giúp mở trực tiếp trên Microsoft Excel mà không bao giờ bị lỗi font tiếng Việt có dấu.

---

## 🛠️ Hướng dẫn Khởi động Nhanh

### Cách 1: Đóng gói & Chạy bằng Docker Compose (Khuyến nghị)

Hệ thống đã đóng gói sẵn 4 container (`sqlserver`, `sqlserver-init`, `backend`, `frontend`). Bạn chỉ cần chạy một lệnh duy nhất:

```bash
docker compose up -d --build
```

Sau khi hoàn tất, mở trình duyệt và truy cập:

- 🌐 **Giao diện Web Portal**: [http://localhost:3000](http://localhost:3000)
- 📑 **Tài liệu Backend Swagger**: [http://localhost:5172/swagger](http://localhost:5172/swagger)
- 🗄️ **Cơ sở dữ liệu SQL Server**: `localhost,14333` (User: `sa`, Password: `Password123!`)

Dừng hệ thống:

```bash
docker compose down
```

---

## 🗄️ Cơ sở Dữ liệu & Script Khởi tạo

Toàn bộ CSDL của hệ thống được quản lý tập trung tại tệp:  
📁 **[`database/FAS_System.sql`](./database/FAS_System.sql)**

### Các bảng dữ liệu chính (22 Tables):

| Nhóm chức năng           | Danh sách Bảng                                                                                                |
| :----------------------- | :------------------------------------------------------------------------------------------------------------ |
| **Bảo mật & Phân quyền** | `users`, `roles`, `user_roles`, `permissions`, `role_permissions`, `refresh_token`, `one_time_tokens`         |
| **Quản lý Đào tạo**      | `classes`, `students`, `attendances`                                                                          |
| **Sổ điểm & Học liệu**   | `books`, `module_groups`, `modules`, `student_scores`, `student_score_columns`                                |
| **Đồ án & Bàn giao BU**  | `mock_projects`, `allocations`                                                                                |
| **Phỏng vấn Kỹ thuật**   | `interview_topics`, `interview_questions`, `interview_assignments`, `interview_sessions`, `interview_answers` |

### Hướng dẫn khởi tạo thủ công bằng SSMS / Azure Data Studio:

1. Mở **SQL Server Management Studio (SSMS)** và kết nối tới máy chủ SQL Server của bạn.
2. Mở file `database/FAS_System.sql`.
3. Nhấn **Execute (F5)** để tự động tạo Database `[StarterProjectDb]`, toàn bộ cấu trúc 22 bảng cùng 1,596 bản ghi dữ liệu mẫu.

---

## 🔑 Tài khoản Kiểm thử Mặc định (Credentials)

Toàn bộ tài khoản mẫu đều dùng chung mật khẩu: **`password123`**

| Vai trò (Role)             | Email đăng nhập      |   Mật khẩu    | Quyền hạn & Phạm vi                                                             |
| :------------------------- | :------------------- | :-----------: | :------------------------------------------------------------------------------ |
| **Quản trị viên (Admin)**  | `admin@gmail.com`    | `password123` | Toàn quyền quản trị hệ thống, danh mục, xuất dữ liệu toàn viện                  |
| **Giảng viên 1 (Teacher)** | `teacher1@gmail.com` | `password123` | Phụ trách lớp Java (Mã GV: `GV001`), quản lý điểm & điểm danh lớp phụ trách     |
| **Giảng viên 2 (Teacher)** | `teacher2@gmail.com` | `password123` | Phụ trách lớp .NET (Mã GV: `GV002`), quản lý điểm & điểm danh lớp phụ trách     |
| **Giảng viên 3 (Teacher)** | `teacher3@gmail.com` | `password123` | Phụ trách lớp Frontend (Mã GV: `GV003`), quản lý điểm & điểm danh lớp phụ trách |
| **Học viên 1 (Student)**   | `student1@gmail.com` | `password123` | Học viên lớp Java, tra cứu điểm danh, bảng điểm, làm bài phỏng vấn              |
| **Học viên 2 (Student)**   | `student2@gmail.com` | `password123` | Học viên lớp .NET, tra cứu điểm danh, bảng điểm, làm bài phỏng vấn              |

---

## 📋 Quy chuẩn Thiết kế & Phát triển (AI Guidelines)

Mọi đóng góp mã nguồn hoặc phát triển tính năng mới bắt buộc phải tuân thủ nghiêm ngặt tài liệu chỉ thị trung tâm:  
📄 **[`AGENTS.md`](./AGENTS.md)**

- **Quy chuẩn UI/UX Đơn sắc (Monochrome Design System)**: Tông màu chủ đạo là Đen / Trắng / Xám Slate tối giản, không sử dụng các màu pastel sặc sỡ cho badge hoặc bảng điểm.
- **Tiền tố Route API chuẩn**: Bắt buộc dùng tiền tố `/api/...` (ví dụ: `/api/classes`, `/api/attendances`). Tuyệt đối không dùng `/api/v1/...`.
- **Bảo vệ mã nguồn**: Luôn chạy kiểm tra `dotnet build` và `npm run build` trước khi xác nhận hoàn thành công việc.

---

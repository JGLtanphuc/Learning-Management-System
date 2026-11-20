import * as XLSX from "xlsx";
import { downloadBlob } from "./dataio.utils";
import type { EntitySchema, FieldSchema } from "@/types";

interface TemplateDefinition {
  filename: string;
  sheetName: string;
  headers: string[];
  rows: Record<string, any>[];
}

const PREDEFINED_TEMPLATES: Record<string, TemplateDefinition> = {
  student: {
    filename: "student_template.xlsx",
    sheetName: "Students",
    headers: [
      "studentCode",
      "fullName",
      "email",
      "phone",
      "className",
      "university",
      "major",
      "gpa",
      "status",
    ],
    rows: [
      {
        studentCode: "SE160101",
        fullName: "Nguyen Van An",
        email: "an.nv@fams.edu.vn",
        phone: "0901234567",
        className: "HN26_FR_JAVA_01",
        university: "Bach Khoa University",
        major: "Software Engineering",
        gpa: 8.5,
        status: "ACTIVE",
      },
      {
        studentCode: "SE160102",
        fullName: "Tran Thi Binh",
        email: "binh.tt@fams.edu.vn",
        phone: "0912345678",
        className: "HN26_FR_JAVA_01",
        university: "FPT University",
        major: "Information Assurance",
        gpa: 8.2,
        status: "ACTIVE",
      },
    ],
  },
  class: {
    filename: "class_template.xlsx",
    sheetName: "Classes",
    headers: [
      "classCode",
      "className",
      "programName",
      "startDate",
      "endDate",
      "capacity",
      "location",
      "room",
      "status",
    ],
    rows: [
      {
        classCode: "HN26_FR_JAVA_01",
        className: "Fresher Java Spring Boot 01",
        programName: "Java Backend Professional",
        startDate: "2026-06-01",
        endDate: "2026-09-30",
        capacity: 25,
        location: "Hanoi Campus",
        room: "Lab 301",
        status: "ONGOING",
      },
      {
        classCode: "HCM26_FR_DOTNET_02",
        className: "Fresher .NET Fullstack 02",
        programName: ".NET Enterprise Development",
        startDate: "2026-07-01",
        endDate: "2026-10-31",
        capacity: 20,
        location: "HCMC F-Town",
        room: "Room 402",
        status: "PLANNING",
      },
    ],
  },
  attendance: {
    filename: "attendance_template.xlsx",
    sheetName: "Attendances",
    headers: [
      "studentCode",
      "studentName",
      "className",
      "attendanceDate",
      "slot",
      "status",
      "note",
    ],
    rows: [
      {
        studentCode: "SE160101",
        studentName: "Nguyen Van An",
        className: "HN26_FR_JAVA_01",
        attendanceDate: "2026-09-01",
        slot: "MORNING",
        status: "PRESENT",
        note: "Có mặt đúng giờ",
      },
      {
        studentCode: "SE160102",
        studentName: "Tran Thi Binh",
        className: "HN26_FR_JAVA_01",
        attendanceDate: "2026-09-01",
        slot: "AFTERNOON",
        status: "EXCUSED",
        note: "Nghỉ có phép",
      },
    ],
  },
  mock_project: {
    filename: "mock_project_template.xlsx",
    sheetName: "MockProjects",
    headers: [
      "projectName",
      "studentCodes",
      "mentorName",
      "techStack",
      "repoUrl",
      "status",
      "finalScore",
    ],
    rows: [
      {
        projectName: "FAS Learning Hub Portal",
        studentCodes: "SE160101, SE160102",
        mentorName: "Nguyen Huu Dat",
        techStack: "Java 17, Spring Boot, React, PostgreSQL",
        repoUrl: "https://github.com/fams/fas-learning-hub",
        status: "ACTIVE",
        finalScore: 8.8,
      },
      {
        projectName: "Enterprise Asset Management",
        studentCodes: "",
        mentorName: "Le Quoc Tuan",
        techStack: "C# .NET 8, EF Core, Next.js, SQL Server",
        repoUrl: "https://github.com/fams/asset-mgmt",
        status: "PLANNING",
        finalScore: "",
      },
      {
        projectName: "Auto Logistics & Delivery App",
        studentCodes: "SE160103",
        mentorName: "Tran Van Hai",
        techStack: "TypeScript, React 19, Tailwind, Vite",
        repoUrl: "https://github.com/fams/logistics-portal",
        status: "PLANNING",
        finalScore: "",
      },
    ],
  },
  allocation: {
    filename: "allocation_template.xlsx",
    sheetName: "Allocations",
    headers: [
      "studentCode",
      "studentName",
      "targetUnit",
      "projectName",
      "roleTitle",
      "matchScore",
      "status",
      "interviewDate",
      "note",
    ],
    rows: [
      {
        studentCode: "SE160101",
        studentName: "Nguyen Van An",
        targetUnit: "FSU1.BU9",
        projectName: "Banking Core Modernization",
        roleTitle: "Backend Java Developer",
        matchScore: 92.5,
        status: "ALLOCATED",
        interviewDate: "2026-09-05",
        note: "Đã vượt qua phỏng vấn",
      },
      {
        studentCode: "SE160102",
        studentName: "Tran Thi Binh",
        targetUnit: "DXL.BU2",
        projectName: "Smart Factory IoT Platform",
        roleTitle: "Fullstack Developer",
        matchScore: 88.0,
        status: "PROPOSED",
        interviewDate: "2026-09-10",
        note: "",
      },
    ],
  },
  permission: {
    filename: "permission_template.xlsx",
    sheetName: "Permissions",
    headers: ["name", "resource", "action", "description"],
    rows: [
      {
        name: "CLASS_CREATE",
        resource: "CLASS",
        action: "CREATE",
        description: "Tạo mới lớp học",
      },
      {
        name: "CLASS_READ",
        resource: "CLASS",
        action: "READ",
        description: "Xem danh sách lớp học",
      },
    ],
  },
  role: {
    filename: "role_template.xlsx",
    sheetName: "Roles",
    headers: ["name", "description"],
    rows: [
      {
        name: "TRAINER",
        description: "Giảng viên đào tạo Fresher",
      },
      {
        name: "MENTOR",
        description: "Mentor hướng dẫn Mock Project",
      },
    ],
  },
  user: {
    filename: "user_template.xlsx",
    sheetName: "Users",
    headers: ["email", "firstName", "lastName", "role"],
    rows: [
      {
        email: "instructor@example.com",
        firstName: "Van An",
        lastName: "Nguyen",
        role: "TEACHER",
      },
      {
        email: "mentor@example.com",
        firstName: "Quoc Tuan",
        lastName: "Le",
        role: "TEACHER",
      },
    ],
  },
  module: {
    filename: "module_template.xlsx",
    sheetName: "Modules",
    headers: ["title", "url", "icon", "requiredPermission", "displayOrder"],
    rows: [
      {
        title: "Thi & Đánh giá",
        url: "/exams",
        icon: "clipboard-check",
        requiredPermission: "CLASS_READ",
        displayOrder: 5,
      },
      {
        title: "Báo cáo thống kê",
        url: "/reports",
        icon: "bar-chart",
        requiredPermission: "USER_READ",
        displayOrder: 6,
      },
    ],
  },
  module_group: {
    filename: "module_group_template.xlsx",
    sheetName: "ModuleGroups",
    headers: ["name", "description", "displayOrder"],
    rows: [
      {
        name: "Đào tạo Fresher",
        description: "Quản lý lớp học, học viên và điểm danh",
        displayOrder: 2,
      },
      {
        name: "Quản lý dự án",
        description: "Quản lý Mock Project và bàn giao BU",
        displayOrder: 3,
      },
    ],
  },
  book: {
    filename: "book_template.xlsx",
    sheetName: "Books",
    headers: ["name", "description"],
    rows: [
      {
        name: "Clean Code: A Handbook of Agile Software Craftsmanship",
        description: "Tác giả Robert C. Martin",
      },
      {
        name: "Design Patterns: Elements of Reusable Object-Oriented Software",
        description: "Tác giả Gang of Four",
      },
    ],
  },
  student_score: {
    filename: "student_score_template.xlsx",
    sheetName: "StudentScores",
    headers: [
      "studentCode",
      "studentName",
      "className",
      "attendance",
      "quiz1",
      "quiz2",
      "assignment",
      "practice",
      "midterm",
      "final",
      "project",
      "bonus",
      "overallScore",
    ],
    rows: [
      {
        studentCode: "STU001",
        studentName: "Nguyen Van An",
        className: "SE-01",
        attendance: 10.0,
        quiz1: 8.5,
        quiz2: 9.0,
        assignment: 8.0,
        practice: 8.5,
        midterm: 8.0,
        final: 9.0,
        project: 8.5,
        bonus: 0.5,
        overallScore: 8.75,
      },
      {
        studentCode: "STU002",
        studentName: "Tran Thi Binh",
        className: "SE-01",
        attendance: 9.5,
        quiz1: 8.0,
        quiz2: 8.5,
        assignment: 9.0,
        practice: 8.0,
        midterm: 7.5,
        final: 8.5,
        project: 9.0,
        bonus: 0.0,
        overallScore: 8.35,
      },
    ],
  },
};

/**
 * Tạo và tải xuống file Excel mẫu (.xlsx) đầy đủ chuẩn cho bất kỳ màn hình / thực thể nào.
 */
export function generateAndDownloadExcelTemplate(
  entityName: string,
  schema?: EntitySchema
): void {
  const normalizedKey = (entityName || "data").toLowerCase().replace(/[-\s]+/g, "_");

  // 1. Kiểm tra template mẫu có sẵn được tinh chỉnh tối ưu
  let template = PREDEFINED_TEMPLATES[normalizedKey];

  if (!template) {
    // Thử tìm theo từ khóa rút gọn (e.g. "classes" -> "class", "menus" -> "module")
    if (normalizedKey.startsWith("class")) template = PREDEFINED_TEMPLATES.class;
    else if (normalizedKey.startsWith("student_score")) template = PREDEFINED_TEMPLATES.student_score;
    else if (normalizedKey.startsWith("student")) template = PREDEFINED_TEMPLATES.student;
    else if (normalizedKey.startsWith("menu_group")) template = PREDEFINED_TEMPLATES.module_group;
    else if (normalizedKey.startsWith("menu")) template = PREDEFINED_TEMPLATES.module;
    else if (normalizedKey.startsWith("mock")) template = PREDEFINED_TEMPLATES.mock_project;
  }

  // 2. Nếu chưa có template mẫu, tự động tạo từ schema fields
  if (!template) {
    const fields = (schema?.fields || []).filter((f: FieldSchema) => {
      if (f.editable === false) return false;
      if (["id", "createdAt", "updatedAt", "createdBy", "updatedBy"].includes(f.name)) return false;
      return true;
    });

    const headers = fields.length > 0 ? fields.map((f) => f.name) : ["name", "description", "status"];
    const row1: Record<string, any> = {};
    const row2: Record<string, any> = {};

    headers.forEach((h) => {
      const field = fields.find((f) => f.name === h);
      if (field?.type === "number") {
        row1[h] = 100;
        row2[h] = 200;
      } else if (field?.type === "date") {
        row1[h] = "2026-09-01";
        row2[h] = "2026-09-15";
      } else if (field?.type === "boolean") {
        row1[h] = true;
        row2[h] = false;
      } else if (field?.options && field.options.length > 0) {
        row1[h] = field.options[0]?.value ?? "VALUE_1";
        row2[h] = field.options[1]?.value ?? field.options[0]?.value ?? "VALUE_2";
      } else if (field?.filterOptions && field.filterOptions.length > 0) {
        row1[h] = field.filterOptions[0]?.value ?? "VALUE_1";
        row2[h] = field.filterOptions[1]?.value ?? field.filterOptions[0]?.value ?? "VALUE_2";
      } else {
        row1[h] = `Sample ${field?.label || h} 1`;
        row2[h] = `Sample ${field?.label || h} 2`;
      }
    });

    template = {
      filename: `${normalizedKey}_template.xlsx`,
      sheetName: normalizedKey.charAt(0).toUpperCase() + normalizedKey.slice(1, 25),
      headers,
      rows: [row1, row2],
    };
  }

  // 3. Sử dụng SheetJS (XLSX) để đóng gói workbook
  const wb = XLSX.utils.book_new();
  const ws = XLSX.utils.json_to_sheet(template.rows, { header: template.headers });

  // Tự động căn chỉnh độ rộng cột
  ws["!cols"] = template.headers.map((h) => {
    const maxValLen = template.rows.reduce(
      (max, r) => Math.max(max, String(r[h] ?? "").length),
      h.length
    );
    return { wch: Math.max(maxValLen + 5, 14) };
  });

  XLSX.utils.book_append_sheet(wb, ws, template.sheetName || "Template");

  // Xuất ra file Excel (.xlsx)
  const excelBuffer = XLSX.write(wb, { bookType: "xlsx", type: "array" });
  const blob = new Blob([excelBuffer], {
    type: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
  });

  downloadBlob(blob, template.filename);
}


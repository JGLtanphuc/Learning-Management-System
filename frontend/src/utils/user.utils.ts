/**
 * USER UTILITIES (Hàm tiện ích xử lý thông tin người dùng và lời chào)
 */

export interface UserLike {
  firstName?: string | null;
  lastName?: string | null;
  email?: string | null;
}

/**
 * Làm sạch chuỗi tên người dùng, khắc phục các lỗi mã hóa ký tự tiếng Việt
 */
export function sanitizeVietnameseName(name?: string | null): string {
  if (!name) return "";
  return name
    .replace(/Gi[áa]º£ng\s*vi[Ãa]ªn/gi, "Giảng viên")
    .replace(/H[áa]»[\x80-\x9f\w]*vi[Ãa]ªn/gi, "Học viên")
    .replace(/ho[áa]º·c/gi, "hoặc")
    .trim();
}

/**
 * Định dạng họ và tên người dùng theo văn phong tiếng Việt (Họ trước, Tên sau)
 */
export function formatUserDisplayName(user?: UserLike | null, defaultFallback = "Người dùng"): string {
  if (!user) return defaultFallback;

  let rawName = "";
  if (user.lastName && user.firstName) {
    rawName = `${user.lastName} ${user.firstName}`.trim();
  } else if (user.firstName || user.lastName) {
    rawName = (user.firstName || user.lastName || "").trim();
  } else if (user.email) {
    rawName = user.email.trim();
  } else {
    rawName = defaultFallback;
  }

  return sanitizeVietnameseName(rawName) || defaultFallback;
}

/**
 * Tạo câu chào thông minh theo vai trò (Giảng viên / Học viên)
 */
export function formatGreetingTitle(
  displayName: string,
  roleType: "TEACHER" | "STUDENT"
): string {
  if (!displayName) {
    return roleType === "TEACHER" ? "Xin chào Giảng viên! 👋" : "Xin chào Học viên! 👋";
  }

  const lower = displayName.toLowerCase();

  if (roleType === "TEACHER") {
    if (
      lower.startsWith("giảng viên") ||
      lower.startsWith("giáo viên") ||
      lower.startsWith("thầy") ||
      lower.startsWith("cô") ||
      lower.startsWith("teacher") ||
      lower.startsWith("trainer")
    ) {
      return `Xin chào ${displayName}! 👋`;
    }
    return `Xin chào Giảng viên ${displayName}! 👋`;
  }

  // STUDENT
  if (
    lower.startsWith("học viên") ||
    lower.startsWith("sinh viên") ||
    lower.startsWith("student")
  ) {
    return `Xin chào ${displayName}! 👋`;
  }
  return `Xin chào Học viên ${displayName}! 👋`;
}

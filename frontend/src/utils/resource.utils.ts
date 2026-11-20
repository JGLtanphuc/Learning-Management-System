/**
 * Hàm chuẩn hóa và bóc tách Resource Code từ URL và Title trên Frontend
 * Giúp Frontend tự động nhận diện và sinh các quyền tương ứng trên giao diện
 */
export function removeDiacritics(str: string): string {
  return str
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .replace(/đ/g, "d")
    .replace(/Đ/g, "D");
}

export function extractResourceCode(
  url?: string,
  title?: string,
  requiredPermission?: string,
): string {
  if (requiredPermission && requiredPermission.includes("_")) {
    const parts = requiredPermission.split("_");
    parts.pop(); // loại bỏ action như READ, CREATE, UPDATE, DELETE
    return parts.join("_").toUpperCase();
  }

  if (url) {
    const clean = url.trim().replace(/^\/+|\/+$/g, "");
    if (clean === "dashboard") return "DASHBOARD";
    if (clean === "books") return "BOOK";
    if (clean === "student-scores" || clean.startsWith("topic-marks")) return "STUDENT_SCORE";
    if (clean === "menus" || clean === "menu-groups") return "MENU";
    if (clean === "users") return "USER";
    if (clean === "roles") return "ROLE";
    if (clean === "permissions") return "PERMISSION";
    if (clean === "classes") return "CLASS";
    if (clean === "students") return "STUDENT";
    if (clean === "attendances") return "ATTENDANCE";
    if (clean === "mock-projects") return "MOCK_PROJECT";
    if (clean === "allocations") return "ALLOCATION";

    const segments = clean.split("/");
    if (segments.length > 0 && segments[0]) {
      let code = segments[0].replace(/[^a-zA-Z0-9]+/g, "_").toUpperCase();
      if (code.endsWith("ES") && code.length > 4) {
        code = code.substring(0, code.length - 2);
      } else if (code.endsWith("S") && code.length > 3 && !code.endsWith("SS")) {
        code = code.substring(0, code.length - 1);
      }
      return code;
    }
  }

  if (title) {
    const noDiacritics = removeDiacritics(title.trim());
    return noDiacritics
      .replace(/[^a-zA-Z0-9]+/g, "_")
      .replace(/^_+|_+$/g, "")
      .toUpperCase();
  }

  return "UNKNOWN";
}

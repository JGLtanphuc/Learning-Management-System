export function downloadBlob(blob: Blob, filename?: string) {
  const url = window.URL.createObjectURL(blob);

  const link = document.createElement("a");
  link.href = url;

  link.download = filename || "download";

  document.body.appendChild(link);
  link.click();

  link.remove();
  window.URL.revokeObjectURL(url);
}

export function getFilenameFromHeader(
  contentDisposition?: string | null
): string | null {
  if (!contentDisposition) return null;

  // 1. Try filename*=UTF-8''... (RFC 5987 / RFC 6266)
  const utf8Match = contentDisposition.match(/filename\*\s*=\s*(?:UTF-8''|utf-8'')([^;\r\n]+)/i);
  if (utf8Match && utf8Match[1]) {
    try {
      const decoded = decodeURIComponent(utf8Match[1].trim().replace(/^["']|["']$/g, ""));
      if (decoded && !decoded.includes(";")) return decoded;
    } catch {
      // ignore decoding error and fallback
    }
  }

  // 2. Try standard filename="..." or filename=...
  const match = contentDisposition.match(/filename\s*=\s*(?:"([^"]+)"|([^;\r\n]+))/i);
  if (match) {
    const raw = (match[1] || match[2] || "").trim();
    const clean = raw.replace(/^["']|["']$/g, "").split(";")[0].trim();
    if (clean) return clean;
  }

  return null;
}
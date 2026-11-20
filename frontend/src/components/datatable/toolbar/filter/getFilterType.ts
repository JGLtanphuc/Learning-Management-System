import type { FieldSchema, FilterType } from "@/types";

/**
 * Xác định loại filter phù hợp cho một field dựa trên `filterType` hoặc `type`.
 */
export function getFilterType(field: FieldSchema): FilterType {
  if (field.filterType) return field.filterType;

  // Tự động nhận diện dropdown nếu trường có cấu hình options hoặc filterOptions
  if (
    (field.filterOptions && field.filterOptions.length > 0) ||
    (field.options && field.options.length > 0)
  ) {
    return "select";
  }

  switch (field.type) {
    case "boolean":
      return "boolean";
    case "icon":
    case "select":
    case "relation":
      return "select";
    case "date":
      return "dateRange";
    case "number":
      return "numberRange";
    case "textarea":
      return "text";
    default:
      return "text";
  }
}

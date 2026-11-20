import type { FileFormat, ImportResult, Pagination } from "@/types";
import type { PageResponse } from "@/types/common/pageable";
import type { AxiosInstance } from "axios";
import axiosInstance from "./axios";

/**
 * Interface định nghĩa các hàm CRUD chuẩn và mở rộng cho mọi thực thể (Entity)
 * DTO: Kiểu dữ liệu đối tượng gửi nhận với Backend (vd: StudentDTO, TeacherDTO)
 * Filter: Kiểu dữ liệu của bộ lọc tìm kiếm (vd: StudentFilter)
 */
export interface BaseApiService<DTO, Filter> {
  path?: string;
  /** Nhập dữ liệu hàng loạt từ file (Excel, CSV) */
  import(file: File): Promise<ImportResult>;

  /** Xuất file báo cáo (Excel, PDF...) trả về Blob nhị phân */
  export(format: FileFormat): Promise<Blob>;

  /** Lấy danh sách có phân trang, sắp xếp và lọc nâng cao */
  getPage(
    pagination: Pagination,
    search?: string,
    filter?: Filter,
  ): Promise<PageResponse<DTO>>;

  /** Lấy chi tiết 1 bản ghi theo ID */
  getById(id: string): Promise<DTO>;

  /** Tạo mới 1 bản ghi */
  create(data: DTO): Promise<DTO>;

  /** Cập nhật 1 bản ghi theo ID */
  update(id: string, data: DTO): Promise<DTO>;

  /** Xóa 1 bản ghi theo ID */
  delete(id: string): Promise<void>;
}

/** Cấu hình đầu vào để tạo API Service */
interface BaseApiConfig {
  path: string; // Đường dẫn URL gốc (ví dụ: '/students', '/classes')
  instance?: AxiosInstance; // Custom axios instance nếu cần (mặc định dùng axiosInstance chung)
}

/**
 * Factory Function: Hàm sinh tự động trọn bộ API CRUD chuẩn RESTful.
 * Giúp tránh lặp code (DRY): Chỉ cần truyền 'path' là có ngay 7 hàm gọi API chuẩn.
 */
export const createBaseApiService = <DTO = any, Filter = any>({
  path,
  instance = axiosInstance,
}: BaseApiConfig): BaseApiService<DTO, Filter> => {
  return {
    path,
    // 1. IMPORT: Tải file lên server dưới dạng multipart/form-data
    import: async (file: File): Promise<ImportResult> => {
      const formData = new FormData();
      formData.append("file", file); // Đóng gói file vào FormData
      const response = await instance.post<ImportResult>(
        `${path}/import`,
        formData,
        {
          headers: {
            "Content-Type": "multipart/form-data", // Header báo server đây là file upload
          },
        },
      );
      return response.data;
    },

    // 2. EXPORT: Tải file dữ liệu về từ server
    export: async (format: FileFormat) => {
      return instance.get(`${path}/export`, {
        params: { format }, // Truyền format (xlsx, pdf...) lên query
        responseType: "blob", // Nhận dữ liệu dưới dạng nhị phân Blob để tải về máy
      });
    },

    // 3. GET PAGE: Lấy danh sách phân trang (kèm tìm kiếm và bộ lọc)
    getPage: async (
      pagination: Pagination,
      search?: string,
      filter?: Filter,
    ): Promise<PageResponse<DTO>> => {
      const { sort, ...rest } = pagination;
      const params = new URLSearchParams(); // Đối tượng hỗ trợ build query string (?page=1&pageSize=10...)

      // Duyệt và thêm các tham số phân trang cơ bản vào query string
      Object.entries(rest).forEach(([key, value]) => {
        if (value !== undefined && value !== null)
          params.append(key, String(value));
      });

      // Xử lý tham số sắp xếp (hỗ trợ cả 1 cột hoặc nhiều cột sắp xếp)
      if (sort) {
        const sortArr = Array.isArray(sort) ? sort : [sort];
        sortArr.forEach((s) => params.append("sort", s));
      }

      // Xử lý từ khóa tìm kiếm chung nếu có
      if (search) params.append("search", search);

      // Hàm chuyển đổi giá trị filter sang chuỗi (đặc biệt là kiểu Date -> ISO string)
      const serializeValue = (value: any) => {
        if (value instanceof Date) return value.toISOString();
        return String(value);
      };

      // Duyệt qua từng điều kiện lọc nâng cao và nhồi vào Query Params
      if (filter) {
        Object.entries(filter as Record<string, any>).forEach(
          ([key, value]) => {
            // Bỏ qua giá trị rỗng hoặc null/undefined
            if (value === undefined || value === null) return;
            if (typeof value === "string" && !value.trim()) return;

            // Nếu filter là mảng (vd chọn nhiều trạng thái) -> append từng phần tử
            if (Array.isArray(value)) {
              value.forEach((v) => params.append(key, serializeValue(v)));
            } else {
              params.append(key, serializeValue(value));
            }
          },
        );
      }

      // Gửi request GET kèm query parameters đã đóng gói hoàn chỉnh
      const response = await instance.get<PageResponse<DTO>>(path, {
        params,
      });
      return response.data;
    },

    // 4. GET BY ID: Lấy chi tiết 1 bản ghi theo ID -> GET /path/{id}
    getById: async (id: string): Promise<DTO> => {
      console.log(`Fetching ${path} with ID:`, id);
      const response = await instance.get<DTO>(`${path}/${id}`);
      console.log("GetById response:", response);
      return response.data;
    },

    // 5. CREATE: Tạo mới 1 bản ghi -> POST /path kèm body JSON
    create: async (data: DTO): Promise<DTO> => {
      console.log("Creating new entry at", path, "with data:", data);
      const response = await instance.post<DTO>(path, data);
      console.log("Create response:", response);
      return response.data;
    },

    // 6. UPDATE: Cập nhật 1 bản ghi theo ID -> PUT /path/{id} kèm body JSON
    update: async (id: string, data: DTO): Promise<DTO> => {
      console.log(`Updating ${path} with ID:`, id, "and data:", data);
      const response = await instance.put<DTO>(`${path}/${id}`, data);
      console.log("Update response:", response);
      return response.data;
    },

    // 7. DELETE: Xóa 1 bản ghi theo ID -> DELETE /path/{id}
    delete: async (id: string): Promise<void> => {
      console.log(`Deleting ${path} with ID:`, id);
      const response = await instance.delete(`${path}/${id}`);
      console.log("Delete response:", response);
      return response.data;
    },
  };
};

import { downloadBlob, getFilenameFromHeader } from "@/components/datatable/util/dataio.utils";
import { generateAndDownloadExcelTemplate } from "@/components/datatable/util/template-generator";
import type { EntitySchema } from "@/types";
import axiosInstance from "../../axios";

export const downloadTemplate = async (entity: string, schema?: EntitySchema) => {
    try {
        // Ưu tiên tạo file Excel (.xlsx) chuẩn hóa giao diện với định dạng bảng và dữ liệu mẫu đầy đủ
        generateAndDownloadExcelTemplate(entity, schema);
    } catch (clientError) {
        console.warn("Client template generator encountered error, trying backend API...", clientError);
        // Fallback sang API backend nếu có
        const res = await axiosInstance.get(`/import/template?entity=${entity}`, {
            responseType: "blob",
        });

        const contentDisposition = res.headers["content-disposition"];
        const filename = getFilenameFromHeader(contentDisposition);

        const contentType =
            res.headers["content-type"] ||
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        const blob = new Blob([res.data], {
            type: contentType,
        });

        downloadBlob(blob, filename || `${entity}_template.xlsx`);
    }
};

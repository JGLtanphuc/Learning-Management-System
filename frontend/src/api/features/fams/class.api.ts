import { createBaseApiService } from "@/api/base-service.api";
import type { ClassDTO, ClassFilter } from "@/types";

const path = "/classes";

const base = createBaseApiService<ClassDTO, ClassFilter>({
  path: path,
});

export const classApi = base;

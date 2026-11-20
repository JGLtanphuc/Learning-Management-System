import { createBaseApiService } from "@/api/base-service.api";
import type { MockProjectDTO, MockProjectFilter } from "@/types";

const path = "/mock-projects";

const base = createBaseApiService<MockProjectDTO, MockProjectFilter>({
  path: path,
});

export const mockProjectApi = base;


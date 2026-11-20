import { createBaseApiService } from "@/api/base-service.api";
import type { AllocationDTO, AllocationFilter } from "@/types";

const path = "/allocations";

const base = createBaseApiService<AllocationDTO, AllocationFilter>({
  path: path,
});

export const allocationApi = base;


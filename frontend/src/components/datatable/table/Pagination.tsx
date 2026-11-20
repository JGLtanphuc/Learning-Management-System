import { Button } from "@/components/ui/button";
import {
  Pagination as PaginationComponent,
  PaginationContent,
  PaginationEllipsis,
  PaginationItem,
} from "@/components/ui/pagination";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { usePagination } from "@/hooks/usePagination";
import { ChevronLeft, ChevronRight } from "lucide-react";
import ActionButton from "../common/ActionButton";

const PAGE_SIZE_OPTIONS = [10, 20, 30, 50, 100];

interface PaginationProps {
  table: any;
  isAutoSize?: boolean;
  autoSize?: number;
  onPageSizeChange?: (value: string) => void;
  showSelectionSummary?: boolean;
}

export function Pagination({
  table,
  isAutoSize = true,
  autoSize,
  onPageSizeChange,
  showSelectionSummary = true,
}: PaginationProps) {
  const totalPages = Math.ceil(table.total / table.size) || 1;
  const currentPage = table.page + 1;

  const { pages, showLeftEllipsis, showRightEllipsis } = usePagination({
    currentPage,
    totalPages,
    paginationItemsToDisplay: 5,
  });

  const canPreviousPage = table.page > 0;
  const canNextPage = table.page < totalPages - 1;

  const currentSelectValue = isAutoSize ? "auto" : String(table.size);

  return (
    <div className="flex flex-col gap-2.5 sm:gap-3 md:flex-row md:items-center md:justify-between py-1">
      <div className="flex items-center justify-between sm:justify-start gap-4 text-xs sm:text-sm text-muted-foreground">
        {showSelectionSummary && table.selected?.length >= 0 && (
          <div>
            <span className="font-medium text-foreground">
              {table.selected?.length || 0}
            </span>{" "}
            of{" "}
            <span className="font-medium text-foreground">
              {table.total || table.data?.length || 0}
            </span>{" "}
            rows selected
          </div>
        )}
      </div>

      <div className="flex flex-wrap items-center justify-between sm:justify-end gap-2 sm:gap-4 w-full md:w-auto">
        <div className="flex items-center gap-1.5 sm:gap-2 text-xs sm:text-sm text-muted-foreground">
          <span className="whitespace-nowrap hidden sm:inline">Rows per page</span>
          <span className="whitespace-nowrap sm:hidden">Rows:</span>
          <Select value={currentSelectValue} onValueChange={onPageSizeChange}>
            <SelectTrigger className="h-8 w-[74px] sm:w-[80px] text-xs">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="auto">
                Auto{autoSize ? ` (${autoSize})` : ""}
              </SelectItem>
              {PAGE_SIZE_OPTIONS.map((size) => (
                <SelectItem key={size} value={String(size)}>
                  {size}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        <div
          className="text-xs sm:text-sm text-muted-foreground whitespace-nowrap"
          aria-live="polite"
        >
          Page{" "}
          <span className="font-medium text-foreground">{currentPage}</span> /{" "}
          <span className="font-medium text-foreground">{totalPages}</span>
        </div>

        <PaginationComponent className="w-auto mx-0">
          <PaginationContent className="gap-1">
            <PaginationItem>
              <ActionButton
                onClick={() => table.setPage(table.page - 1)}
                disabled={!canPreviousPage}
                icon={<ChevronLeft size={16} />}
                variant="secondary"
                tooltip="Previous page"
              />
            </PaginationItem>

            {showLeftEllipsis && <PaginationEllipsis className="hidden sm:flex" />}

            {pages.map((p) => {
              const isCurrent = p === currentPage;
              return (
                <PaginationItem key={p} className={!isCurrent ? "hidden sm:inline-block" : ""}>
                  <Button
                    size="icon"
                    className="h-8 w-8 text-xs sm:h-9 sm:w-9 sm:text-sm"
                    variant={isCurrent ? "default" : "outline"}
                    onClick={() => table.setPage(p - 1)}
                  >
                    {p}
                  </Button>
                </PaginationItem>
              );
            })}

            {showRightEllipsis && <PaginationEllipsis className="hidden sm:flex" />}

            <PaginationItem>
              <ActionButton
                onClick={() => table.setPage(table.page + 1)}
                disabled={!canNextPage}
                icon={<ChevronRight size={16} />}
                variant="secondary"
                tooltip="Next page"
              />
            </PaginationItem>
          </PaginationContent>
        </PaginationComponent>
      </div>
    </div>
  );
}

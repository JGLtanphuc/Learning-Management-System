import { ColumnToggle } from "./ColumnToggle";
import { FilterPopover } from "./filter/FilterPopover";
import { SearchInput } from "./SearchInput";
import { ToolbarActions } from "./ToolbarActions";
import ActionButton from "../common/ActionButton";
import { ArrowUpDown, LayoutGrid, Table2 } from "lucide-react";

export type ViewMode = "table" | "card";

interface ToolbarProps {
  table: any;
  headerActions?: React.ReactNode;
  viewMode?: ViewMode;
  onViewModeChange?: (mode: ViewMode) => void;
  showViewModeToggle?: boolean;
  showActions?: boolean;
  showCardColumnToggle?: boolean;
}

export function Toolbar({
  table,
  headerActions,
  viewMode = "table",
  onViewModeChange,
  showViewModeToggle = true,
  showActions = true,
  showCardColumnToggle = true,
}: ToolbarProps) {
  const { schema } = table;

  const hasFilters = schema.fields.some((f: any) => f.filterable);
  const activeFilterCount = Object.keys(table.filters || {}).filter(
    (key) => table.filters[key] !== undefined && table.filters[key] !== "",
  ).length;

  const hasSorts = table.sortState?.length > 0;

  return (
    <div
      className="flex flex-col lg:flex-row lg:items-center justify-between gap-2.5 w-full"
    >
      <div className="flex flex-wrap sm:flex-nowrap items-center gap-2 w-full lg:w-auto">
        <div className="w-full sm:w-64 md:w-72 lg:w-80 xl:w-96 shrink-0">
          <SearchInput search={table.search} onSearchChange={table.setSearch} />
        </div>

        <div className="flex items-center gap-1.5 shrink-0">
          <FilterPopover
            table={table}
            hasFilters={hasFilters}
            activeFilterCount={activeFilterCount}
          />

          {viewMode === "card" && showCardColumnToggle && (
            <ColumnToggle
              schema={schema}
              columnVisibility={table.columnVisibility}
              toggleFieldVisibility={table.toggleFieldVisibility}
            />
          )}

          {hasSorts && (
            <ActionButton
              onClick={() => table.clearSort()}
              tooltip={`Clear all sorts (${table.sortState.length})`}
              title={`Clear sort (${table.sortState.length})`}
              variant="outline"
              icon={<ArrowUpDown size={14} />}
            />
          )}

          {showViewModeToggle && (
            <ActionButton
              onClick={() => onViewModeChange?.(viewMode === "table" ? "card" : "table")}
              tooltip={viewMode === "table" ? "Switch to card view" : "Switch to table view"}
              variant="outline"
              icon={viewMode === "table" ? <LayoutGrid size={16} /> : <Table2 size={16} />}
            />
          )}
        </div>
      </div>

      {showActions && (
        <div className="w-full lg:w-auto flex justify-start sm:justify-end shrink-0">
          <ToolbarActions table={table} headerActions={headerActions} />
        </div>
      )}
    </div>
  );
}

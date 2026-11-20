import { useState, useMemo, useEffect, useRef } from "react";
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from "@/components/ui/popover";
import { Checkbox } from "@/components/ui/checkbox";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { ScrollArea } from "@/components/ui/scroll-area";
import { ChevronDown, Search, X } from "lucide-react";
import type { FieldSchema, RelationConfig } from "@/types";
import { FieldError } from "./FieldError";

const MAX_VISIBLE_BADGES = 3;

function normalizeString(str: string): string {
  return str
    .toLowerCase()
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .replace(/[đĐ]/g, "d")
    .trim();
}

interface MultiRelationSelectProps {
  field: FieldSchema;
  rel: RelationConfig;
  items: any[];
  value: any;
  errorClass: string;
  onChange: (val: any[]) => void;
  fieldErrors?: string[];
  formValues?: Record<string, any>;
  currentId?: any;
}

export function MultiRelationSelect({
  field,
  rel,
  items,
  value,
  errorClass,
  onChange,
  fieldErrors,
  formValues,
  currentId,
}: MultiRelationSelectProps) {
  const [search, setSearch] = useState("");
  const inputRef = useRef<HTMLInputElement>(null);
  const [fetchedOptions, setFetchedOptions] = useState<any[] | null>(null);
  const [isLoading, setIsLoading] = useState(false);

  const selectedValues: any[] = Array.isArray(value) ? value : [];

  useEffect(() => {
    if (rel.fetchOptions) {
      setIsLoading(true);
      rel
        .fetchOptions({ currentId, classId: formValues?.classId })
        .then((res) => {
          setFetchedOptions(Array.isArray(res) ? res : []);
        })
        .catch((err) => {
          console.error("Error fetching relation options:", err);
          setFetchedOptions([]);
        })
        .finally(() => {
          setIsLoading(false);
        });
    }
  }, [rel.fetchOptions, currentId, formValues?.classId]);

  const candidateItems = fetchedOptions !== null ? fetchedOptions : items;

  const allKnownItems = useMemo(() => {
    const map = new Map<string, any>();
    items.forEach((item) => {
      const key = item[rel.valueField]?.toString();
      if (key) map.set(key, item);
    });
    candidateItems.forEach((item) => {
      const key = item[rel.valueField]?.toString();
      if (key) map.set(key, item);
    });
    return Array.from(map.values());
  }, [items, candidateItems, rel.valueField]);

  const selectedItems = useMemo(
    () =>
      allKnownItems.filter((item) =>
        selectedValues.some(
          (v) => v?.toString() === item[rel.valueField]?.toString(),
        ),
      ),
    [allKnownItems, selectedValues, rel.valueField],
  );

  const filteredItems = useMemo(() => {
    if (!search.trim()) return candidateItems;
    const term = normalizeString(search);
    const rawTerm = search.trim().toLowerCase();

    return candidateItems.filter((item) => {
      const label = item[rel.labelField]?.toString() || "";
      const code = item.studentCode?.toString() || "";
      const className = item.className?.toString() || "";
      const email = item.email?.toString() || "";

      const rawCombined =
        `${label} ${code} ${className} ${email}`.toLowerCase();
      const normCombined = `${normalizeString(label)} ${normalizeString(code)} ${normalizeString(className)} ${normalizeString(email)}`;

      return rawCombined.includes(rawTerm) || normCombined.includes(term);
    });
  }, [candidateItems, search, rel.labelField]);

  const isMaxReached = rel.max ? selectedValues.length >= rel.max : false;

  const allFilteredSelected =
    filteredItems.length > 0 &&
    filteredItems.every((item) =>
      selectedValues.some(
        (v) => v?.toString() === item[rel.valueField]?.toString(),
      ),
    );

  const toggleItem = (itemValue: any) => {
    const strVal = itemValue?.toString();
    const exists = selectedValues.some((v) => v?.toString() === strVal);
    if (exists) {
      onChange(selectedValues.filter((v) => v?.toString() !== strVal));
    } else {
      if (rel.max && selectedValues.length >= rel.max) {
        return;
      }
      onChange([...selectedValues, itemValue]);
    }
  };

  const selectAllFiltered = () => {
    const newValues = [...selectedValues];
    for (const item of filteredItems) {
      if (rel.max && newValues.length >= rel.max) break;
      const val = item[rel.valueField];
      if (!newValues.some((v) => v?.toString() === val?.toString())) {
        newValues.push(val);
      }
    }
    onChange(newValues);
  };

  const deselectAllFiltered = () => {
    const filteredIds = new Set(
      filteredItems.map((i) => i[rel.valueField]?.toString()),
    );
    onChange(selectedValues.filter((v) => !filteredIds.has(v?.toString())));
  };

  const isRequired = Boolean(field.validation?.required ?? field.required);

  return (
    <div className="grid gap-2">
      <div className="flex items-center justify-between">
        <Label>
          {field.label}
          {isRequired && <span className="text-rose-500 ml-1 font-bold">*</span>}
        </Label>
        {rel.max && (
          <span className="text-xs text-muted-foreground">
            Tối đa {rel.max} ({selectedValues.length}/{rel.max})
          </span>
        )}
      </div>
      <Popover modal={true}>
        <PopoverTrigger asChild>
          <Button
            variant="outline"
            role="combobox"
            className={`w-full justify-between font-normal h-auto min-h-10 py-1.5 px-3 ${errorClass}`}
          >
            <div className="flex flex-wrap gap-1 items-center">
              {selectedItems.length > 0 ? (
                <>
                  {selectedItems.slice(0, MAX_VISIBLE_BADGES).map((item) => (
                    <Badge
                      key={item[rel.valueField]}
                      variant="secondary"
                      className="text-xs gap-1 pr-1"
                    >
                      {item[rel.labelField]}
                      {item.studentCode ? ` (${item.studentCode})` : ""}
                      <X
                        size={12}
                        className="cursor-pointer hover:text-destructive"
                        onClick={(e) => {
                          e.stopPropagation();
                          toggleItem(item[rel.valueField]);
                        }}
                      />
                    </Badge>
                  ))}
                  {selectedItems.length > MAX_VISIBLE_BADGES && (
                    <Badge variant="outline" className="text-xs">
                      +{selectedItems.length - MAX_VISIBLE_BADGES} more
                    </Badge>
                  )}
                </>
              ) : (
                <span className="text-muted-foreground">
                  {isLoading
                    ? "Đang tải dữ liệu..."
                    : `Select ${field.label.toLowerCase()}...`}
                </span>
              )}
            </div>
            <ChevronDown
              size={14}
              className="shrink-0 ml-2 text-muted-foreground"
            />
          </Button>
        </PopoverTrigger>
        <PopoverContent
          className="w-[--radix-popover-trigger-width] p-0"
          align="start"
          onOpenAutoFocus={(e) => {
            e.preventDefault();
            inputRef.current?.focus();
          }}
        >
          {/* Search */}
          <div className="p-2 border-b">
            <div className="relative">
              <Search
                size={14}
                className="absolute left-2.5 top-1/2 -translate-y-1/2 text-muted-foreground pointer-events-none"
              />
              <Input
                ref={inputRef}
                placeholder="Search name or code..."
                className="h-8 pl-8 pr-7 text-sm"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
              />
              {search && (
                <button
                  type="button"
                  aria-label="Clear search"
                  className="absolute right-2 top-1/2 -translate-y-1/2 p-0.5 text-muted-foreground hover:text-foreground rounded cursor-pointer"
                  onClick={() => {
                    setSearch("");
                    inputRef.current?.focus();
                  }}
                >
                  <X size={14} />
                </button>
              )}
            </div>
          </div>

          {/* Select all / Clear */}
          {filteredItems.length > 0 && (
            <div className="px-2 py-1.5 border-b flex items-center justify-between">
              <span className="text-xs text-muted-foreground">
                {selectedValues.length}
                {rel.max ? ` / ${rel.max}` : ` / ${candidateItems.length}`}{" "}
                selected
                {rel.max && isMaxReached ? " (Đã đạt tối đa)" : ""}
              </span>
              <div className="flex gap-1">
                {!allFilteredSelected && !isMaxReached && (
                  <Button
                    variant="ghost"
                    size="sm"
                    className="h-6 text-xs px-2"
                    onClick={selectAllFiltered}
                  >
                    Select all{search.trim() ? " filtered" : ""}
                  </Button>
                )}
                {selectedValues.length > 0 && (
                  <Button
                    variant="ghost"
                    size="sm"
                    className="h-6 text-xs px-2 text-destructive hover:text-destructive"
                    onClick={
                      search.trim() ? deselectAllFiltered : () => onChange([])
                    }
                  >
                    Clear{search.trim() ? " filtered" : " all"}
                  </Button>
                )}
              </div>
            </div>
          )}

          {/* Options */}
          <ScrollArea className="h-[220px]">
            <div className="p-1">
              {isLoading ? (
                <div className="px-2 py-4 text-sm text-center text-muted-foreground">
                  Đang tải danh sách...
                </div>
              ) : (
                filteredItems.map((item) => {
                  const itemVal = item[rel.valueField];
                  const checked = selectedValues.some(
                    (v) => v?.toString() === itemVal?.toString(),
                  );
                  const disabled = !checked && isMaxReached;

                  return (
                    <div
                      key={itemVal}
                      className={`flex items-center justify-between px-2 py-1.5 rounded-sm transition-colors ${
                        disabled
                          ? "opacity-50 cursor-not-allowed bg-muted/20"
                          : "hover:bg-accent cursor-pointer"
                      }`}
                      onClick={() => {
                        if (!disabled) toggleItem(itemVal);
                      }}
                    >
                      <div className="flex items-center gap-2 min-w-0">
                        <Checkbox checked={checked} disabled={disabled} />
                        <div className="flex flex-col min-w-0 text-left">
                          <span className="text-sm font-medium truncate">
                            {item[rel.labelField]}
                          </span>
                          {item.studentCode && (
                            <span className="text-xs text-muted-foreground truncate">
                              {item.studentCode}
                              {item.className ? ` • ${item.className}` : ""}
                            </span>
                          )}
                        </div>
                      </div>
                      {checked && (
                        <span className="text-xs text-primary font-medium shrink-0 ml-2">
                          ✓
                        </span>
                      )}
                    </div>
                  );
                })
              )}
              {!isLoading && filteredItems.length === 0 && (
                <div className="px-2 py-4 text-sm text-center text-muted-foreground">
                  {candidateItems.length === 0
                    ? "Không có sinh viên nào khả dụng"
                    : "Không tìm thấy kết quả"}
                </div>
              )}
            </div>
          </ScrollArea>
        </PopoverContent>
      </Popover>
      <FieldError errors={fieldErrors} />
    </div>
  );
}

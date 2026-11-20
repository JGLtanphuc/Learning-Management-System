import React, { useEffect, useState } from "react";
import { Label } from "@/components/ui/label";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { Check, Plus, Trash2 } from "lucide-react";
import { cn } from "@/lib/utils";
import type { InterviewQuestionOption } from "@/types/features/interview/interview.types";
import { FieldError } from "./FieldError";

interface McqOptionsEditorProps {
  optionsJson?: string;
  correctOptionId?: string;
  onChangeOptions: (json: string) => void;
  onChangeCorrectOption: (id: string) => void;
  fieldErrors?: string[];
}

const DEFAULT_OPTIONS: InterviewQuestionOption[] = [
  { id: "A", text: "" },
  { id: "B", text: "" },
  { id: "C", text: "" },
  { id: "D", text: "" },
];

const OPTION_LETTERS = ["A", "B", "C", "D", "E", "F", "G", "H"];

export const McqOptionsEditor: React.FC<McqOptionsEditorProps> = ({
  optionsJson,
  correctOptionId,
  onChangeOptions,
  onChangeCorrectOption,
  fieldErrors,
}) => {
  const [options, setOptions] = useState<InterviewQuestionOption[]>(() => {
    if (!optionsJson) return DEFAULT_OPTIONS;
    try {
      const parsed = JSON.parse(optionsJson);
      return Array.isArray(parsed) && parsed.length > 0 ? parsed : DEFAULT_OPTIONS;
    } catch {
      return DEFAULT_OPTIONS;
    }
  });

  const [selectedCorrect, setSelectedCorrect] = useState<string>(
    correctOptionId || "A"
  );

  // Sync from props if external optionsJson/correctOptionId changes
  useEffect(() => {
    if (optionsJson) {
      try {
        const parsed = JSON.parse(optionsJson);
        if (Array.isArray(parsed) && parsed.length > 0) {
          setOptions(parsed);
        }
      } catch {
        // Keep current
      }
    }
  }, [optionsJson]);

  useEffect(() => {
    if (correctOptionId) {
      setSelectedCorrect(correctOptionId);
    }
  }, [correctOptionId]);

  const emitChanges = (
    newOptions: InterviewQuestionOption[],
    newCorrectId: string
  ) => {
    setOptions(newOptions);
    setSelectedCorrect(newCorrectId);
    onChangeOptions(JSON.stringify(newOptions));
    onChangeCorrectOption(newCorrectId);
  };

  const handleTextChange = (index: number, text: string) => {
    const updated = options.map((opt, i) =>
      i === index ? { ...opt, text } : opt
    );
    setOptions(updated);
    onChangeOptions(JSON.stringify(updated));
  };

  const handleSelectCorrect = (id: string) => {
    setSelectedCorrect(id);
    onChangeCorrectOption(id);
  };

  const handleAddOption = () => {
    if (options.length >= 8) return;
    const usedIds = new Set(options.map((o) => o.id));
    const nextId =
      OPTION_LETTERS.find((letter) => !usedIds.has(letter)) ||
      String.fromCharCode(65 + options.length);

    const updated = [...options, { id: nextId, text: "" }];
    setOptions(updated);
    onChangeOptions(JSON.stringify(updated));
  };

  const handleRemoveOption = (indexToRemove: number) => {
    if (options.length <= 2) return;
    const removedOption = options[indexToRemove];
    const updated = options.filter((_, i) => i !== indexToRemove);

    let nextCorrect = selectedCorrect;
    if (removedOption.id === selectedCorrect) {
      nextCorrect = updated[0]?.id || "A";
    }

    emitChanges(updated, nextCorrect);
  };

  return (
    <div className="grid gap-2 min-w-0">
      <div className="flex items-center justify-between gap-2">
        <Label className="text-xs font-bold text-slate-800 dark:text-slate-200">
          Danh sách đáp án & Đáp án đúng <span className="text-rose-500 font-bold">*</span>
        </Label>
        <span className="text-[11px] text-slate-400 dark:text-slate-500">
          Chọn ký tự để đặt làm đáp án đúng
        </span>
      </div>

      <div className="rounded-xl border border-slate-200 dark:border-slate-800 bg-slate-50/60 dark:bg-slate-900/40 p-3 sm:p-3.5 space-y-2.5">
        <div className="space-y-2">
          {options.map((option, index) => {
            const isCorrect = option.id === selectedCorrect;

            return (
              <div
                key={option.id || index}
                className={cn(
                  "flex items-center gap-2.5 p-2 rounded-lg border transition-all",
                  isCorrect
                    ? "border-slate-900 dark:border-slate-100 bg-white dark:bg-slate-900 shadow-2xs ring-1 ring-slate-900/10 dark:ring-slate-100/10"
                    : "border-slate-200/80 dark:border-slate-800 bg-white/80 dark:bg-slate-950/40 hover:border-slate-300 dark:hover:border-slate-700"
                )}
              >
                {/* Radio button / Selection trigger */}
                <button
                  type="button"
                  onClick={() => handleSelectCorrect(option.id)}
                  className={cn(
                    "h-7 w-7 rounded-md flex items-center justify-center text-xs font-bold transition-all shrink-0 cursor-pointer",
                    isCorrect
                      ? "bg-slate-900 text-white dark:bg-slate-100 dark:text-slate-900 shadow-xs"
                      : "bg-slate-100 text-slate-600 dark:bg-slate-800 dark:text-slate-400 hover:bg-slate-200 dark:hover:bg-slate-700 hover:text-slate-900 dark:hover:text-slate-100"
                  )}
                  title={`Chọn ${option.id} làm đáp án đúng`}
                >
                  {option.id}
                </button>

                {/* Option text */}
                <Input
                  value={option.text}
                  onChange={(e) => handleTextChange(index, e.target.value)}
                  placeholder={`Nội dung đáp án ${option.id}...`}
                  className={cn(
                    "h-8 text-xs flex-1 bg-white dark:bg-slate-950/60 border-slate-200 dark:border-slate-800 focus-visible:ring-slate-900 dark:focus-visible:ring-slate-100",
                    !option.text.trim() && "border-dashed"
                  )}
                />

                {/* Status indicator */}
                {isCorrect ? (
                  <span className="shrink-0 text-[10px] font-semibold tracking-wider px-2 py-0.5 rounded-full bg-slate-900 text-white dark:bg-slate-100 dark:text-slate-900 flex items-center gap-1 shadow-2xs">
                    <Check className="h-3 w-3 stroke-[2.5]" /> Đáp án đúng
                  </span>
                ) : (
                  <button
                    type="button"
                    onClick={() => handleSelectCorrect(option.id)}
                    className="shrink-0 text-[11px] text-slate-400 dark:text-slate-500 hover:text-slate-800 dark:hover:text-slate-200 px-2 py-0.5 rounded hover:bg-slate-100 dark:hover:bg-slate-800 transition-colors cursor-pointer"
                  >
                    Chọn đúng
                  </button>
                )}

                {/* Remove button */}
                <Button
                  type="button"
                  variant="ghost"
                  size="icon"
                  disabled={options.length <= 2}
                  onClick={() => handleRemoveOption(index)}
                  className="h-7 w-7 text-slate-400 hover:text-rose-600 dark:hover:text-rose-400 shrink-0 cursor-pointer disabled:opacity-20"
                  title={options.length <= 2 ? "Tối thiểu 2 đáp án" : `Xóa đáp án ${option.id}`}
                >
                  <Trash2 className="h-3.5 w-3.5" />
                </Button>
              </div>
            );
          })}
        </div>

        {/* Add option button */}
        <div className="pt-1">
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={handleAddOption}
            disabled={options.length >= 8}
            className="w-full h-8 text-xs font-medium border-dashed border-slate-300 dark:border-slate-700 hover:border-slate-400 dark:hover:border-slate-600 hover:bg-slate-100/60 dark:hover:bg-slate-800/60 cursor-pointer text-slate-700 dark:text-slate-300"
          >
            <Plus className="h-3.5 w-3.5 mr-1.5" /> Thêm lựa chọn đáp án ({options.length}/8)
          </Button>
        </div>
      </div>

      <FieldError errors={fieldErrors} />
    </div>
  );
};

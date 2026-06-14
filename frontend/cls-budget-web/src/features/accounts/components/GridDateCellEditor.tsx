"use client";

import {
  forwardRef,
  useEffect,
  useImperativeHandle,
  useRef,
  useState,
} from "react";
import type { ICellEditorParams } from "ag-grid-community";
import { CalendarDays } from "lucide-react";
import {
  formatDateForGrid,
  toDateInputValue,
} from "@/features/accounts/utils/accountMapper";

export const GridDateCellEditor = forwardRef<
  { getValue: () => string },
  ICellEditorParams
>((props, ref) => {
  const textRef = useRef<HTMLInputElement>(null);
  const dateRef = useRef<HTMLInputElement>(null);
  const [value, setValue] = useState("");

  useEffect(() => {
    const field = props.colDef.field;
    const raw =
      field && props.node.data
        ? (props.node.data as Record<string, unknown>)[field]
        : props.value;
    const iso = typeof raw === "string" ? raw : null;
    setValue(formatDateForGrid(iso));
    textRef.current?.focus();
    textRef.current?.select();
  }, [props.colDef.field, props.node.data, props.value]);

  useImperativeHandle(ref, () => ({
    getValue: () => value.trim(),
  }));

  const openCalendar = () => {
    const field = props.colDef.field;
    const raw =
      field && props.node.data
        ? ((props.node.data as Record<string, unknown>)[field] as
            | string
            | null
            | undefined)
        : null;
    if (dateRef.current) {
      dateRef.current.value = toDateInputValue(raw ?? null);
      dateRef.current.showPicker?.();
    }
  };

  const onCalendarChange = (next: string) => {
    if (!next) return;
    const [year, month, day] = next.split("-");
    setValue(`${month}/${day}/${year}`);
    textRef.current?.focus();
  };

  return (
    <div className="grid-date-cell-editor">
      <input
        ref={textRef}
        type="text"
        className="grid-date-cell-editor-input"
        value={value}
        placeholder="MM/DD/YYYY"
        onChange={(event) => setValue(event.target.value)}
        onKeyDown={(event) => event.stopPropagation()}
      />
      <button
        type="button"
        className="grid-date-cell-editor-calendar"
        aria-label="Open calendar"
        tabIndex={-1}
        onMouseDown={(event) => event.preventDefault()}
        onClick={openCalendar}
      >
        <CalendarDays size={14} strokeWidth={2} />
      </button>
      <input
        ref={dateRef}
        type="date"
        className="grid-date-cell-editor-native-date"
        tabIndex={-1}
        aria-hidden
        onChange={(event) => onCalendarChange(event.target.value)}
      />
    </div>
  );
});

GridDateCellEditor.displayName = "GridDateCellEditor";

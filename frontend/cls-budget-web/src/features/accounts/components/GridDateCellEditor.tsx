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

function readIsoFromEditorParams(props: ICellEditorParams): string | null {
  const field = props.colDef.field;
  const fromData =
    field && props.node.data
      ? (props.node.data as Record<string, unknown>)[field]
      : null;

  if (typeof fromData === "string") return fromData;

  if (props.value instanceof Date && Number.isFinite(props.value.getTime())) {
    return new Date(
      Date.UTC(
        props.value.getUTCFullYear(),
        props.value.getUTCMonth(),
        props.value.getUTCDate(),
      ),
    ).toISOString();
  }

  if (typeof props.value === "string") return props.value;

  return null;
}

export const GridDateCellEditor = forwardRef<
  { getValue: () => string },
  ICellEditorParams
>((props, ref) => {
  const textRef = useRef<HTMLInputElement>(null);
  const dateRef = useRef<HTMLInputElement>(null);
  const initialDisplay = formatDateForGrid(readIsoFromEditorParams(props));
  const valueRef = useRef(initialDisplay);
  const [value, setValue] = useState(initialDisplay);

  const setEditorValue = (next: string) => {
    valueRef.current = next;
    setValue(next);
  };

  useEffect(() => {
    textRef.current?.focus();
    textRef.current?.select();
  }, []);

  useImperativeHandle(
    ref,
    () => ({
      getValue: () => valueRef.current.trim(),
    }),
    [],
  );

  const openCalendar = () => {
    const iso = readIsoFromEditorParams(props);
    if (dateRef.current) {
      dateRef.current.value = toDateInputValue(iso);
      dateRef.current.showPicker?.();
    }
  };

  const onCalendarChange = (next: string) => {
    if (!next) return;
    const [year, month, day] = next.split("-");
    setEditorValue(`${month}/${day}/${year}`);
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
        onChange={(event) => setEditorValue(event.target.value)}
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

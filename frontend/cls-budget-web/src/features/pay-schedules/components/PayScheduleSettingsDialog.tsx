"use client";

import { useEffect, useMemo, useState } from "react";
import { Save, X } from "lucide-react";
import { paySchedulesApi } from "@/features/pay-schedules/api/paySchedulesApi";
import {
  PayFrequencyTypeIds,
  type PayFrequencyTypeResponse,
  type PayScheduleResponse,
} from "@/features/pay-schedules/types/paySchedule";
import { toDateInputValue, dateInputToIso } from "@/features/budgets/utils/budgetFormat";
import { Card } from "@/components/ui/Card";
import { ApiError } from "@/lib/api/client";

export function PayScheduleSettingsDialog({
  schedule,
  onClose,
  onSaved,
}: {
  schedule: PayScheduleResponse;
  onClose: () => void;
  onSaved: (schedule: PayScheduleResponse) => void;
}) {
  const [frequencyTypes, setFrequencyTypes] = useState<
    PayFrequencyTypeResponse[]
  >([]);
  const [payFrequencyTypeId, setPayFrequencyTypeId] = useState(
    schedule.payFrequencyTypeId,
  );
  const [name, setName] = useState(schedule.name);
  const [anchorDate, setAnchorDate] = useState(
    schedule.anchorDate ? toDateInputValue(schedule.anchorDate) : "",
  );
  const [semiMonthlyDay1, setSemiMonthlyDay1] = useState(
    String(schedule.semiMonthlyDay1 ?? 1),
  );
  const [semiMonthlyDay2, setSemiMonthlyDay2] = useState(
    String(schedule.semiMonthlyDay2 ?? 15),
  );
  const [isDefault, setIsDefault] = useState(schedule.isDefault);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const isSemiMonthly = payFrequencyTypeId === PayFrequencyTypeIds.SemiMonthly;
  const isIntervalBased =
    payFrequencyTypeId === PayFrequencyTypeIds.Weekly ||
    payFrequencyTypeId === PayFrequencyTypeIds.BiWeekly;

  useEffect(() => {
    void paySchedulesApi
      .getFrequencyTypes()
      .then((result) => setFrequencyTypes(result.data ?? []))
      .catch(() => setFrequencyTypes([]));
  }, []);

  useEffect(() => {
    function onKeyDown(event: KeyboardEvent) {
      if (event.key === "Escape" && !submitting) onClose();
    }

    document.addEventListener("keydown", onKeyDown);
    return () => document.removeEventListener("keydown", onKeyDown);
  }, [onClose, submitting]);

  const frequencyLabel = useMemo(
    () =>
      frequencyTypes.find((type) => type.payFrequencyTypeId === payFrequencyTypeId)
        ?.name ?? "Pay frequency",
    [frequencyTypes, payFrequencyTypeId],
  );

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    setSubmitting(true);
    setError(null);

    const parsedDay1 = Number.parseInt(semiMonthlyDay1, 10);
    const parsedDay2 = Number.parseInt(semiMonthlyDay2, 10);

    if (isSemiMonthly) {
      if (
        !Number.isFinite(parsedDay1) ||
        parsedDay1 < 1 ||
        parsedDay1 > 31 ||
        !Number.isFinite(parsedDay2) ||
        parsedDay2 < 1 ||
        parsedDay2 > 31
      ) {
        setError("Semi-monthly pay days must be between 1 and 31.");
        setSubmitting(false);
        return;
      }
    }

    if (isIntervalBased && !anchorDate) {
      setError("Choose an anchor pay date for weekly or biweekly schedules.");
      setSubmitting(false);
      return;
    }

    try {
      const result = await paySchedulesApi.update(schedule.payScheduleId, {
        incomeSourceId: schedule.incomeSourceId,
        payFrequencyTypeId,
        name: name.trim() || schedule.name,
        anchorDate: isIntervalBased ? dateInputToIso(anchorDate) : null,
        dayOfWeek: isIntervalBased
          ? new Date(dateInputToIso(anchorDate)).getUTCDay()
          : null,
        semiMonthlyDay1: isSemiMonthly ? parsedDay1 : null,
        semiMonthlyDay2: isSemiMonthly ? parsedDay2 : null,
        isDefault,
        isActive: schedule.isActive,
      });
      onSaved(result.data!);
      onClose();
    } catch (err) {
      const message =
        err instanceof ApiError
          ? err.errors.length > 0
            ? err.errors.join(" ")
            : err.message
          : err instanceof Error
            ? err.message
            : "Failed to save pay schedule";
      setError(message);
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4"
      role="dialog"
      aria-modal="true"
      aria-labelledby="pay-schedule-settings-title"
      onClick={submitting ? undefined : onClose}
    >
      <Card className="w-full max-w-lg p-5 shadow-xl">
        <div onClick={(event) => event.stopPropagation()}>
          <div className="mb-4 flex items-start justify-between gap-3">
            <div>
              <h2
                id="pay-schedule-settings-title"
                className="text-lg font-semibold text-[var(--foreground)]"
              >
                Pay schedule
              </h2>
              <p className="mt-1 text-sm text-[var(--muted)]">
                Controls how the budget header groups payments by paycheck.
              </p>
            </div>
            <button
              type="button"
              onClick={onClose}
              disabled={submitting}
              className="rounded-lg p-1 text-[var(--muted)] hover:bg-black/[0.04] disabled:opacity-40"
              aria-label="Close"
            >
              <X size={18} />
            </button>
          </div>

          <form onSubmit={(event) => void handleSubmit(event)} className="space-y-4">
            <label className="block text-sm">
              <span className="mb-1.5 block font-medium">Schedule name</span>
              <input
                type="text"
                value={name}
                onChange={(event) => setName(event.target.value)}
                disabled={submitting}
                className="w-full rounded-xl border border-[var(--border)] bg-white px-3 py-2.5 text-sm"
              />
            </label>

            <label className="block text-sm">
              <span className="mb-1.5 block font-medium">Pay frequency</span>
              <select
                value={payFrequencyTypeId}
                onChange={(event) =>
                  setPayFrequencyTypeId(Number(event.target.value))
                }
                disabled={submitting}
                className="w-full rounded-xl border border-[var(--border)] bg-white px-3 py-2.5 text-sm"
              >
                {frequencyTypes.map((type) => (
                  <option
                    key={type.payFrequencyTypeId}
                    value={type.payFrequencyTypeId}
                  >
                    {type.name}
                  </option>
                ))}
              </select>
            </label>

            {isSemiMonthly ? (
              <div className="grid gap-4 sm:grid-cols-2">
                <label className="block text-sm">
                  <span className="mb-1.5 block font-medium">First pay day</span>
                  <input
                    type="number"
                    min="1"
                    max="31"
                    value={semiMonthlyDay1}
                    onChange={(event) => setSemiMonthlyDay1(event.target.value)}
                    disabled={submitting}
                    className="w-full rounded-xl border border-[var(--border)] bg-white px-3 py-2.5 text-sm"
                  />
                </label>
                <label className="block text-sm">
                  <span className="mb-1.5 block font-medium">Second pay day</span>
                  <input
                    type="number"
                    min="1"
                    max="31"
                    value={semiMonthlyDay2}
                    onChange={(event) => setSemiMonthlyDay2(event.target.value)}
                    disabled={submitting}
                    className="w-full rounded-xl border border-[var(--border)] bg-white px-3 py-2.5 text-sm"
                  />
                </label>
              </div>
            ) : null}

            {isIntervalBased ? (
              <label className="block text-sm">
                <span className="mb-1.5 block font-medium">
                  Anchor pay date ({frequencyLabel})
                </span>
                <input
                  type="date"
                  value={anchorDate}
                  onChange={(event) => setAnchorDate(event.target.value)}
                  disabled={submitting}
                  required
                  className="w-full rounded-xl border border-[var(--border)] bg-white px-3 py-2.5 text-sm"
                />
                <p className="mt-1 text-xs text-[var(--muted)]">
                  Any recent paycheck date on your {frequencyLabel.toLowerCase()}{" "}
                  cycle.
                </p>
              </label>
            ) : null}

            <label className="flex items-center gap-2 text-sm">
              <input
                type="checkbox"
                checked={isDefault}
                onChange={(event) => setIsDefault(event.target.checked)}
                disabled={submitting}
              />
              Use as default pay schedule for new budgets
            </label>

            {error ? (
              <p className="rounded-xl border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-900">
                {error}
              </p>
            ) : null}

            <div className="flex justify-end gap-2">
              <button
                type="button"
                onClick={onClose}
                disabled={submitting}
                className="rounded-full border border-[var(--border)] px-4 py-2 text-sm font-medium disabled:opacity-40"
              >
                Cancel
              </button>
              <button
                type="submit"
                disabled={submitting}
                className="inline-flex items-center gap-2 rounded-full bg-[var(--link)] px-4 py-2 text-sm font-semibold text-white disabled:opacity-40"
              >
                <Save size={15} aria-hidden />
                {submitting ? "Saving…" : "Save schedule"}
              </button>
            </div>
          </form>
        </div>
      </Card>
    </div>
  );
}

import { Suspense } from "react";
import { ResetPasswordForm } from "@/features/auth/components/ResetPasswordForm";

export default function ResetPasswordPage() {
  return (
    <div className="flex min-h-screen flex-col items-center justify-center bg-[var(--background)] px-4 py-12">
      <LinkBrand />
      <Suspense
        fallback={
          <div className="mt-8 text-sm text-[var(--muted)]">Loading…</div>
        }
      >
        <div className="mt-8 w-full flex justify-center">
          <ResetPasswordForm />
        </div>
      </Suspense>
    </div>
  );
}

function LinkBrand() {
  return (
    <a href="/" className="text-xl font-bold tracking-tight">
      CLS<span className="text-[var(--link)]">Budget</span>
    </a>
  );
}

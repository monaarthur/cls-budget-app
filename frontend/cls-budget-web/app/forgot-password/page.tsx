import { ForgotPasswordForm } from "@/features/auth/components/ForgotPasswordForm";

export default function ForgotPasswordPage() {
  return (
    <div className="flex min-h-screen flex-col items-center justify-center bg-[var(--background)] px-4 py-12">
      <LinkBrand />
      <div className="mt-8 w-full flex justify-center">
        <ForgotPasswordForm />
      </div>
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

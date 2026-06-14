import { RegisterForm } from "@/features/auth/components/RegisterForm";

export default function RegisterPage() {
  return (
    <div className="flex min-h-screen flex-col items-center justify-center bg-[var(--background)] px-4 py-12">
      <a href="/" className="text-xl font-bold tracking-tight">
        CLS<span className="text-[var(--link)]">Budget</span>
      </a>
      <div className="mt-8 w-full flex justify-center">
        <RegisterForm />
      </div>
    </div>
  );
}

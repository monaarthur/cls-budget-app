import { AdminPageContent } from "@/features/admin/components/AdminPageContent";

export default function AdminPage() {
  return (
    <div className="flex min-h-screen flex-col items-center bg-[var(--background)] px-4 py-12">
      <LinkBrand />
      <div className="mt-8 w-full flex justify-center">
        <AdminPageContent />
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

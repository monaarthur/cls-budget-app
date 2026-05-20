import { PaymentList } from "@/features/payments";
import { TopBar } from "@/components/layout/TopBar";

export default function PaymentsPage() {
  return (
    <>
      <TopBar title="Payments" />
      <PaymentList />
    </>
  );
}

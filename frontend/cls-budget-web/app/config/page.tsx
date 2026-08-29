import { TopBar } from "@/components/layout/TopBar";
import { ConfigHub } from "@/features/config/components/ConfigHub";

export default function ConfigPage() {
  return (
    <>
      <TopBar title="Config" />
      <ConfigHub />
    </>
  );
}

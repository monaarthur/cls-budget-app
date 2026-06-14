import { syncAccountLogos } from "@/lib/syncAccountLogosServer";

export const runtime = "nodejs";

export async function POST() {
  try {
    const result = await syncAccountLogos();
    return Response.json(result);
  } catch (error) {
    const message =
      error instanceof Error ? error.message : "Failed to sync account logos";
    return Response.json({ message }, { status: 500 });
  }
}

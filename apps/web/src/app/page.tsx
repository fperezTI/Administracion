import Link from "next/link";
import { getTranslations } from "next-intl/server";
import { auth, signIn } from "@/auth";
import { Button } from "@/components/ui/button";
import { ThemeToggle } from "@/components/theme-toggle";
import { Logomark } from "@/components/logomark";
import { getSystemInfo } from "@/lib/api";

export default async function Home() {
  const t = await getTranslations("Home");
  const session = await auth();

  let statusLine: { text: string; error: boolean };
  try {
    const info = await getSystemInfo();
    statusLine = {
      text: `${info.product} — ${info.environment} — ${new Date(info.serverTimeUtc).toLocaleString("es-MX")}`,
      error: false,
    };
  } catch {
    statusLine = { text: t("systemInfoError"), error: true };
  }

  return (
    <div className="flex min-h-screen flex-col items-center justify-center gap-8 p-8">
      <div className="absolute top-4 right-4">
        <ThemeToggle />
      </div>
      <div className="flex flex-col items-center gap-4 text-center">
        <Logomark withWordmark size="lg" className="flex-col" />
        <div className="flex flex-col items-center gap-1.5">
          <h1 className="max-w-sm text-2xl font-semibold tracking-tight text-balance">{t("title")}</h1>
          <p className="text-muted-foreground max-w-xs text-sm text-balance">{t("subtitle")}</p>
        </div>
      </div>

      {session?.user ? (
        <Button size="lg" render={<Link href="/dashboard">{t("goToDashboard")}</Link>} />
      ) : (
        <form
          action={async () => {
            "use server";
            await signIn("microsoft-entra-id", { redirectTo: "/dashboard" });
          }}
        >
          <Button type="submit" size="lg">
            {t("signIn")}
          </Button>
        </form>
      )}

      <div className="flex items-center gap-2 rounded-md border px-3 py-2 font-mono text-xs">
        <span
          aria-hidden
          className={statusLine.error ? "bg-destructive size-1.5 rounded-full" : "bg-success size-1.5 rounded-full"}
        />
        <span className="text-muted-foreground">{statusLine.text}</span>
      </div>
    </div>
  );
}

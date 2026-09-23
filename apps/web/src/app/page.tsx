import Link from "next/link";
import { getTranslations } from "next-intl/server";
import { ShieldCheck } from "lucide-react";
import { auth, signIn } from "@/auth";
import { Button } from "@/components/ui/button";
import { ThemeToggle } from "@/components/theme-toggle";
import { Logomark } from "@/components/logomark";
import { MicrosoftLogo } from "@/components/microsoft-logo";
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

  const bullets = [t("heroBullet1"), t("heroBullet2"), t("heroBullet3")];

  return (
    <div className="grid min-h-screen lg:grid-cols-2">
      {/* Panel de héroe: solo desktop — en móvil no hay espacio para algo decorativo, la tarjeta
       * de acceso es lo único que importa. */}
      <div className="bg-surface-brand text-surface-brand-foreground relative hidden flex-col justify-between overflow-hidden p-10 lg:flex">
        <div
          aria-hidden
          className="bg-brand/25 pointer-events-none absolute -top-24 -right-24 size-96 rounded-full blur-3xl"
        />
        <Logomark withWordmark inverse />
        <div className="relative flex flex-col gap-6">
          <h1 className="text-4xl leading-tight font-bold tracking-tight text-balance">
            {t("heroHeadlinePlain")}
            <br />
            <span className="text-brand">{t("heroHeadlineAccent")}</span>
          </h1>
          <p className="text-surface-brand-foreground/80 max-w-sm text-base text-balance">{t("heroSubtitle")}</p>
          <ul className="flex flex-col gap-3">
            {bullets.map((bullet) => (
              <li key={bullet} className="flex items-center gap-3 text-sm">
                <span aria-hidden className="bg-brand size-1.5 shrink-0 rounded-full" />
                {bullet}
              </li>
            ))}
          </ul>
        </div>
        <div />
      </div>

      {/* Panel de acceso */}
      <div className="bg-background relative flex flex-col items-center justify-center gap-6 p-8">
        <div className="absolute top-4 right-4">
          <ThemeToggle />
        </div>
        <div className="lg:hidden">
          <Logomark withWordmark size="lg" className="flex-col" />
        </div>

        <div className="bg-card w-full max-w-sm rounded-xl p-8 ring-1 ring-foreground/10">
          <h2 className="text-2xl font-semibold tracking-tight">{t("welcomeHeading")}</h2>
          <p className="text-muted-foreground mt-1.5 text-sm">{t("welcomeSubtitle")}</p>

          <div className="mt-6">
            {session?.user ? (
              <Button size="lg" className="w-full" render={<Link href="/dashboard">{t("goToDashboard")}</Link>} />
            ) : (
              <form
                action={async () => {
                  "use server";
                  await signIn("microsoft-entra-id", { redirectTo: "/dashboard" });
                }}
              >
                <Button type="submit" size="lg" className="w-full">
                  <MicrosoftLogo data-icon="inline-start" />
                  {t("signIn")}
                </Button>
              </form>
            )}
          </div>

          <div className="text-muted-foreground mt-6 flex items-center justify-center gap-2 border-t pt-4 text-xs">
            <ShieldCheck className="size-3.5 stroke-[1.5]" />
            {t("secureFooter")}
          </div>
        </div>

        <div className="flex items-center gap-2 rounded-md border px-3 py-2 font-mono text-xs">
          <span
            aria-hidden
            className={statusLine.error ? "bg-destructive size-1.5 rounded-full" : "bg-success size-1.5 rounded-full"}
          />
          <span className="text-muted-foreground">{statusLine.text}</span>
        </div>
      </div>
    </div>
  );
}

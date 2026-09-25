import Link from "next/link";
import { Bell } from "lucide-react";
import { getTranslations } from "next-intl/server";
import { auth } from "@/auth";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { ThemeToggle } from "@/components/theme-toggle";
import { Logomark } from "@/components/logomark";
import { MobileNav } from "@/components/layout/app-nav";
import { UserMenu } from "@/components/layout/user-menu";

/** Chrome global de la aplicación: se monta una sola vez (en AppShell), no por página. Todo lo
 * específico de cada página (título, subtítulo, breadcrumbs, selector de empresa) sigue viviendo
 * en el AppHeader que cada página ya invoca — ver components/app-header.tsx. */
export async function Topbar() {
  const t = await getTranslations("Layout");
  const session = await auth();
  const user = {
    name: session?.user?.name ?? null,
    email: session?.user?.email ?? null,
    image: session?.user?.image ?? null,
  };

  return (
    <header className="bg-background sticky top-0 z-30 flex items-center gap-3 border-b px-4 py-2.5 lg:px-6">
      <div className="lg:hidden">
        <MobileNav />
      </div>
      <Logomark className="lg:hidden" />

      <form method="GET" action="/search" className="flex flex-1 items-center">
        <Input
          name="term"
          placeholder={t("searchPlaceholder")}
          aria-label={t("searchPlaceholder")}
          className="h-8 w-full max-w-80"
        />
      </form>

      <div className="flex shrink-0 items-center gap-2">
        <Button
          variant="ghost"
          size="icon"
          nativeButton={false}
          aria-label={t("notifications")}
          render={<Link href="/notifications" />}
        >
          <Bell className="size-4 stroke-[1.5]" />
        </Button>
        <ThemeToggle />
        <UserMenu name={user.name} email={user.email} image={user.image} />
      </div>
    </header>
  );
}

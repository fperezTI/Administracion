"use client";

import { LogOut } from "lucide-react";
import { useTranslations } from "next-intl";
import { cn } from "cn";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuGroup,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { federatedSignOut } from "@/lib/auth-actions";

function getInitials(name: string | null, email: string | null) {
  const source = name ?? email;
  if (!source) return "?";
  const parts = source.trim().split(/\s+/);
  if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase();
  return `${parts[0][0]}${parts[parts.length - 1][0]}`.toUpperCase();
}

export function UserMenu({ name, email, image }: { name: string | null; email: string | null; image: string | null }) {
  const t = useTranslations("Layout");
  const initials = getInitials(name, email);
  const displayName = name ?? t("userNameFallback");

  return (
    <DropdownMenu>
      <DropdownMenuTrigger
        aria-label={t("userMenu")}
        className={cn(
          "flex size-8 shrink-0 items-center justify-center rounded-full bg-primary text-xs font-semibold text-primary-foreground outline-none",
          "focus-visible:ring-3 focus-visible:ring-ring/50"
        )}
      >
        {image ? (
          // eslint-disable-next-line @next/next/no-img-element -- avatar hosted by Microsoft Graph, not an optimizable local/remote asset
          <img src={image} alt="" className="size-8 rounded-full object-cover" />
        ) : (
          <span aria-hidden>{initials}</span>
        )}
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-56">
        <DropdownMenuGroup>
          <DropdownMenuLabel className="flex flex-col gap-0.5 px-2 py-1.5">
            <span className="text-foreground truncate text-sm font-medium">{displayName}</span>
            {email && <span className="text-muted-foreground truncate text-xs">{email}</span>}
          </DropdownMenuLabel>
        </DropdownMenuGroup>
        <DropdownMenuSeparator />
        <form action={federatedSignOut}>
          <button
            type="submit"
            className="text-destructive hover:bg-destructive/10 focus-visible:bg-destructive/10 flex w-full items-center gap-1.5 rounded-md px-1.5 py-1 text-sm outline-none [&_svg]:size-4 [&_svg]:shrink-0 [&_svg]:stroke-[1.5]"
          >
            <LogOut aria-hidden />
            {t("signOut")}
          </button>
        </form>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}

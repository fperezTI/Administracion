"use client";

import { useEffect, useRef, useState, useTransition } from "react";
import { useTranslations } from "next-intl";
import { Check, Palette } from "lucide-react";
import { cn } from "cn";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { DropdownMenuItem } from "@/components/ui/dropdown-menu";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { confirmThemeSelectionAction, previewCompanyThemeAction } from "@/lib/theme-actions";
import {
  INHERIT_COMPANY_THEME,
  THEME_CODES,
  isThemeCode,
  resolveThemeCode,
  type ThemeCode,
  type ThemeSelection,
} from "@/lib/theme-catalog";

const PREVIEW_SECONDS = 15;

/** Entry point rendered inside the profile dropdown (`DropdownMenuContent`). Deliberately just a
 * plain item that flips state owned by the parent `UserMenu` — the actual dialog lives outside the
 * dropdown's own subtree (see `ThemeSelectorDialog` below), because Base UI unmounts a Menu's popup
 * (and everything inside it, dialog included) once it finishes closing. */
export function ThemeMenuItem({ onOpen }: { onOpen: () => void }) {
  const tLayout = useTranslations("Layout");
  return (
    <DropdownMenuItem onClick={onOpen}>
      <Palette aria-hidden />
      {tLayout("theme")}
    </DropdownMenuItem>
  );
}

/** One swatch per theme, built entirely from the same semantic tokens the theme itself defines — no
 * hardcoded colors here (pedido: "no coloques valores de color directamente en componentes
 * funcionales"). Rendered inside a `<div className={themeClass}>` scope so each swatch picks up
 * that theme's own token values regardless of which theme is currently applied to <html>. */
function ThemeSwatch({ themeClass }: { themeClass: string }) {
  return (
    <div className={cn(themeClass, "bg-background flex w-full gap-1 rounded-md border p-1.5")}>
      <div className="bg-primary size-3 rounded-full" />
      <div className="bg-accent size-3 rounded-full" />
      <div className="bg-muted-foreground/40 size-3 rounded-full" />
      <div className="bg-card ml-auto h-3 flex-1 rounded-sm border" />
    </div>
  );
}

function ThemeOptionButton({
  themeClassForSwatch,
  label,
  isSelected,
  isEffective,
  onSelect,
}: {
  themeClassForSwatch: string;
  label: string;
  isSelected: boolean;
  isEffective: boolean;
  onSelect: () => void;
}) {
  const t = useTranslations("Theme");
  return (
    <button
      type="button"
      role="radio"
      aria-checked={isSelected}
      onClick={onSelect}
      className={cn(
        "flex flex-col gap-2 rounded-lg border p-2 text-left outline-none transition-colors",
        "focus-visible:ring-3 focus-visible:ring-ring/50",
        isSelected ? "border-primary bg-accent" : "border-border hover:bg-muted"
      )}
    >
      <ThemeSwatch themeClass={themeClassForSwatch} />
      <div className="flex items-center justify-between gap-1">
        <span className="text-sm font-medium">{label}</span>
        {isSelected && <Check className="text-primary size-4 shrink-0 stroke-[1.5]" aria-hidden />}
      </div>
      {isEffective && (
        <Badge variant="info" className="w-fit">
          {t("currentlyApplied")}
        </Badge>
      )}
    </button>
  );
}

/** The actual gallery/preview/confirm/restore/timer UI. Mounted once in `UserMenu`, as a sibling of
 * `DropdownMenu` (not inside it), and controlled entirely via `open`/`onOpenChange` — so it survives
 * the dropdown menu closing (which happens as soon as `ThemeMenuItem` is clicked) and keeps its own
 * preview/timer state independent of that unrelated unmount. */
export function ThemeSelectorDialog({
  open,
  onOpenChange,
  effectiveTheme,
  themePreference,
}: {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  effectiveTheme: ThemeCode;
  themePreference: string | null;
}) {
  const t = useTranslations("Theme");
  const [pending, startTransition] = useTransition();
  const [previewSelection, setPreviewSelection] = useState<ThemeSelection | null>(null);
  const [secondsLeft, setSecondsLeft] = useState<number | null>(null);
  const [error, setError] = useState<string | null>(null);

  const originalClassNameRef = useRef<string>("");
  const timerRef = useRef<ReturnType<typeof setInterval> | null>(null);
  // Mirrors `previewSelection` for the unmount cleanup below, which otherwise closes over the
  // value from its one-time mount render (always `null`) instead of the latest one.
  const previewSelectionRef = useRef<ThemeSelection | null>(null);
  previewSelectionRef.current = previewSelection;

  const currentSelection: ThemeSelection = isThemeCode(themePreference) ? themePreference : INHERIT_COMPANY_THEME;

  function clearTimer() {
    if (timerRef.current) {
      clearInterval(timerRef.current);
      timerRef.current = null;
    }
  }

  function revertPreview() {
    clearTimer();
    document.documentElement.className = originalClassNameRef.current;
    setPreviewSelection(null);
    setSecondsLeft(null);
  }

  async function applyPreview(selection: ThemeSelection) {
    setError(null);
    let previewClass: string;
    if (selection === INHERIT_COMPANY_THEME) {
      // We only know the company's live default by asking — cheap, on-demand, and only happens
      // when this specific option is chosen, not on every render.
      try {
        const companyTheme = await previewCompanyThemeAction();
        previewClass = resolveThemeCode(companyTheme);
      } catch {
        previewClass = effectiveTheme;
      }
    } else {
      previewClass = selection;
    }

    document.documentElement.className = previewClass;
    setPreviewSelection(selection);
    clearTimer();
    setSecondsLeft(PREVIEW_SECONDS);
    timerRef.current = setInterval(() => {
      setSecondsLeft((prev) => {
        if (prev === null || prev <= 1) {
          clearTimer();
          revertPreview();
          return null;
        }
        return prev - 1;
      });
    }, 1000);
  }

  // `open` is flipped to true by `ThemeMenuItem` in the parent `UserMenu`, not by this dialog's own
  // `onOpenChange` — capturing the pre-preview class has to react to that prop change directly,
  // rather than living in `handleOpenChange` below (which only fires for closes this dialog itself
  // initiates: Escape, backdrop click, Restaurar/Conservar).
  useEffect(() => {
    if (open) {
      originalClassNameRef.current = document.documentElement.className;
      setError(null);
    }
  }, [open]);

  function handleOpenChange(nextOpen: boolean) {
    if (nextOpen) {
      onOpenChange(true);
      return;
    }

    if (previewSelection !== null) {
      revertPreview();
    }
    onOpenChange(false);
  }

  function handleConfirm() {
    if (previewSelection === null) {
      onOpenChange(false);
      return;
    }

    const selection = previewSelection;
    startTransition(async () => {
      const result = await confirmThemeSelectionAction(selection);
      if ("error" in result) {
        setError(result.error);
        revertPreview();
        return;
      }
      clearTimer();
      setPreviewSelection(null);
      setSecondsLeft(null);
      onOpenChange(false);
    });
  }

  // Reverts an in-progress, unconfirmed preview if this dialog unmounts mid-preview (e.g. the user
  // navigates away) — pedido: "si navega durante la vista previa, no debe quedar una preferencia
  // temporal persistida accidentalmente". Nothing was ever persisted (the preview only touches the
  // DOM class), so this just restores the visual state for correctness.
  useEffect(() => {
    return () => {
      clearTimer();
      if (previewSelectionRef.current !== null) {
        document.documentElement.className = originalClassNameRef.current;
      }
    };
  }, []);

  const options: { selection: ThemeSelection; label: string }[] = [
    { selection: INHERIT_COMPANY_THEME, label: t("company") },
    ...THEME_CODES.map((code) => ({ selection: code, label: t(code) })),
  ];

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>{t("selectorTitle")}</DialogTitle>
          <DialogDescription>{t("selectorDescription")}</DialogDescription>
        </DialogHeader>

        <div role="radiogroup" aria-label={t("selectorTitle")} className="grid grid-cols-2 gap-2 sm:grid-cols-3">
          {options.map(({ selection, label }) => {
            const isSelected = (previewSelection ?? currentSelection) === selection;
            const isEffective =
              previewSelection === null &&
              (selection === INHERIT_COMPANY_THEME ? themePreference === null : selection === effectiveTheme);
            const swatchClass = selection === INHERIT_COMPANY_THEME ? effectiveTheme : selection;
            return (
              <ThemeOptionButton
                key={selection}
                themeClassForSwatch={swatchClass}
                label={label}
                isSelected={isSelected}
                isEffective={isEffective}
                onSelect={() => applyPreview(selection)}
              />
            );
          })}
        </div>

        <div aria-live="polite" className="min-h-5 text-sm">
          {error && <p className="text-destructive">{error}</p>}
          {!error && previewSelection !== null && secondsLeft !== null && (
            <p className="text-muted-foreground">{t("previewCountdown", { seconds: secondsLeft })}</p>
          )}
        </div>

        <DialogFooter>
          <Button type="button" variant="outline" onClick={revertPreview} disabled={previewSelection === null || pending}>
            {t("restore")}
          </Button>
          <Button type="button" onClick={handleConfirm} disabled={previewSelection === null || pending}>
            {pending ? t("saving") : t("keep")}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

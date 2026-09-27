"use client";

import { useState, useTransition } from "react";
import { useTranslations } from "next-intl";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { THEME_CODES, isThemeCode } from "@/lib/theme-catalog";
import { setCompanyThemeAction } from "./actions";

export function CompanyThemeSelect({ companyId, defaultThemeCode }: { companyId: string; defaultThemeCode: string }) {
  const t = useTranslations("Theme");
  const [value, setValue] = useState(defaultThemeCode);
  const [error, setError] = useState<string | null>(null);
  const [pending, startTransition] = useTransition();

  function handleChange(next: unknown) {
    if (typeof next !== "string" || !isThemeCode(next)) return;
    const previous = value;
    setValue(next);
    setError(null);
    startTransition(async () => {
      const result = await setCompanyThemeAction(companyId, next);
      if (result?.error) {
        setValue(previous);
        setError(result.error);
      }
    });
  }

  return (
    <div className="flex flex-col gap-1">
      <Select value={value} onValueChange={handleChange} disabled={pending}>
        <SelectTrigger size="sm" aria-label={t("selectorTitle")}>
          <SelectValue>{t(value as (typeof THEME_CODES)[number])}</SelectValue>
        </SelectTrigger>
        <SelectContent>
          {THEME_CODES.map((code) => (
            <SelectItem key={code} value={code}>
              {t(code)}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>
      {error && <span className="text-destructive text-xs">{error}</span>}
    </div>
  );
}

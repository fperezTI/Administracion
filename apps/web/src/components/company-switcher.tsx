"use client";

import { useRouter, usePathname, useSearchParams } from "next/navigation";
import type { MeCompany } from "@/lib/api";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";

export function CompanySwitcher({ companies, currentCompanyId }: { companies: MeCompany[]; currentCompanyId: string }) {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();

  function handleChange(companyId: string) {
    const params = new URLSearchParams(searchParams.toString());
    params.set("companyId", companyId);
    params.delete("pageNumber");
    router.push(`${pathname}?${params.toString()}`);
  }

  return (
    <div className="bg-accent text-accent-foreground flex h-8 items-center gap-1.5 rounded-md pl-2.5">
      <span className="text-xs font-medium">Empresa</span>
      <Select value={currentCompanyId} onValueChange={(value) => value && handleChange(value)}>
        <SelectTrigger
          aria-label="Empresa activa"
          className="h-8 w-fit gap-1 border-none bg-transparent pr-2 text-sm font-medium shadow-none focus-visible:ring-3 focus-visible:ring-ring/50"
        >
          <SelectValue>{(value: string) => companies.find((c) => c.companyId === value)?.tradeName}</SelectValue>
        </SelectTrigger>
        <SelectContent>
          {companies.map((company) => (
            <SelectItem key={company.companyId} value={company.companyId}>
              {company.tradeName}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>
    </div>
  );
}

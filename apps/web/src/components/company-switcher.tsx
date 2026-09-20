"use client";

import { useRouter, usePathname, useSearchParams } from "next/navigation";
import type { MeCompany } from "@/lib/api";

export function CompanySwitcher({ companies, currentCompanyId }: { companies: MeCompany[]; currentCompanyId: string }) {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();

  function handleChange(event: React.ChangeEvent<HTMLSelectElement>) {
    const params = new URLSearchParams(searchParams.toString());
    params.set("companyId", event.target.value);
    params.delete("pageNumber");
    router.push(`${pathname}?${params.toString()}`);
  }

  return (
    <label className="bg-accent text-accent-foreground flex h-8 items-center gap-1.5 rounded-md pl-2.5">
      <span className="text-xs font-medium">Empresa</span>
      <select
        value={currentCompanyId}
        onChange={handleChange}
        aria-label="Empresa activa"
        className="h-8 rounded-md border-none bg-transparent pr-2 text-sm font-medium outline-none focus-visible:ring-3 focus-visible:ring-ring/50"
      >
        {companies.map((company) => (
          <option key={company.companyId} value={company.companyId}>
            {company.tradeName}
          </option>
        ))}
      </select>
    </label>
  );
}

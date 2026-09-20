"use client";

import { useActionState } from "react";
import type { AssetSummary, UserSummary } from "@/lib/api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { createLoanAction, type CreateLoanActionState } from "./actions";

const initialState: CreateLoanActionState = { error: null };

const selectClassName =
  "h-8 rounded-lg border border-input bg-transparent px-2.5 text-sm outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 dark:bg-input/30";

export function CreateLoanForm({ assets, users }: { assets: AssetSummary[]; users: UserSummary[] }) {
  const [state, formAction, pending] = useActionState(createLoanAction, initialState);
  const minDate = new Date().toISOString().slice(0, 10);

  return (
    <form action={formAction} className="flex flex-col gap-4">
      <div className="flex flex-col gap-1">
        <Label htmlFor="assetId">Activo (debe estar en almacén)</Label>
        <select id="assetId" name="assetId" className={selectClassName} required>
          <option value="" disabled defaultValue="">
            Selecciona
          </option>
          {assets.map((asset) => (
            <option key={asset.id} value={asset.id}>
              {asset.internalFolio} — {asset.brand} {asset.model}
            </option>
          ))}
        </select>
        {assets.length === 0 && (
          <p className="text-muted-foreground text-xs">No hay activos en almacén disponibles para prestar.</p>
        )}
      </div>

      <div className="flex flex-col gap-1">
        <Label htmlFor="borrowerUserId">Destinatario</Label>
        <select id="borrowerUserId" name="borrowerUserId" className={selectClassName} required>
          <option value="" disabled defaultValue="">
            Selecciona
          </option>
          {users.map((user) => (
            <option key={user.id} value={user.id}>
              {user.displayName} — {user.email}
            </option>
          ))}
        </select>
      </div>

      <div className="flex flex-col gap-1">
        <Label htmlFor="expectedReturnDate">Fecha esperada de devolución</Label>
        <Input id="expectedReturnDate" name="expectedReturnDate" type="date" min={minDate} required />
      </div>

      <div className="flex flex-col gap-1">
        <Label htmlFor="notes">Notas (opcional)</Label>
        <Input id="notes" name="notes" maxLength={500} />
      </div>

      {state.error && <p className="text-destructive text-sm">{state.error}</p>}

      <div className="flex justify-end">
        <Button type="submit" disabled={pending}>
          {pending ? "Guardando…" : "Crear préstamo"}
        </Button>
      </div>
    </form>
  );
}

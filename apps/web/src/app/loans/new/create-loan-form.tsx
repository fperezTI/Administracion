"use client";

import { useActionState } from "react";
import type { AssetSummary, UserSummary } from "@/lib/api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { createLoanAction, type CreateLoanActionState } from "./actions";

const initialState: CreateLoanActionState = { error: null };

export function CreateLoanForm({ assets, users }: { assets: AssetSummary[]; users: UserSummary[] }) {
  const [state, formAction, pending] = useActionState(createLoanAction, initialState);
  const minDate = new Date().toISOString().slice(0, 10);

  return (
    <form action={formAction} className="flex flex-col gap-4">
      <div className="flex flex-col gap-1">
        <Label htmlFor="assetId">Activo (debe estar en almacén)</Label>
        <Select name="assetId" required>
          <SelectTrigger id="assetId" className="w-full">
            <SelectValue placeholder="Selecciona">
              {(value: string) => {
                const asset = assets.find((a) => a.id === value);
                return asset ? `${asset.internalFolio} — ${asset.brand} ${asset.model}` : null;
              }}
            </SelectValue>
          </SelectTrigger>
          <SelectContent>
            {assets.map((asset) => (
              <SelectItem key={asset.id} value={asset.id}>
                {asset.internalFolio} — {asset.brand} {asset.model}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        {assets.length === 0 && (
          <p className="text-muted-foreground text-xs">No hay activos en almacén disponibles para prestar.</p>
        )}
      </div>

      <div className="flex flex-col gap-1">
        <Label htmlFor="borrowerUserId">Destinatario</Label>
        <Select name="borrowerUserId" required>
          <SelectTrigger id="borrowerUserId" className="w-full">
            <SelectValue placeholder="Selecciona">
              {(value: string) => {
                const user = users.find((u) => u.id === value);
                return user ? `${user.displayName} — ${user.email}` : null;
              }}
            </SelectValue>
          </SelectTrigger>
          <SelectContent>
            {users.map((user) => (
              <SelectItem key={user.id} value={user.id}>
                {user.displayName} — {user.email}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      <div className="flex flex-col gap-1">
        <Label htmlFor="expectedReturnDate">Fecha esperada de devolución</Label>
        <Input id="expectedReturnDate" name="expectedReturnDate" type="date" min={minDate} required />
      </div>

      <div className="flex flex-col gap-1">
        <Label htmlFor="notes">Notas (opcional)</Label>
        <Input id="notes" name="notes" maxLength={500} />
      </div>

      {state.error && <p className="text-destructive text-sm" role="alert">{state.error}</p>}

      <div className="flex justify-end">
        <Button type="submit" disabled={pending}>
          {pending ? "Guardando…" : "Crear préstamo"}
        </Button>
      </div>
    </form>
  );
}

"use client";

import { useActionState, useState } from "react";
import type { ApprovalMode, MeCompany, RoleSummary } from "@/lib/api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { createApprovalFlowAction, type CreateApprovalFlowActionState } from "./actions";

const initialState: CreateApprovalFlowActionState = { error: null };

export function CreateApprovalFlowForm({ roles, companies }: { roles: RoleSummary[]; companies: MeCompany[] }) {
  const [state, formAction, pending] = useActionState(createApprovalFlowAction, initialState);
  const [mode, setMode] = useState<ApprovalMode>("Parallel");
  // Ordered list of role slots — order only matters when mode is Sequential, but tracked always so
  // switching modes doesn't lose the admin's picks.
  const [roleSlots, setRoleSlots] = useState<string[]>([roles[0]?.id ?? ""]);
  const [requiredApprovals, setRequiredApprovals] = useState(1);

  function updateSlot(index: number, roleId: string) {
    setRoleSlots((slots) => slots.map((s, i) => (i === index ? roleId : s)));
  }

  function addSlot() {
    setRoleSlots((slots) => [...slots, roles[0]?.id ?? ""]);
  }

  function removeSlot(index: number) {
    setRoleSlots((slots) => slots.filter((_, i) => i !== index));
  }

  return (
    <form action={formAction} className="flex flex-col gap-4">
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <div className="flex flex-col gap-1">
          <Label htmlFor="key">Clave</Label>
          <Input id="key" name="key" required maxLength={100} placeholder="p. ej. asset.decommission" />
        </div>
        <div className="flex flex-col gap-1">
          <Label htmlFor="companyId">Alcance</Label>
          <Select name="companyId" defaultValue="">
            <SelectTrigger id="companyId" className="w-full">
              <SelectValue>
                {(value: string) => (value === "" ? "Todas las empresas" : companies.find((c) => c.companyId === value)?.tradeName)}
              </SelectValue>
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="">Todas las empresas</SelectItem>
              {companies.map((c) => (
                <SelectItem key={c.companyId} value={c.companyId}>
                  {c.tradeName}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <div className="flex flex-col gap-1">
          <Label htmlFor="mode">Modo</Label>
          <Select name="mode" value={mode} onValueChange={(value) => setMode(value as ApprovalMode)}>
            <SelectTrigger id="mode" className="w-full">
              <SelectValue>
                {(value: ApprovalMode) =>
                  value === "Parallel" ? "Paralelo (cualquier orden)" : "Secuencial (en el orden de la lista)"
                }
              </SelectValue>
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="Parallel">Paralelo (cualquier orden)</SelectItem>
              <SelectItem value="Sequential">Secuencial (en el orden de la lista)</SelectItem>
            </SelectContent>
          </Select>
        </div>
        <div className="flex flex-col gap-1">
          <Label htmlFor="requiredApprovals">Aprobaciones requeridas</Label>
          <Input
            id="requiredApprovals"
            name="requiredApprovals"
            type="number"
            min={1}
            value={mode === "Sequential" ? roleSlots.length : requiredApprovals}
            onChange={(e) => setRequiredApprovals(Number(e.target.value))}
            readOnly={mode === "Sequential"}
            required
          />
          {mode === "Sequential" && (
            <p className="text-muted-foreground text-xs">En modo secuencial se exige una aprobación por cada rol listado.</p>
          )}
        </div>
      </div>

      <div className="flex flex-col gap-2">
        <Label>Roles aprobadores {mode === "Sequential" && "(en orden)"}</Label>
        {roleSlots.map((roleId, index) => (
          <div key={index} className="flex items-center gap-2">
            {mode === "Sequential" && <span className="text-muted-foreground w-5 text-sm">{index + 1}.</span>}
            <Select name="approverRoleIds" value={roleId} onValueChange={(value) => updateSlot(index, value ?? "")} required>
              <SelectTrigger className="w-full flex-1">
                <SelectValue>{(value: string) => roles.find((r) => r.id === value)?.name}</SelectValue>
              </SelectTrigger>
              <SelectContent>
                {roles.map((role) => (
                  <SelectItem key={role.id} value={role.id}>
                    {role.name}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            {roleSlots.length > 1 && (
              <Button type="button" variant="outline" size="sm" onClick={() => removeSlot(index)}>
                Quitar
              </Button>
            )}
          </div>
        ))}
        <Button type="button" variant="outline" size="sm" className="self-start" onClick={addSlot}>
          + Agregar rol
        </Button>
      </div>

      <label className="flex items-center gap-2 text-sm">
        <input type="checkbox" name="requiresComment" className="size-4" />
        Exigir justificación al solicitar
      </label>

      {state.error && <p className="text-destructive text-sm" role="alert">{state.error}</p>}

      <div className="flex justify-end">
        <Button type="submit" disabled={pending}>
          {pending ? "Guardando…" : "Crear flujo"}
        </Button>
      </div>
    </form>
  );
}

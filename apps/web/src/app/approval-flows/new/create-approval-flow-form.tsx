"use client";

import { useActionState, useState } from "react";
import type { ApprovalMode, MeCompany, RoleSummary } from "@/lib/api";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { createApprovalFlowAction, type CreateApprovalFlowActionState } from "./actions";

const initialState: CreateApprovalFlowActionState = { error: null };

const selectClassName =
  "h-8 rounded-lg border border-input bg-transparent px-2.5 text-sm outline-none focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 dark:bg-input/30";

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
      <div className="grid grid-cols-2 gap-4">
        <div className="flex flex-col gap-1">
          <Label htmlFor="key">Clave</Label>
          <Input id="key" name="key" required maxLength={100} placeholder="p. ej. asset.decommission" />
        </div>
        <div className="flex flex-col gap-1">
          <Label htmlFor="companyId">Alcance</Label>
          <select id="companyId" name="companyId" defaultValue="" className={selectClassName}>
            <option value="">Todas las empresas</option>
            {companies.map((c) => (
              <option key={c.companyId} value={c.companyId}>
                {c.tradeName}
              </option>
            ))}
          </select>
        </div>
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div className="flex flex-col gap-1">
          <Label htmlFor="mode">Modo</Label>
          <select
            id="mode"
            name="mode"
            value={mode}
            onChange={(e) => setMode(e.target.value as ApprovalMode)}
            className={selectClassName}
          >
            <option value="Parallel">Paralelo (cualquier orden)</option>
            <option value="Sequential">Secuencial (en el orden de la lista)</option>
          </select>
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
            <select
              name="approverRoleIds"
              value={roleId}
              onChange={(e) => updateSlot(index, e.target.value)}
              className={`${selectClassName} flex-1`}
              required
            >
              {roles.map((role) => (
                <option key={role.id} value={role.id}>
                  {role.name}
                </option>
              ))}
            </select>
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

      {state.error && <p className="text-destructive text-sm">{state.error}</p>}

      <div className="flex justify-end">
        <Button type="submit" disabled={pending}>
          {pending ? "Guardando…" : "Crear flujo"}
        </Button>
      </div>
    </form>
  );
}

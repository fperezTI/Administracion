"use client";

import { useActionState, useState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { SignatureMechanismFields } from "@/components/signature-mechanism-fields";
import { approveAction, rejectAction, type DecideActionState } from "./actions";

const initialState: DecideActionState = { error: null, success: false };

export function DecideApprovalForm({ approvalInstanceId }: { approvalInstanceId: string }) {
  const [mode, setMode] = useState<"idle" | "approve" | "reject">("idle");
  const boundApprove = approveAction.bind(null, approvalInstanceId);
  const boundReject = rejectAction.bind(null, approvalInstanceId);
  const [approveState, approveFormAction, approvePending] = useActionState(boundApprove, initialState);
  const [rejectState, rejectFormAction, rejectPending] = useActionState(boundReject, initialState);

  if (approveState.success || rejectState.success) {
    return <p className="text-success text-sm font-medium">Decisión registrada.</p>;
  }

  if (mode === "idle") {
    return (
      <div className="flex gap-2">
        <Button size="sm" onClick={() => setMode("approve")}>
          Aprobar
        </Button>
        <Button size="sm" variant="outline" onClick={() => setMode("reject")}>
          Rechazar
        </Button>
      </div>
    );
  }

  if (mode === "approve") {
    return (
      <form action={approveFormAction} className="flex flex-col gap-3 rounded-lg border p-3">
        <SignatureMechanismFields />
        <div className="flex items-center justify-between">
          <p className="text-sm">{approveState.error && <span className="text-destructive">{approveState.error}</span>}</p>
          <div className="flex gap-2">
            <Button type="button" variant="outline" size="sm" onClick={() => setMode("idle")}>
              Cancelar
            </Button>
            <Button type="submit" size="sm" disabled={approvePending}>
              {approvePending ? "Enviando…" : "Confirmar aprobación"}
            </Button>
          </div>
        </div>
      </form>
    );
  }

  return (
    <form action={rejectFormAction} className="flex flex-col gap-3 rounded-lg border p-3">
      <div className="flex flex-col gap-1">
        <Label htmlFor={`comment-${approvalInstanceId}`}>Motivo del rechazo</Label>
        <Input id={`comment-${approvalInstanceId}`} name="comment" required maxLength={1000} />
      </div>
      <SignatureMechanismFields />
      <div className="flex items-center justify-between">
        <p className="text-sm">{rejectState.error && <span className="text-destructive">{rejectState.error}</span>}</p>
        <div className="flex gap-2">
          <Button type="button" variant="outline" size="sm" onClick={() => setMode("idle")}>
            Cancelar
          </Button>
          <Button type="submit" variant="destructive" size="sm" disabled={rejectPending}>
            {rejectPending ? "Enviando…" : "Confirmar rechazo"}
          </Button>
        </div>
      </div>
    </form>
  );
}

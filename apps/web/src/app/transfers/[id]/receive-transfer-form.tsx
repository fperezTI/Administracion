"use client";

import { useActionState } from "react";
import { Button } from "@/components/ui/button";
import { SignatureMechanismFields } from "@/components/signature-mechanism-fields";
import { receiveTransferAction, type TransferActionState } from "./actions";

const initialState: TransferActionState = { error: null, success: false };

export function ReceiveTransferForm({ transferId }: { transferId: string }) {
  const boundAction = receiveTransferAction.bind(null, transferId);
  const [state, formAction, pending] = useActionState(boundAction, initialState);

  if (state.success) {
    return <p className="text-success text-sm font-medium">Transferencia recibida.</p>;
  }

  return (
    <form action={formAction} className="flex flex-col gap-3 rounded-lg border p-4">
      <p className="text-sm font-medium">Recibir en esta empresa</p>
      <SignatureMechanismFields />
      <div className="flex items-center justify-between">
        <p className="text-sm">{state.error && <span className="text-destructive">{state.error}</span>}</p>
        <Button type="submit" disabled={pending}>
          {pending ? "Confirmando…" : "Confirmar recepción"}
        </Button>
      </div>
    </form>
  );
}

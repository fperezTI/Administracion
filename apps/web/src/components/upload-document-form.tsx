"use client";

import { useActionState } from "react";
import { Button } from "@/components/ui/button";
import { uploadDocumentAction, type UploadDocumentActionState } from "./documents-panel-actions";

const initialState: UploadDocumentActionState = { error: null };

export function UploadDocumentForm({
  entityType,
  entityId,
  revalidatePathTarget,
}: {
  entityType: string;
  entityId: string;
  revalidatePathTarget: string;
}) {
  const boundAction = uploadDocumentAction.bind(null, entityType, entityId, revalidatePathTarget);
  const [state, formAction, pending] = useActionState(boundAction, initialState);

  return (
    <form action={formAction} className="flex items-end gap-2">
      <input
        type="file"
        name="file"
        required
        className="border-input bg-transparent file:bg-secondary file:text-secondary-foreground flex-1 rounded-lg border text-sm outline-none file:mr-3 file:h-8 file:cursor-pointer file:rounded-l-lg file:border-0 file:px-3"
      />
      <Button type="submit" size="sm" disabled={pending}>
        {pending ? "Cargando…" : "Cargar"}
      </Button>
      {state.error && <p className="text-destructive text-xs">{state.error}</p>}
    </form>
  );
}

"use client";

import { useActionState } from "react";
import { Button } from "@/components/ui/button";
import { IMPORT_COMMIT_MODE_LABELS } from "@/lib/import-export-labels";
import { commitImportBatchAction, type CommitImportBatchActionState } from "./actions";

const initialState: CommitImportBatchActionState = { error: null };

export function CommitImportBatchForm({ importBatchId, hasInvalidRows }: { importBatchId: string; hasInvalidRows: boolean }) {
  const boundAction = commitImportBatchAction.bind(null, importBatchId);
  const [state, formAction, pending] = useActionState(boundAction, initialState);

  return (
    <form action={formAction} className="flex flex-col gap-3">
      <fieldset className="flex flex-col gap-2">
        <label className="flex items-center gap-2 text-sm">
          <input type="radio" name="mode" value="ValidRowsOnly" defaultChecked className="size-4" />
          {IMPORT_COMMIT_MODE_LABELS.ValidRowsOnly}
          <span className="text-muted-foreground text-xs">— crea las filas válidas, omite las inválidas.</span>
        </label>
        <label className="flex items-center gap-2 text-sm">
          <input type="radio" name="mode" value="AllOrNothing" disabled={hasInvalidRows} className="size-4" />
          {IMPORT_COMMIT_MODE_LABELS.AllOrNothing}
          <span className="text-muted-foreground text-xs">
            — crea todo o nada.{hasInvalidRows && " No disponible: el lote tiene filas inválidas."}
          </span>
        </label>
      </fieldset>
      {state.error && <p className="text-destructive text-sm" role="alert">{state.error}</p>}
      <Button type="submit" disabled={pending}>
        {pending ? "Confirmando…" : "Confirmar importación"}
      </Button>
    </form>
  );
}

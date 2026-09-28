"use client";

import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import { ArrowLeft } from "lucide-react";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Button, buttonVariants } from "@/components/ui/button";

type DirtyContextValue = {
  setSectionDirty: (section: string, dirty: boolean) => void;
  isDirty: boolean;
};

const DirtyContext = createContext<DirtyContextValue | null>(null);

/** Tracks whether any of the edit page's independent per-section forms (General/Financiera/
 * Garantía-Seguro/Mantenimiento) has unsaved changes, so "Volver al detalle" can warn before
 * discarding them — each form only has its own `useActionState`, with no shared state otherwise. */
export function UnsavedChangesProvider({ children }: { children: ReactNode }) {
  const [dirtySections, setDirtySections] = useState<Record<string, boolean>>({});

  const setSectionDirty = (section: string, dirty: boolean) => {
    setDirtySections((prev) => (prev[section] === dirty ? prev : { ...prev, [section]: dirty }));
  };

  const isDirty = useMemo(() => Object.values(dirtySections).some(Boolean), [dirtySections]);

  return <DirtyContext.Provider value={{ setSectionDirty, isDirty }}>{children}</DirtyContext.Provider>;
}

function useDirtyContext(): DirtyContextValue {
  const ctx = useContext(DirtyContext);
  if (!ctx) {
    throw new Error("This component must be used inside <UnsavedChangesProvider>.");
  }
  return ctx;
}

/** Returns an onChange handler to attach to a form: marks `section` dirty on any field change, and
 * clears it once that same section's own save action reports success. */
export function useSectionDirtyTracking(section: string, saveSucceeded: boolean) {
  const { setSectionDirty } = useDirtyContext();

  useEffect(() => {
    if (saveSucceeded) {
      setSectionDirty(section, false);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [saveSucceeded, section]);

  return () => setSectionDirty(section, true);
}

/** Plain <a> instead of next/link's <Link> — the Button+Link composition here was observed to
 * start the RSC navigation request but never actually commit it (client stayed on /edit), matching
 * the same class of Next.js App Router soft-navigation bug already worked around elsewhere in this
 * app (TablePagination/SortableTableHead — see deploy 7 in memory). A plain anchor forces a real
 * navigation instead. */
export function BackToDetailLink({ href }: { href: string }) {
  const { isDirty } = useDirtyContext();
  const [confirmOpen, setConfirmOpen] = useState(false);

  return (
    <>
      <a
        href={href}
        onClick={(event) => {
          if (isDirty) {
            event.preventDefault();
            setConfirmOpen(true);
          }
        }}
        className={buttonVariants({ variant: "outline" })}
      >
        <ArrowLeft data-icon="inline-start" />
        Volver al detalle
      </a>

      <Dialog open={confirmOpen} onOpenChange={setConfirmOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>¿Descartar cambios sin guardar?</DialogTitle>
            <DialogDescription>
              Hiciste cambios en este formulario que todavía no se han guardado. Si regresas ahora, se
              perderán.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => setConfirmOpen(false)}>
              Seguir editando
            </Button>
            <Button type="button" variant="destructive" onClick={() => { window.location.href = href; }}>
              Descartar y salir
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}

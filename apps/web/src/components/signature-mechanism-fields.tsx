"use client";

import { useState } from "react";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { SignatureCanvas } from "@/components/signature-canvas";

/** Shared between every place that lets someone sign a decision (approve/reject an approval) — F4's two
 * mechanisms, see ADR 0006. Native radios so the choice travels with the rest of the form on submit,
 * no client state needed beyond which field set to render. */
export function SignatureMechanismFields() {
  const [mechanism, setMechanism] = useState<"TypedConfirmation" | "DrawnSignature">("TypedConfirmation");

  return (
    <div className="flex flex-col gap-2">
      <div className="flex gap-4 text-sm">
        <label className="flex items-center gap-1.5">
          <input
            type="radio"
            name="signatureMechanism"
            value="TypedConfirmation"
            checked={mechanism === "TypedConfirmation"}
            onChange={() => setMechanism("TypedConfirmation")}
          />
          Escribir mi nombre
        </label>
        <label className="flex items-center gap-1.5">
          <input
            type="radio"
            name="signatureMechanism"
            value="DrawnSignature"
            checked={mechanism === "DrawnSignature"}
            onChange={() => setMechanism("DrawnSignature")}
          />
          Dibujar firma
        </label>
      </div>
      {mechanism === "TypedConfirmation" ? (
        <div className="flex flex-col gap-1">
          <Label htmlFor="typedFullName">Nombre completo</Label>
          <Input id="typedFullName" name="typedFullName" maxLength={200} required />
        </div>
      ) : (
        <SignatureCanvas name="signatureImageDataUrl" />
      )}
    </div>
  );
}

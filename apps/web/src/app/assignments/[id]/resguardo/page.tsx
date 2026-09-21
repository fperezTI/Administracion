import Link from "next/link";
import { notFound } from "next/navigation";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, getAssignmentById, getCompanyById } from "@/lib/api";
import { Button } from "@/components/ui/button";
import { PrintButton } from "@/components/print-button";

export default async function AssignmentReceiptPage({ params }: { params: Promise<{ id: string }> }) {
  const accessToken = await requireAccessToken();
  const { id } = await params;

  let assignment;
  try {
    assignment = await getAssignmentById(accessToken, id);
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) {
      notFound();
    }
    throw error;
  }

  const company = await getCompanyById(accessToken, assignment.companyId).catch(() => null);

  return (
    <div className="mx-auto flex max-w-2xl flex-col gap-6 p-8 print:p-0">
      <div className="flex items-center justify-between print:hidden">
        <Button variant="outline" render={<Link href={`/assignments/${id}`} />}>
          ← Volver a la asignación
        </Button>
        <PrintButton />
      </div>

      <div className="flex flex-col gap-6 rounded-lg border p-8 text-sm print:border-none print:p-0">
        <div className="flex items-start justify-between border-b pb-4">
          <div>
            <p className="text-base font-semibold">{company?.tradeName ?? "Empresa"}</p>
            <p className="text-muted-foreground text-xs">{company?.legalName}</p>
          </div>
          <div className="text-right">
            <p className="font-semibold uppercase tracking-wide">Resguardo de activo</p>
            <p className="text-muted-foreground font-mono text-xs">{assignment.movementFolio}</p>
          </div>
        </div>

        {assignment.groupMembers.length > 1 ? (
          <section>
            <h3 className="mb-2 text-xs font-semibold tracking-wide uppercase">Activos incluidos en este resguardo</h3>
            <table className="w-full text-left text-xs">
              <thead>
                <tr className="border-b">
                  <th className="py-1 pr-2 font-semibold">Folio</th>
                  <th className="py-1 pr-2 font-semibold">Marca / modelo</th>
                  <th className="py-1 pr-2 font-semibold">Serie</th>
                  <th className="py-1 font-semibold">Tipo</th>
                </tr>
              </thead>
              <tbody>
                {assignment.groupMembers.map((member) => (
                  <tr key={member.assetId} className="border-b last:border-0">
                    <td className="py-1 pr-2 font-mono">{member.assetFolio}</td>
                    <td className="py-1 pr-2">
                      {member.brand} {member.model}
                    </td>
                    <td className="py-1 pr-2 font-mono">{member.serialNumber ?? "—"}</td>
                    <td className="py-1">{member.isPrimary ? "Principal" : "Accesorio"}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </section>
        ) : (
          <section className="grid grid-cols-2 gap-x-6 gap-y-2">
            <h3 className="col-span-2 text-xs font-semibold tracking-wide uppercase">Datos del activo</h3>
            <Field label="Folio interno" value={assignment.assetFolio} mono />
            <Field label="Folio patrimonial" value={assignment.assetPatrimonialFolio} />
            <Field label="Marca / modelo" value={`${assignment.assetBrand} ${assignment.assetModel}`} />
            <Field label="Número de serie" value={assignment.assetSerialNumber} mono />
            <Field label="Descripción" value={assignment.assetDescription} />
          </section>
        )}

        <section className="grid grid-cols-2 gap-x-6 gap-y-2">
          <h3 className="col-span-2 text-xs font-semibold tracking-wide uppercase">Datos del resguardante</h3>
          <Field label="Nombre" value={assignment.assignedToDisplayName} />
          <Field label="Correo" value={assignment.assignedToEmail} />
          <Field label="Unidad organizacional" value={assignment.orgUnitName} />
          <Field label="Fecha de entrega" value={new Date(assignment.assignedAtUtc).toLocaleDateString("es-MX")} />
        </section>

        <section>
          <h3 className="mb-1 text-xs font-semibold tracking-wide uppercase">Confirmación digital</h3>
          {assignment.acceptanceSignature ? (
            <p className="text-muted-foreground text-xs">
              Recepción confirmada digitalmente por {assignment.acceptanceSignature.signerDisplayName} el{" "}
              {new Date(assignment.acceptanceSignature.signedAtUtc).toLocaleString("es-MX")}.
            </p>
          ) : (
            <p className="text-muted-foreground text-xs">
              Recepción todavía pendiente de confirmación digital en el sistema.
            </p>
          )}
        </section>

        <section className="text-muted-foreground text-xs leading-relaxed">
          <h3 className="text-foreground mb-1 text-xs font-semibold tracking-wide uppercase">
            Términos de resguardo
          </h3>
          <p>
            El resguardante declara recibir el activo descrito en buen estado y se compromete a darle un uso
            adecuado, cuidarlo y protegerlo de daño, pérdida o robo, así como a reportar de inmediato al área
            correspondiente cualquier falla, daño o incidente. El activo es propiedad de {company?.legalName ??
              "la empresa"} y deberá devolverse cuando se le solicite o al término de la relación laboral, en las
            mismas condiciones en que fue recibido, salvo el desgaste normal por su uso.
          </p>
        </section>

        <section className="grid grid-cols-2 gap-6 pt-8">
          <SignatureLine label="Firma del resguardante" name={assignment.assignedToDisplayName} />
          <SignatureLine label="Firma de quien entrega" />
        </section>
      </div>
    </div>
  );
}

function Field({ label, value, mono }: { label: string; value: string | null | undefined; mono?: boolean }) {
  return (
    <div>
      <p className="text-muted-foreground text-xs">{label}</p>
      <p className={mono ? "font-mono" : undefined}>{value ?? "—"}</p>
    </div>
  );
}

function SignatureLine({ label, name }: { label: string; name?: string }) {
  return (
    <div className="flex flex-col items-center gap-1">
      <div className="h-12 w-full border-b" />
      <p className="text-xs font-medium">{name ?? " "}</p>
      <p className="text-muted-foreground text-xs">{label}</p>
    </div>
  );
}

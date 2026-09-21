import Link from "next/link";
import { notFound } from "next/navigation";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, getAssetById, getAssignments, getOrgUnitTree, getUsers } from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { Button } from "@/components/ui/button";
import { ReassignAssetForm } from "./reassign-asset-form";

export default async function ReassignAssetPage({ params }: { params: Promise<{ id: string }> }) {
  const accessToken = await requireAccessToken();
  const { id } = await params;

  let asset;
  try {
    asset = await getAssetById(accessToken, id);
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) {
      notFound();
    }
    throw error;
  }

  const activeAssignment = await getAssignments(accessToken, {
    companyId: asset.companyId,
    assetId: id,
    status: "Accepted",
    pageSize: 1,
  })
    .then((result) => result.items[0] ?? null)
    .catch(() => null);

  if (!activeAssignment) {
    notFound();
  }

  const [users, orgUnits] = await Promise.all([
    getUsers(accessToken, { pageSize: 200 }),
    getOrgUnitTree(accessToken, asset.companyId),
  ]);

  const candidateUsers = users.items.filter((u) => u.id !== activeAssignment.assignedToUserId);
  const accessoryOptions = asset.accessories.filter((a) => a.status === "Assigned" || a.status === "InWarehouse");

  return (
    <>
      <AppHeader title="Reasignar activo" subtitle={`${asset.internalFolio} — ${asset.brand} ${asset.model}`} />
    <div className="mx-auto max-w-xl px-8 pb-8">
      <div className="mb-4 flex justify-end">
        <Button variant="outline" render={<Link href={`/assets/${id}`} />}>
          ← Volver
        </Button>
      </div>
      <p className="text-muted-foreground mb-4 text-sm">
        Actualmente asignado a <span className="font-medium">{activeAssignment.assignedToDisplayName}</span>. Al
        reasignar, la asignación actual se cierra y se crea una nueva pendiente de firma para el nuevo
        destinatario, en un solo paso.
      </p>
      <ReassignAssetForm assetId={id} users={candidateUsers} orgUnits={orgUnits} accessories={accessoryOptions} />
    </div>
    </>
  );
}

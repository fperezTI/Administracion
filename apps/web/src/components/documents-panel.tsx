import { ApiError, getDocumentDownloadUrl, getDocuments } from "@/lib/api";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { UploadDocumentForm } from "./upload-document-form";

function formatSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

/** Reusable across every allowlisted entity type (see DocumentEntityTypes in the backend) — currently
 * mounted on asset and maintenance order detail pages. */
export async function DocumentsPanel({
  accessToken,
  entityType,
  entityId,
  revalidatePathTarget,
}: {
  accessToken: string;
  entityType: string;
  entityId: string;
  revalidatePathTarget: string;
}) {
  let content: React.ReactNode;
  try {
    const documents = await getDocuments(accessToken, entityType, entityId);
    content = (
      <ul className="flex flex-col gap-2">
        {documents.length === 0 ? (
          <li className="text-muted-foreground text-sm">Sin documentos todavía.</li>
        ) : (
          documents.map((d) => (
            <li key={d.id} className="flex items-center justify-between gap-2 text-sm">
              <a href={getDocumentDownloadUrl(d.id)} className="hover:text-primary truncate hover:underline" download>
                {d.fileName}
              </a>
              <span className="text-muted-foreground shrink-0 text-xs">
                {formatSize(d.sizeBytes)} — {d.uploadedByDisplayName}
              </span>
            </li>
          ))
        )}
      </ul>
    );
  } catch (error) {
    const status = error instanceof ApiError ? error.status : undefined;
    content = (
      <p className="text-destructive text-sm">
        {status === 403 ? "No tienes permiso para consultar documentos (Documents.Read)." : "No fue posible consultar los documentos."}
      </p>
    );
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-sm">Documentos</CardTitle>
      </CardHeader>
      <CardContent className="flex flex-col gap-3">
        {content}
        <UploadDocumentForm entityType={entityType} entityId={entityId} revalidatePathTarget={revalidatePathTarget} />
      </CardContent>
    </Card>
  );
}

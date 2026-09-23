import Link from "next/link";
import { notFound } from "next/navigation";
import { revalidatePath } from "next/cache";
import { ArrowLeft, Printer } from "lucide-react";
import QRCode from "qrcode";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, getAssetById, getAssetCategoryById, getCompanyById, reprintAssetTag } from "@/lib/api";
import { Button } from "@/components/ui/button";
import { PrintButton } from "@/components/print-button";
import { IDENTIFICATION_TECHNOLOGY_LABELS } from "@/lib/asset-labels";

export default async function AssetLabelPage({ params }: { params: Promise<{ id: string }> }) {
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

  if (!asset.tag) {
    notFound();
  }

  const [category, company] = await Promise.all([
    getAssetCategoryById(accessToken, asset.assetCategoryId).catch(() => null),
    getCompanyById(accessToken, asset.companyId).catch(() => null),
  ]);

  // QR is the only technology this preview renders as a scannable image — the printable QR/text label
  // matches pedido §12's default; Barcode/NFC/RFID require specialized label-printer/writer hardware to
  // physically encode, which is out of scope for a browser-rendered preview (documented simplification).
  const showsQr = asset.tag.technology === "Qr" || asset.tag.technology === "QrAndBarcode";
  const qrDataUrl = showsQr ? await QRCode.toDataURL(asset.tag.code, { margin: 1, width: 240 }) : null;

  async function reprintTag() {
    "use server";
    const token = await requireAccessToken();
    await reprintAssetTag(token, id);
    revalidatePath(`/assets/${id}/label`);
  }

  return (
    <div className="mx-auto flex max-w-md flex-col gap-4 p-8 print:p-0">
      <div className="flex items-center justify-between print:hidden">
        <Button variant="outline" render={<Link href={`/assets/${id}`} />}>
          <ArrowLeft data-icon="inline-start" />
          Volver al activo
        </Button>
        <div className="flex gap-2">
          <form action={reprintTag}>
            <Button type="submit" variant="outline">
              <Printer data-icon="inline-start" />
              Reimprimir (+1)
            </Button>
          </form>
          <PrintButton />
        </div>
      </div>

      <div className="flex flex-col items-center gap-2 rounded-lg border-2 border-dashed p-6 text-center print:border-solid">
        <p className="text-xs font-semibold tracking-wide uppercase">{company?.tradeName ?? "Empresa"}</p>
        <p className="text-muted-foreground text-xs">{category?.name ?? "Categoría"}</p>

        {qrDataUrl ? (
          // eslint-disable-next-line @next/next/no-img-element -- data: URL generated server-side, not an optimizable remote image
          <img src={qrDataUrl} alt={`Código QR ${asset.tag.code}`} width={180} height={180} className="my-2" />
        ) : (
          <p className="text-muted-foreground my-4 text-xs">
            Tecnología {IDENTIFICATION_TECHNOLOGY_LABELS[asset.tag.technology]} — se codifica con el equipo de
            impresión/grabado correspondiente.
          </p>
        )}

        <p className="text-lg font-bold tracking-wide">{asset.internalFolio}</p>
        <p className="text-muted-foreground font-mono text-xs">{asset.tag.code}</p>
      </div>

      <p className="text-muted-foreground text-center text-xs print:hidden">
        Impresa {asset.tag.printCount} {asset.tag.printCount === 1 ? "vez" : "veces"}. Reimprimir no cambia la
        identidad del activo.
      </p>
    </div>
  );
}

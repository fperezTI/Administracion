import Link from "next/link";
import { notFound } from "next/navigation";
import { requireAccessToken } from "@/lib/require-session";
import { ApiError, getLoanById } from "@/lib/api";
import { AppHeader } from "@/components/app-header";
import { Badge } from "@/components/ui/badge";
import { LOAN_STATUS_LABELS, loanStatusBadgeVariant } from "@/lib/inventory-labels";
import { ReturnLoanForm } from "./return-loan-form";

export default async function LoanDetailPage({ params }: { params: Promise<{ id: string }> }) {
  const accessToken = await requireAccessToken();
  const { id } = await params;

  let loan;
  try {
    loan = await getLoanById(accessToken, id);
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) {
      notFound();
    }
    throw error;
  }

  return (
    <div className="mx-auto flex max-w-2xl flex-col gap-4 p-8">
      <AppHeader title="Préstamo" />
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <Link href={`/assets/${loan.assetId}`} className="hover:text-primary font-mono text-lg font-semibold hover:underline">
            {loan.assetFolio}
          </Link>
          <p className="text-muted-foreground text-sm">Prestado a {loan.borrowerDisplayName}</p>
        </div>
        <Badge variant={loanStatusBadgeVariant(loan.status)}>{LOAN_STATUS_LABELS[loan.status]}</Badge>
      </div>

      <div className="grid grid-cols-2 gap-4 rounded-lg border p-4 text-sm">
        <div>
          <p className="text-muted-foreground text-xs">Fecha de préstamo</p>
          <p>{new Date(loan.loanedAtUtc).toLocaleDateString("es-MX")}</p>
        </div>
        <div>
          <p className="text-muted-foreground text-xs">Devolución esperada</p>
          <p>{loan.expectedReturnDate}</p>
        </div>
        {loan.returnedAtUtc && (
          <div>
            <p className="text-muted-foreground text-xs">Devuelto</p>
            <p>{new Date(loan.returnedAtUtc).toLocaleDateString("es-MX")}</p>
          </div>
        )}
      </div>

      {loan.status === "Active" && <ReturnLoanForm loanId={loan.id} />}
    </div>
  );
}

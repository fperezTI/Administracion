import { Loader2 } from "lucide-react";
import { AppHeader } from "@/components/app-header";

/** Se muestra automáticamente durante cualquier navegación dentro de /assets hecha vía el router
 * de Next (filtros, paginación) — reemplaza el único indicador que había antes (el ícono de carga
 * del navegador en un <form method="GET">, fácil de no ver cuando la tabla es larga). */
export default function AssetsLoading() {
  return (
    <>
      <AppHeader title="Activos" subtitle="Inventario de activos de TI por empresa." />
      <div className="text-muted-foreground mx-auto flex max-w-6xl flex-col items-center justify-center gap-3 px-8 py-24">
        <Loader2 className="size-6 animate-spin stroke-[1.5]" />
        <p className="text-sm">Cargando activos…</p>
      </div>
    </>
  );
}

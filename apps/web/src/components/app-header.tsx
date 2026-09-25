/** Encabezado de contenido de página: título, subtítulo y controles específicos de esa página
 * (p. ej. el selector de empresa). El chrome global de la aplicación (buscador, tema,
 * notificaciones, menú de usuario, logout, navegación) ya no vive aquí — se centralizó una sola
 * vez en el App Shell (ver components/layout/app-shell.tsx y topbar.tsx), montado desde el root
 * layout para todas las rutas que lo necesitan. Las páginas siguen invocando este componente sin
 * cambios en sus props. */
export function AppHeader({
  title,
  subtitle,
  activeCompany,
}: {
  title: string;
  subtitle?: string;
  /** Selector de empresa activa u otro control que deba quedar junto al título — nunca escondido
   * más abajo en la página: en un sistema multiempresa, equivocar la empresa activa es un riesgo
   * funcional, no solo un detalle visual. */
  activeCompany?: React.ReactNode;
}) {
  return (
    <div className="mb-6 flex flex-wrap items-center justify-between gap-3 border-b px-8 pt-8 pb-3">
      <div>
        <h1 className="text-lg leading-tight font-semibold tracking-tight">{title}</h1>
        {subtitle && <p className="text-muted-foreground text-sm">{subtitle}</p>}
      </div>
      {activeCompany}
    </div>
  );
}

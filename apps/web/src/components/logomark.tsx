import { Boxes } from "lucide-react";
import { cn } from "cn";

const MARK_SIZES = {
  sm: { box: "size-8", icon: "size-4", text: "text-base" },
  lg: { box: "size-12", icon: "size-6", text: "text-xl" },
} as const;

/** Marca de AssetHub: usada en el sidebar y el header móvil (compacta) y en la pantalla de login
 * (grande, como hero) — un solo componente para no repetir el mismo mark en varios archivos como
 * pasaba con el "AT". */
export function Logomark({
  withWordmark = false,
  size = "sm",
  inverse = false,
  className,
}: {
  withWordmark?: boolean;
  size?: keyof typeof MARK_SIZES;
  /** Para usar sobre --primary (p. ej. el panel de héroe del login): invierte los colores del
   * cuadro del ícono para que no se funda con el fondo, y fuerza el wordmark a --primary-foreground. */
  inverse?: boolean;
  className?: string;
}) {
  const s = MARK_SIZES[size];
  return (
    <span className={cn("flex items-center gap-2", className)}>
      <span
        aria-hidden
        className={cn(
          "flex shrink-0 items-center justify-center rounded-md",
          inverse
            ? "bg-surface-brand-foreground text-surface-brand"
            : "bg-primary text-primary-foreground",
          s.box
        )}
      >
        <Boxes className={cn(s.icon, "stroke-[1.5]")} />
      </span>
      {withWordmark && (
        <span
          className={cn(
            "font-heading font-semibold tracking-tight",
            s.text,
            inverse && "text-surface-brand-foreground"
          )}
        >
          AssetHub
        </span>
      )}
    </span>
  );
}

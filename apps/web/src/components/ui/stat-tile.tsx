import * as React from "react"
import { cn } from "cn"

function StatTile({
  label,
  value,
  tone = "neutral",
  className,
}: {
  label: string
  value: React.ReactNode
  tone?: "neutral" | "success" | "warning" | "destructive"
  className?: string
}) {
  return (
    <div
      data-slot="stat-tile"
      className={cn("flex flex-col gap-1 rounded-lg p-4 ring-1 ring-foreground/10", className)}
    >
      <p className="text-muted-foreground text-xs font-medium tracking-wide uppercase">{label}</p>
      <p
        className={cn(
          "text-2xl font-semibold tracking-tight",
          tone === "success" && "text-success",
          tone === "warning" && "text-warning",
          tone === "destructive" && "text-destructive"
        )}
      >
        {value}
      </p>
    </div>
  )
}

export { StatTile }

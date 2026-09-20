import * as React from "react"
import { cn } from "cn"

function PageTitle({ className, ...props }: React.ComponentProps<"h1">) {
  return (
    <h1
      data-slot="page-title"
      className={cn(
        "font-heading text-xl leading-tight font-semibold tracking-tight",
        className
      )}
      {...props}
    />
  )
}

function PageSubtitle({ className, ...props }: React.ComponentProps<"p">) {
  return (
    <p
      data-slot="page-subtitle"
      className={cn("text-sm text-muted-foreground", className)}
      {...props}
    />
  )
}

function SectionTitle({ className, ...props }: React.ComponentProps<"h2">) {
  return (
    <h2
      data-slot="section-title"
      className={cn("font-heading text-base leading-snug font-medium", className)}
      {...props}
    />
  )
}

export { PageTitle, PageSubtitle, SectionTitle }

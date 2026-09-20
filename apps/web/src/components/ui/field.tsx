import * as React from "react"
import { Label } from "@/components/ui/label"
import { cn } from "cn"

function Field({
  label,
  htmlFor,
  optional,
  className,
  children,
}: {
  label: string
  htmlFor: string
  optional?: boolean
  className?: string
  children: React.ReactNode
}) {
  return (
    <div data-slot="field" className={cn("flex flex-col gap-1", className)}>
      <Label htmlFor={htmlFor}>
        {label} {optional && <span className="text-muted-foreground font-normal">(opcional)</span>}
      </Label>
      {children}
    </div>
  )
}

export { Field }

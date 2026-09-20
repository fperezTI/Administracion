import * as React from "react"
import { cn } from "cn"

const PAGE_CONTAINER_WIDTH = {
  list: "max-w-6xl",
  detail: "max-w-3xl",
  form: "max-w-xl",
} as const

function PageContainer({
  className,
  variant = "detail",
  ...props
}: React.ComponentProps<"div"> & {
  variant?: keyof typeof PAGE_CONTAINER_WIDTH
}) {
  return (
    <div
      data-slot="page-container"
      className={cn(
        "mx-auto w-full px-4 py-6",
        PAGE_CONTAINER_WIDTH[variant],
        className
      )}
      {...props}
    />
  )
}

export { PageContainer }

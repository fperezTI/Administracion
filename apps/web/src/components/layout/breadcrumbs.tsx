import Link from "next/link";
import { ChevronRight } from "lucide-react";
import { cn } from "cn";

export type BreadcrumbItem = {
  label: string;
  href?: string;
};

function Breadcrumbs({
  items,
  className,
}: {
  items: BreadcrumbItem[];
  className?: string;
}) {
  return (
    <nav
      aria-label="Breadcrumb"
      className={cn("text-muted-foreground flex items-center text-sm", className)}
    >
      <ol className="flex items-center gap-1.5">
        {items.map((item, index) => {
          const isLast = index === items.length - 1;
          return (
            <li key={`${item.label}-${index}`} className="flex items-center gap-1.5">
              {index > 0 && <ChevronRight aria-hidden className="size-3.5 stroke-[1.5]" />}
              {item.href && !isLast ? (
                <Link
                  href={item.href}
                  className="hover:text-foreground underline-offset-4 hover:underline"
                >
                  {item.label}
                </Link>
              ) : (
                <span
                  aria-current={isLast ? "page" : undefined}
                  className={cn(isLast && "text-foreground font-medium")}
                >
                  {item.label}
                </span>
              )}
            </li>
          );
        })}
      </ol>
    </nav>
  );
}

export { Breadcrumbs };

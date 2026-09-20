import type { MetadataRoute } from "next";

// Placeholder branding (name, colors, icon) — swap once real company branding/logo is provided
// (see docs/architecture/00-analysis.md §18, "Información externa realmente necesaria").
export default function manifest(): MetadataRoute.Manifest {
  return {
    name: "Gestión de Activos de TI e Infraestructura",
    short_name: "Activos TI",
    description: "Administración operativa de activos de TI e infraestructura multiempresa.",
    start_url: "/",
    display: "standalone",
    background_color: "#12151a",
    theme_color: "#1e4fa3",
    lang: "es",
    icons: [
      {
        src: "/icons/icon.svg",
        sizes: "any",
        type: "image/svg+xml",
        purpose: "any",
      },
    ],
  };
}

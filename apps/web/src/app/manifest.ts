import type { MetadataRoute } from "next";

// Icon matches the app's own logomark (components/logomark.tsx): Boxes en caja navy, el mismo diseño
// que el favicon (app/icon.svg) — colores fijos (no siguen el tema activo, a diferencia del logomark en
// la UI) porque el ícono del PWA/favicon no puede reaccionar a la preferencia de tema del usuario.
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

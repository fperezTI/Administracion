export type NavLink = {
  href: string;
  label: string;
};

export type NavGroup = {
  label: string;
  links: NavLink[];
};

/** Agrupación visual de NAV_LINKS por familia de negocio (ver módulos en CLAUDE.md) — puramente
 * de presentación, no cambia rutas ni permisos: cada link sigue apuntando a la misma ruta que antes. */
export const NAV_GROUPS: NavGroup[] = [
  {
    label: "Activos",
    links: [
      { href: "/assets", label: "Activos" },
      { href: "/asset-categories", label: "Categorías" },
      { href: "/warranties", label: "Garantías" },
    ],
  },
  {
    label: "Inventario y movimientos",
    links: [
      { href: "/assignments", label: "Asignaciones" },
      { href: "/loans", label: "Préstamos" },
      { href: "/movements", label: "Movimientos" },
      { href: "/transfers", label: "Transferencias" },
      { href: "/spare-parts", label: "Refacciones" },
      { href: "/consumables", label: "Consumibles" },
    ],
  },
  {
    label: "Mantenimiento",
    links: [
      { href: "/maintenance-orders", label: "Mantenimiento" },
      { href: "/maintenance-checklists", label: "Checklists" },
    ],
  },
  {
    label: "Solicitudes y aprobaciones",
    links: [
      { href: "/requests", label: "Solicitudes" },
      { href: "/approval-flows", label: "Flujos de aprobación" },
    ],
  },
  {
    label: "Datos",
    links: [
      { href: "/imports", label: "Importaciones" },
      { href: "/reports", label: "Reportes" },
      { href: "/templates", label: "Plantillas" },
    ],
  },
  {
    label: "Administración",
    links: [
      { href: "/companies", label: "Empresas" },
      { href: "/org-units", label: "Estructura" },
      { href: "/roles", label: "Roles" },
      { href: "/users", label: "Usuarios" },
      { href: "/audit", label: "Auditoría" },
    ],
  },
];

/** Cada usuario autenticado, sin importar sus permisos RBAC — son de autoservicio, no acciones
 * administrativas, así que nunca están condicionadas por un permiso como el resto de NAV_GROUPS. */
export const SELF_SERVICE_LINKS: NavLink[] = [
  { href: "/my-assignments", label: "Mis asignaciones" },
  { href: "/my-requests", label: "Mis solicitudes" },
  { href: "/my-approvals", label: "Mis aprobaciones" },
  { href: "/notifications", label: "Mis notificaciones" },
];

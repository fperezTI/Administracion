/** F11: the 8 searchable entity types (see ADR 0013). `LinkEntityType` is always one of these keys —
 * `Movement` never appears here since it always links to its `Asset` instead of itself. */
export const SEARCH_ENTITY_TYPE_LABELS: Record<string, string> = {
  Asset: "Activos",
  Movement: "Movimientos",
  MaintenanceOrder: "Órdenes de mantenimiento",
  Warranty: "Garantías",
  SparePart: "Refacciones",
  Consumable: "Consumibles",
  InternalRequest: "Solicitudes internas",
  User: "Usuarios",
};

const ROUTE_BY_LINK_ENTITY_TYPE: Record<string, string> = {
  Asset: "/assets",
  MaintenanceOrder: "/maintenance-orders",
  Warranty: "/warranties",
  SparePart: "/spare-parts",
  Consumable: "/consumables",
  InternalRequest: "/requests",
  User: "/users",
};

export function searchResultHref(linkEntityType: string, linkEntityId: string): string {
  const base = ROUTE_BY_LINK_ENTITY_TYPE[linkEntityType];
  return base ? `${base}/${linkEntityId}` : "#";
}

import type { OrgUnitNode } from "@/lib/api";

export type OrgUnitOption = {
  id: string;
  label: string;
};

/** Depth-first flatten of the (parentOrgUnitId-linked) flat list the API returns into an
 * indented list suitable for a single <select> — see docs/architecture/domain-model.md, ADR 0002. */
export function flattenOrgUnitTree(nodes: OrgUnitNode[]): OrgUnitOption[] {
  const byParent = new Map<string | null, OrgUnitNode[]>();
  for (const node of nodes) {
    const key = node.parentOrgUnitId;
    const siblings = byParent.get(key) ?? [];
    siblings.push(node);
    byParent.set(key, siblings);
  }
  for (const siblings of byParent.values()) {
    siblings.sort((a, b) => a.name.localeCompare(b.name));
  }

  const result: OrgUnitOption[] = [];
  function visit(parentId: string | null, depth: number) {
    for (const node of byParent.get(parentId) ?? []) {
      result.push({ id: node.id, label: `${"— ".repeat(depth)}${node.name} (${node.orgUnitTypeName})` });
      visit(node.id, depth + 1);
    }
  }
  visit(null, 0);

  return result;
}

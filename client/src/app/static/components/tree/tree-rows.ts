import { TreeNode, TreeRow } from './tree.types';

export function buildTreeRows(
  nodes: readonly TreeNode[],
  expandedIds: ReadonlySet<number>
): TreeRow[] {
  const childrenByParent = groupByParent(nodes);
  const knownIds = new Set(nodes.map((node) => node.id));
  const roots = nodes.filter((node) => {
    return node.parentId === null || !knownIds.has(node.parentId);
  });

  const rows: TreeRow[] = [];

  appendRows(roots, 0, childrenByParent, expandedIds, rows);

  return rows;
}

export function collectDescendantIds(
  nodes: readonly TreeNode[],
  rootId: number
): Set<number> {
  const childrenByParent = groupByParent(nodes);
  const descendants = new Set<number>();
  const pending = [rootId];

  while (pending.length) {
    const current = pending.pop() as number;

    for (const child of childrenByParent.get(current) ?? []) {
      if (descendants.has(child.id)) continue;

      descendants.add(child.id);
      pending.push(child.id);
    }
  }

  return descendants;
}

function groupByParent(nodes: readonly TreeNode[]): Map<number, TreeNode[]> {
  const childrenByParent = new Map<number, TreeNode[]>();

  for (const node of nodes) {
    if (node.parentId === null) continue;

    const siblings = childrenByParent.get(node.parentId) ?? [];

    siblings.push(node);
    childrenByParent.set(node.parentId, siblings);
  }

  return childrenByParent;
}

function appendRows(
  siblings: readonly TreeNode[],
  depth: number,
  childrenByParent: Map<number, TreeNode[]>,
  expandedIds: ReadonlySet<number>,
  rows: TreeRow[]
) {
  for (const node of siblings) {
    const children = childrenByParent.get(node.id) ?? [];
    const expanded = expandedIds.has(node.id);

    rows.push({ node, depth, childCount: children.length, expanded });

    if (expanded) {
      appendRows(children, depth + 1, childrenByParent, expandedIds, rows);
    }
  }
}

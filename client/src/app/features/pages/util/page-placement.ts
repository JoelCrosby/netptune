import { MovePageRequest, PageTreeNode } from '@core/models/pages';
import { TreeDropPosition } from '@static/components/tree/tree.types';

// Turns a drop on a tree row into the parent and fractional sort order the server stores.
export function placementFor(
  nodes: readonly PageTreeNode[],
  movedId: number,
  target: PageTreeNode,
  position: TreeDropPosition
): MovePageRequest {
  if (position === 'inside') {
    const children = siblingsOf(nodes, target.id, movedId);
    const lastChild = children.at(-1);

    return {
      parentId: target.id,
      sortOrder: lastChild ? lastChild.sortOrder + 1 : 1,
    };
  }

  const siblings = siblingsOf(nodes, target.parentId, movedId);
  const targetIndex = siblings.findIndex((node) => node.id === target.id);
  const neighbourIndex =
    position === 'before' ? targetIndex - 1 : targetIndex + 1;
  const neighbour = siblings.at(neighbourIndex);
  const hasNeighbour = neighbourIndex >= 0 && neighbour !== undefined;
  const step = position === 'before' ? -1 : 1;

  const sortOrder = hasNeighbour
    ? (target.sortOrder + neighbour.sortOrder) / 2
    : target.sortOrder + step;

  return { parentId: target.parentId, sortOrder };
}

function siblingsOf(
  nodes: readonly PageTreeNode[],
  parentId: number | null,
  excludedId: number
): PageTreeNode[] {
  return nodes
    .filter((node) => node.parentId === parentId && node.id !== excludedId)
    .sort((a, b) => a.sortOrder - b.sortOrder || a.id - b.id);
}

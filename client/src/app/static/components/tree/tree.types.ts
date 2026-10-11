import type { LucideIconInput } from '@lucide/angular';

export interface TreeNode {
  id: number;
  parentId: number | null;
  label: string;
  icon: LucideIconInput;
  link: string[];
}

export type TreeDropPosition = 'before' | 'after' | 'inside';

export interface TreeMove {
  nodeId: number;
  targetId: number;
  position: TreeDropPosition;
}

export interface TreeRow {
  node: TreeNode;
  depth: number;
  childCount: number;
  expanded: boolean;
}

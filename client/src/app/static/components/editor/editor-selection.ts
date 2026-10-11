import type { LucideIconInput } from '@lucide/angular';

export interface EditorSelectionAction {
  id: string;
  label: string;
  icon: LucideIconInput;
}

export interface EditorAnchor {
  left: number;
  top: number;
}

export interface EditorSelection {
  actionId: string;
  from: number;
  to: number;
  markdown: string;
  // Just below the selection, in viewport coordinates.
  anchor: EditorAnchor;
}

export interface AiClientContext {
  view?: string;
  projectId?: number;
  projectName?: string;
  boardId?: number;
  boardName?: string;
  sprintId?: number;
  sprintName?: string;
  taskSystemId?: string;
  taskName?: string;
  pageSlug?: string;
  pageTitle?: string;
  pageSelection?: string;
}

export type AiContextKind =
  'view' | 'project' | 'board' | 'sprint' | 'task' | 'page' | 'selection';

export interface AiContextChip {
  kind: AiContextKind;
  label: string;
  name: string;
  description?: string;
  route: string[] | null;
  context: AiClientContext;
}

export const contextChipKey = (chip: AiContextChip): string => {
  return `${chip.kind}:${chip.name}`;
};

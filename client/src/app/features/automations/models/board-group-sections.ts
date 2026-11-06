import { AutomationBoardGroupOption } from '@core/models/automation-board-group-option';

export interface BoardGroupSection {
  key: string;
  label: string;
  groups: AutomationBoardGroupOption[];
}

export function boardGroupSections(
  options: readonly AutomationBoardGroupOption[]
): BoardGroupSection[] {
  const sections = new Map<string, BoardGroupSection>();

  for (const option of options) {
    const key = `${option.projectId}:${option.boardName}`;
    const section = sections.get(key) ?? {
      key,
      label: boardLabel(option),
      groups: [],
    };

    section.groups.push(option);
    sections.set(key, section);
  }

  return [...sections.values()];
}

export function boardGroupLabel(option: AutomationBoardGroupOption): string {
  return `${boardLabel(option)} · ${option.name}`;
}

function boardLabel(option: AutomationBoardGroupOption): string {
  if (option.boardName === option.projectName) return option.boardName;

  return `${option.projectName} · ${option.boardName}`;
}

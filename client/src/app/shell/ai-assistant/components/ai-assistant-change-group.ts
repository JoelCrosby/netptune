import {
  AiChangeApplyStatus,
  AiChangeValidationStatus,
  AiProposedChange,
} from '@core/models/ai-conversation';

export interface AiChangeGroup {
  key: string;
  entityType: string;
  label: string;
  changes: AiProposedChange[];
}

const ENTITY_LABELS: Record<string, string> = {
  task: $localize`:Entity a proposed change targets:Task`,
  project: $localize`:Entity a proposed change targets:Project`,
  sprint: $localize`:Entity a proposed change targets:Sprint`,
  board: $localize`:Entity a proposed change targets:Board`,
  boardGroup: $localize`:Entity a proposed change targets:Board group`,
  status: $localize`:Entity a proposed change targets:Status`,
  comment: $localize`:Entity a proposed change targets:Comment`,
};

const FIELD_LABELS: Record<string, string> = {
  name: $localize`:Field of a proposed change:Name`,
  description: $localize`:Field of a proposed change:Description`,
  status: $localize`:Field of a proposed change:Status`,
  priority: $localize`:Field of a proposed change:Priority`,
  startDate: $localize`:Field of a proposed change:Start date`,
  endDate: $localize`:Field of a proposed change:End date`,
  dueDate: $localize`:Field of a proposed change:Due date`,
  estimate: $localize`:Field of a proposed change:Estimate`,
  assignee: $localize`:Field of a proposed change:Assignee`,
  tags: $localize`:Field of a proposed change:Tags`,
  sprint: $localize`:Field of a proposed change:Sprint`,
  project: $localize`:Field of a proposed change:Project`,
  board: $localize`:Field of a proposed change:Board`,
  boardGroup: $localize`:Field of a proposed change:Board group`,
  goal: $localize`:Field of a proposed change:Goal`,
  comment: $localize`:Field of a proposed change:Comment`,
  flag: $localize`:Field of a proposed change:Flag`,
  repositoryUrl: $localize`:Field of a proposed change:Repository URL`,
};

/** camelCase field names are internal; anything unmapped still has to read as prose. */
export const fieldLabel = (name: string): string => {
  const known = FIELD_LABELS[name];

  if (known) {
    return known;
  }

  const spaced = name.replace(/([a-z0-9])([A-Z])/g, '$1 $2');

  return spaced.charAt(0).toUpperCase() + spaced.slice(1).toLowerCase();
};

export const entityLabel = (entityType: string): string => {
  return ENTITY_LABELS[entityType] ?? fieldLabel(entityType);
};

export const isValid = (change: AiProposedChange): boolean => {
  return change.validationStatus === AiChangeValidationStatus.valid;
};

export const isApplied = (change: AiProposedChange): boolean => {
  return change.applyStatus === AiChangeApplyStatus.applied;
};

/**
 * Changes to one entity belong together. Anything without an id — a creation the
 * change set has not applied yet — stands alone so two new tasks never merge.
 */
export const groupChanges = (changes: AiProposedChange[]): AiChangeGroup[] => {
  const groups = new Map<string, AiChangeGroup>();

  for (const change of changes) {
    const identity = change.entityId ?? change.refKey ?? `new-${change.id}`;
    const key = `${change.entityType}:${identity}`;
    const existing = groups.get(key);

    if (existing) {
      existing.changes.push(change);

      continue;
    }

    groups.set(key, {
      key,
      entityType: change.entityType,
      label: entityLabel(change.entityType),
      changes: [change],
    });
  }

  return [...groups.values()];
};

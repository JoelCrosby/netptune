import { BulkCollectionMode } from '@core/enums/bulk-collection-mode';
import { estimateTypeLabels } from '@core/enums/estimate-type';
import { TaskPriority, taskPriorityLabels } from '@core/enums/task-priority';
import { BulkEditTask } from './bulk-edit-task';

interface Tally {
  label: string;
  count: number;
}

const noEstimateTypeLabel = $localize`:Stands in for a task with no estimate unit set:No estimate type`;

const noSprintLabel = $localize`:Stands in for a task that belongs to no sprint:No sprint`;

export function statusHint(tasks: BulkEditTask[], locale: string): string {
  return today(distribution(tasks.map(statusLabel), locale));
}

export function priorityHint(tasks: BulkEditTask[], locale: string): string {
  return today(distribution(tasks.map(priorityLabel), locale));
}

export function estimateTypeHint(
  tasks: BulkEditTask[],
  locale: string
): string {
  return today(distribution(tasks.map(estimateTypeLabel), locale));
}

export function dueDateHint(tasks: BulkEditTask[]): string {
  const scheduled = tasks.filter((task) => !!task.dueDate).length;

  return $localize`:Says how many of the selected tasks already have a due date. COUNT is how many do and TOTAL is how many are selected:Today: ${scheduled}:COUNT: of ${tasks.length}:TOTAL: have a due date`;
}

// Story points read differently under each unit, so the row reports the unit rather than the value.
export function estimateValueHint(tasks: BulkEditTask[]): string {
  const units = new Set(tasks.map(estimateTypeLabel));
  const [unit] = units;

  if (units.size > 1) {
    return $localize`:Warns that the selected tasks do not share one estimate unit:Estimate type varies across the selection`;
  }

  return $localize`:Says which estimate unit every selected task uses. UNIT is the unit and TOTAL is how many tasks are selected:Estimate type is ${unit}:UNIT: on all ${tasks.length}:TOTAL:`;
}

export function tagsHint(
  tasks: BulkEditTask[],
  selectedCount: number,
  mode: BulkCollectionMode
): string {
  if (mode === BulkCollectionMode.add) {
    return $localize`:Says what adding tags in a bulk edit does. COUNT is how many tags were picked:Adds ${selectedCount}:COUNT: tags alongside the tags already on each task`;
  }

  return $localize`:Says what replacing tags in a bulk edit does. TOTAL is how many tasks are selected and COUNT is how many tags were picked:Replaces every existing tag on all ${tasks.length}:TOTAL: tasks with these ${selectedCount}:COUNT:`;
}

export function assigneesHint(
  tasks: BulkEditTask[],
  mode: BulkCollectionMode
): string {
  const assigned = new Set(
    tasks.flatMap((task) => task.assignees.map((assignee) => assignee.id))
  );

  if (mode === BulkCollectionMode.add) {
    return $localize`:Says that adding assignees in a bulk edit leaves the current ones in place. COUNT is how many people are assigned across the selection:Keeps the ${assigned.size}:COUNT: people already assigned across the selection`;
  }

  return $localize`:Says that replacing assignees in a bulk edit removes the current ones. COUNT is how many people are assigned across the selection:Replaces the ${assigned.size}:COUNT: people already assigned across the selection`;
}

export function projectHint(
  tasks: BulkEditTask[],
  projectNames: Map<number, string>,
  locale: string
): string {
  const names = tasks.map((task) => {
    return projectNames.get(task.projectId) ?? unknownProjectLabel();
  });
  const distinct = new Set(names);
  const [name] = distinct;

  if (distinct.size === 1) {
    return $localize`:Says that every selected task is already on one project. TOTAL is how many tasks are selected and PROJECT is the project's name:Today: all ${tasks.length}:TOTAL: on ${name}:PROJECT:`;
  }

  return today(distribution(names, locale));
}

export function sprintHint(
  tasks: BulkEditTask[],
  clearsSprint: boolean,
  locale: string
): string {
  if (clearsSprint) {
    const inSprint = tasks.filter((task) => !!task.sprintId).length;

    return $localize`:Says how many of the selected tasks lose their sprint. COUNT is how many are in a sprint today:Clears the sprint on ${inSprint}:COUNT: of the selected tasks`;
  }

  return today(distribution(tasks.map(sprintLabel), locale));
}

function statusLabel(task: BulkEditTask): string {
  return task.statusName;
}

function priorityLabel(task: BulkEditTask): string {
  return taskPriorityLabels[task.priority ?? TaskPriority.none];
}

function estimateTypeLabel(task: BulkEditTask): string {
  const type = task.estimateType;

  return type === null ? noEstimateTypeLabel : estimateTypeLabels[type];
}

function sprintLabel(task: BulkEditTask): string {
  return task.sprintName ?? noSprintLabel;
}

function distribution(labels: string[], locale: string): string {
  const entries = tally(labels);
  const [first] = entries;

  if (entries.length === 1) {
    return everyTask(labels.length, first.label);
  }

  const fragments = entries.map((entry) => counted(entry.count, entry.label));

  return new Intl.ListFormat(locale, {
    style: 'narrow',
    type: 'unit',
  }).format(fragments);
}

function tally(labels: string[]): Tally[] {
  const counts = new Map<string, number>();

  for (const label of labels) {
    counts.set(label, (counts.get(label) ?? 0) + 1);
  }

  return [...counts]
    .map(([label, count]) => ({ label, count }))
    .sort((a, b) => b.count - a.count);
}

function counted(count: number, label: string): string {
  return $localize`:One entry in a breakdown of what the selected tasks hold today, e.g. "3 In Progress". COUNT is how many tasks and VALUE is the value they hold:${count}:COUNT: ${label}:VALUE:`;
}

function everyTask(count: number, label: string): string {
  return $localize`:Says that every selected task holds the same value, e.g. "all 4 High". COUNT is how many tasks and VALUE is the value they hold:all ${count}:COUNT: ${label}:VALUE:`;
}

function today(detail: string): string {
  return $localize`:Prefixes a summary of what the selected tasks hold before a bulk edit is applied. DETAIL is the breakdown:Today: ${detail}:DETAIL:`;
}

function unknownProjectLabel(): string {
  return $localize`:Stands in for a project the current user cannot see:Another project`;
}

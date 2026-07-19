import { RelationCategory } from '@core/models/relation-type';
import {
  RoadmapDisplayTask,
  RoadmapProjectGroup,
  RoadmapRelation,
  RoadmapTask,
} from '../models/roadmap.models';

export const buildRoadmapGroups = (
  tasks: RoadmapTask[],
  relations: RoadmapRelation[]
): RoadmapProjectGroup[] => {
  const tasksById = new Map(tasks.map((task) => [task.id, task]));
  const parentByTaskId = new Map<number, number>();
  const childrenByTaskId = new Map<number, number[]>();
  const blockerCountByTaskId = new Map<number, number>();
  const offscreenBlockerCountByTaskId = new Map<number, number>();

  for (const relation of relations) {
    const hasVisibleSource = tasksById.has(relation.sourceTaskId);
    const hasVisibleTarget = tasksById.has(relation.targetTaskId);

    if (
      relation.category === RelationCategory.hierarchy &&
      hasVisibleSource &&
      hasVisibleTarget
    ) {
      parentByTaskId.set(relation.targetTaskId, relation.sourceTaskId);
      const children = childrenByTaskId.get(relation.sourceTaskId) ?? [];
      children.push(relation.targetTaskId);
      childrenByTaskId.set(relation.sourceTaskId, children);
    }

    if (relation.category === RelationCategory.dependency && hasVisibleTarget) {
      blockerCountByTaskId.set(
        relation.targetTaskId,
        (blockerCountByTaskId.get(relation.targetTaskId) ?? 0) + 1
      );

      if (!hasVisibleSource) {
        offscreenBlockerCountByTaskId.set(
          relation.targetTaskId,
          (offscreenBlockerCountByTaskId.get(relation.targetTaskId) ?? 0) + 1
        );
      }
    }
  }

  const projectTasks = new Map<number, RoadmapTask[]>();

  for (const task of tasks) {
    const tasksInProject = projectTasks.get(task.projectId) ?? [];
    tasksInProject.push(task);
    projectTasks.set(task.projectId, tasksInProject);
  }

  return [...projectTasks.entries()]
    .map(([projectId, tasksInProject]) => ({
      id: projectId,
      name: tasksInProject[0]?.projectName ?? '',
      tasks: flattenProjectTasks(
        tasksInProject,
        parentByTaskId,
        childrenByTaskId,
        blockerCountByTaskId,
        offscreenBlockerCountByTaskId
      ),
    }))
    .sort((left, right) => left.name.localeCompare(right.name));
};

export const filterCollapsedRoadmapGroups = (
  groups: RoadmapProjectGroup[],
  collapsedProjectIds: ReadonlySet<number>,
  collapsedTaskIds: ReadonlySet<number>
): RoadmapProjectGroup[] =>
  groups.map((group) => ({
    ...group,
    tasks: collapsedProjectIds.has(group.id)
      ? []
      : filterCollapsedTasks(group.tasks, collapsedTaskIds),
  }));

const filterCollapsedTasks = (
  rows: RoadmapDisplayTask[],
  collapsedTaskIds: ReadonlySet<number>
): RoadmapDisplayTask[] => {
  const visibleRows: RoadmapDisplayTask[] = [];
  let collapsedDepth: number | undefined;

  for (const row of rows) {
    if (collapsedDepth !== undefined && row.depth > collapsedDepth) {
      continue;
    }

    collapsedDepth = undefined;
    visibleRows.push(row);

    if (row.hasChildren && collapsedTaskIds.has(row.task.id)) {
      collapsedDepth = row.depth;
    }
  }

  return visibleRows;
};

const flattenProjectTasks = (
  tasks: RoadmapTask[],
  parentByTaskId: ReadonlyMap<number, number>,
  childrenByTaskId: ReadonlyMap<number, number[]>,
  blockerCountByTaskId: ReadonlyMap<number, number>,
  offscreenBlockerCountByTaskId: ReadonlyMap<number, number>
): RoadmapDisplayTask[] => {
  const taskIds = new Set(tasks.map((task) => task.id));
  const tasksById = new Map(tasks.map((task) => [task.id, task]));
  const visited = new Set<number>();
  const rows: RoadmapDisplayTask[] = [];

  const append = (taskId: number, depth: number): void => {
    if (visited.has(taskId)) {
      return;
    }

    const task = tasksById.get(taskId);

    if (!task) {
      return;
    }

    visited.add(taskId);
    rows.push({
      task,
      depth,
      parentTaskId: parentByTaskId.get(taskId),
      hasChildren: (childrenByTaskId.get(taskId)?.length ?? 0) > 0,
      blockedByCount: blockerCountByTaskId.get(taskId) ?? 0,
      offscreenBlockedByCount: offscreenBlockerCountByTaskId.get(taskId) ?? 0,
    });

    const children = (childrenByTaskId.get(taskId) ?? [])
      .map((id) => tasksById.get(id))
      .filter((child): child is RoadmapTask => !!child)
      .sort(compareTasks);

    for (const child of children) {
      append(child.id, depth + 1);
    }
  };

  const roots = tasks
    .filter((task) => {
      const parentId = parentByTaskId.get(task.id);
      return parentId === undefined || !taskIds.has(parentId);
    })
    .sort(compareTasks);

  for (const root of roots) {
    append(root.id, 0);
  }

  for (const task of [...tasks].sort(compareTasks)) {
    append(task.id, 0);
  }

  return rows;
};

const compareTasks = (left: RoadmapTask, right: RoadmapTask): number =>
  left.projectScopeId - right.projectScopeId ||
  left.name.localeCompare(right.name);

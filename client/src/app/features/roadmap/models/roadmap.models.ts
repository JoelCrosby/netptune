import { SprintStatus } from '@core/enums/sprint-status';
import {
  ScheduledTask,
  ScheduledTaskChange,
} from '@core/models/scheduled-task';
import { RelationCategory } from '@core/models/relation-type';

export interface RoadmapViewModel {
  from: string;
  to: string;
  tasks: RoadmapTask[];
  relations: RoadmapRelation[];
  sprints: RoadmapSprint[];
  truncated: boolean;
}

export type RoadmapTask = ScheduledTask;

export interface RoadmapRelation {
  id: number;
  sourceTaskId: number;
  targetTaskId: number;
  relationTypeId: number;
  relationTypeKey: string;
  category: RelationCategory;
}

export interface RoadmapSprint {
  id: number;
  name: string;
  startDate: string;
  endDate: string;
  status: SprintStatus;
  projectId: number;
}

export interface RoadmapProjectGroup {
  id: number;
  name: string;
  tasks: RoadmapDisplayTask[];
}

export interface RoadmapDisplayTask {
  task: RoadmapTask;
  depth: number;
  parentTaskId?: number;
  hasChildren: boolean;
  blockedByCount: number;
  offscreenBlockedByCount: number;
}

export type RoadmapScheduleChange = ScheduledTaskChange;

export const roadmapTaskDragType = 'application/x-netptune-roadmap-task';

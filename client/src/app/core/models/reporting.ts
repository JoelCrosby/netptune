export type ReportingUnit = 'Tasks' | 'StoryPoints' | 'Hours';

export interface ReportingCoverage {
  coverageStart?: string | null;
  isPartial: boolean;
}

export interface FlowBucket {
  date: string;
  completed: number;
  medianCycleTimeHours?: number | null;
}

export interface FlowReport {
  buckets: FlowBucket[];
  throughput: number;
  medianCycleTimeHours?: number | null;
  p85CycleTimeHours?: number | null;
  cycleTimeSampleSize: number;
  coverage: ReportingCoverage;
}

export interface WorkloadRow {
  userId?: string | null;
  displayName: string;
  taskCount: number;
  value: number;
}

export interface WorkloadReport {
  rows: WorkloadRow[];
  uniqueTaskCount: number;
  unassignedTaskCount: number;
  multiAssignedTaskCount: number;
  missingEstimateCount: number;
  unit: ReportingUnit;
}

export interface BurndownPoint {
  date: string;
  remaining: number;
  totalScope: number;
  ideal: number;
}

export interface SprintBurndownReport {
  sprintId: number;
  sprintName: string;
  unit: ReportingUnit;
  points: BurndownPoint[];
  committedCount: number;
  addedCount: number;
  removedCount: number;
  missingEstimateCount: number;
  coverage: ReportingCoverage;
}

export interface VelocityPoint {
  sprintId: number;
  sprintName: string;
  completedAt: string;
  committed: number;
  completed: number;
}

export interface VelocityReport {
  sprints: VelocityPoint[];
  unit: ReportingUnit;
  excludedSprintCount: number;
  coverage: ReportingCoverage;
}

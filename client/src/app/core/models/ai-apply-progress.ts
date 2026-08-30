import { AiChangeApplyStatus, AiChangeSetStatus } from './ai-conversation';

export enum AiApplyProgressType {
  started = 0,
  changeStarted = 1,
  changeCompleted = 2,
  completed = 3,
  failed = 4,
}

export interface AiApplyProgress {
  type: AiApplyProgressType;
  total: number;
  completed: number;
  changeId?: number | null;
  status?: AiChangeApplyStatus | null;
  changeSetStatus?: AiChangeSetStatus | null;
  message?: string | null;
}

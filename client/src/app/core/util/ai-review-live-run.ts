import { ActivityLogItem } from '@static/components/activity-log/activity-log.component';
import {
  AiReviewFindingCategory,
  AiReviewStage,
  AiReviewStreamEvent,
  AiReviewStreamEventType,
} from '@core/models/ai-review';

export interface AiReviewLiveDraft {
  category: AiReviewFindingCategory;
  title: string;
}

export interface AiReviewLiveRun {
  startedAt: number;
  stage: AiReviewStage;
  log: ActivityLogItem[];
  thinking: string;
  summary: string | null;
  drafts: AiReviewLiveDraft[];
}

const maxThinkingCharacters = 4_000;

export const startAiReviewLiveRun = (): AiReviewLiveRun => {
  return {
    startedAt: Date.now(),
    stage: 'preparing',
    log: [],
    thinking: '',
    summary: null,
    drafts: [],
  };
};

export const applyAiReviewEvent = (
  run: AiReviewLiveRun,
  event: AiReviewStreamEvent
): AiReviewLiveRun => {
  switch (event.type) {
    case AiReviewStreamEventType.stage:
      return applyStage(run, event.stage ?? run.stage);
    case AiReviewStreamEventType.note:
      return { ...run, log: appendNote(run.log, event.text ?? '') };
    case AiReviewStreamEventType.thinking:
      return { ...run, thinking: appendThinking(run, event) };
    case AiReviewStreamEventType.tool:
      return { ...run, log: [...run.log, step(event.tool ?? '')] };
    case AiReviewStreamEventType.toolFinished:
      return { ...run, log: finishStep(run.log, event) };
    case AiReviewStreamEventType.finding:
      return { ...run, drafts: [...run.drafts, draft(event)] };
    case AiReviewStreamEventType.summary:
      return { ...run, summary: event.text ?? null };
    default:
      return run;
  }
};

// A reply that turned out to be a note before another tool call takes its drafts with it.
const applyStage = (
  run: AiReviewLiveRun,
  stage: AiReviewStage
): AiReviewLiveRun => {
  const isBackToInvestigating =
    run.stage === 'writing' && stage === 'investigating';

  if (!isBackToInvestigating) return { ...run, stage };

  return { ...run, stage, summary: null, drafts: [] };
};

const appendNote = (
  log: ActivityLogItem[],
  text: string
): ActivityLogItem[] => {
  const last = log.at(-1);

  if (last?.kind !== 'note') {
    return [...log, { kind: 'note', text, state: 'done' }];
  }

  return [...log.slice(0, -1), { ...last, text: last.text + text }];
};

const appendThinking = (run: AiReviewLiveRun, event: AiReviewStreamEvent) => {
  const text = event.text ?? '';
  const thinking = event.isNewBlock ? text : run.thinking + text;

  return thinking.slice(-maxThinkingCharacters);
};

const step = (label: string): ActivityLogItem => {
  return { kind: 'step', text: label, state: 'running' };
};

const finishStep = (
  log: ActivityLogItem[],
  event: AiReviewStreamEvent
): ActivityLogItem[] => {
  const runningIndexes = log
    .map((item, position) => (item.state === 'running' ? position : -1))
    .filter((position) => position >= 0);
  const index = runningIndexes.at(-1) ?? -1;

  if (index < 0) return log;

  const state: ActivityLogItem['state'] = event.isError ? 'failed' : 'done';

  return log.map((item, position) => {
    return position === index ? { ...item, state } : item;
  });
};

const draft = (event: AiReviewStreamEvent): AiReviewLiveDraft => {
  return {
    category: event.category ?? AiReviewFindingCategory.gap,
    title: event.text ?? '',
  };
};

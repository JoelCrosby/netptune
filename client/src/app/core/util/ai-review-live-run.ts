import { StepLogItem } from '@static/components/step-log/step-log.component';
import {
  AiReviewFindingCategory,
  AiReviewStage,
  AiReviewStreamEvent,
  AiReviewStreamEventType,
} from '@core/models/ai-review';

export interface AiReviewLiveDraft {
  position: number;
  category: AiReviewFindingCategory;
  title: string;
  explanation: string | null;
}

export interface AiReviewLiveRun {
  startedAt: number;
  stage: AiReviewStage;
  log: StepLogItem[];
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
      return { ...run, drafts: withDraft(run.drafts, draft(event)) };
    case AiReviewStreamEventType.findingDetail:
      return { ...run, drafts: withExplanation(run.drafts, event) };
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

const appendNote = (log: StepLogItem[], text: string): StepLogItem[] => {
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

const step = (label: string): StepLogItem => {
  return { kind: 'step', text: label, state: 'running' };
};

const finishStep = (
  log: StepLogItem[],
  event: AiReviewStreamEvent
): StepLogItem[] => {
  const runningIndexes = log
    .map((item, position) => (item.state === 'running' ? position : -1))
    .filter((position) => position >= 0);
  const index = runningIndexes.at(-1) ?? -1;

  if (index < 0) return log;

  const state: StepLogItem['state'] = event.isError ? 'failed' : 'done';

  return log.map((item, position) => {
    return position === index ? { ...item, state } : item;
  });
};

const draft = (event: AiReviewStreamEvent): AiReviewLiveDraft => {
  return {
    position: event.position ?? 0,
    category: event.category ?? AiReviewFindingCategory.gap,
    title: event.text ?? '',
    explanation: null,
  };
};

// Drafts are keyed by their place in the reply. A detail already on screen outlives
// the title being reported again, because the two arrive as separate events.
const withDraft = (
  drafts: AiReviewLiveDraft[],
  next: AiReviewLiveDraft
): AiReviewLiveDraft[] => {
  const index = drafts.findIndex((item) => item.position === next.position);

  if (index < 0) return [...drafts, next];

  return drafts.map((item, position) => {
    return position === index
      ? { ...next, explanation: item.explanation ?? next.explanation }
      : item;
  });
};

const withExplanation = (
  drafts: AiReviewLiveDraft[],
  event: AiReviewStreamEvent
): AiReviewLiveDraft[] => {
  const index = drafts.findIndex((item) => item.position === event.position);

  if (index < 0) return drafts;

  return drafts.map((item, position) => {
    return position === index
      ? { ...item, explanation: event.text ?? null }
      : item;
  });
};

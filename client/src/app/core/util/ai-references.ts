import { AiEntityReference } from '@core/models/ai-conversation';

export type AiTextSegment =
  | { kind: 'text'; value: string }
  | { kind: 'reference'; type: string; id: string; label: string };

const REFERENCE_PATTERN =
  /\[\[(task|project|sprint|board):([^\]|]+)(?:\|([^\]]+))?\]\]/g;

const ROUTES: Record<string, string> = {
  task: 'tasks',
  project: 'projects',
  sprint: 'sprints',
  board: 'boards',
};

export const referenceKey = (type: string, id: string): string => {
  return `${type}:${id}`;
};

export const referenceMap = (
  references: AiEntityReference[]
): Map<string, AiEntityReference> => {
  return new Map(
    references.map((reference) => {
      return [referenceKey(reference.type, reference.id), reference];
    })
  );
};

export const referenceRoute = (
  workspace: string,
  type: string,
  id: string
): string[] | null => {
  const segment = ROUTES[type];

  if (!segment) {
    return null;
  }

  return ['/', workspace, segment, id];
};

export const dropPartialReference = (text: string): string => {
  const opened = text.lastIndexOf('[[');

  if (opened < 0) {
    return text;
  }

  const closed = text.indexOf(']]', opened);

  return closed < 0 ? text.slice(0, opened) : text;
};

export interface AiReferenceSlot {
  type: string;
  id: string;
  label: string;
  raw: string;
}

export interface AiProtectedText {
  text: string;
  slots: AiReferenceSlot[];
}

const SENTINEL_OPEN = '\uE000';
const SENTINEL_CLOSE = '\uE001';
const SENTINEL_PATTERN = /\uE000(\d+)\uE001/g;

export const protectReferences = (source: string): AiProtectedText => {
  const slots: AiReferenceSlot[] = [];

  REFERENCE_PATTERN.lastIndex = 0;

  const text = source.replace(
    REFERENCE_PATTERN,
    (raw: string, type: string, id: string, label?: string) => {
      const trimmedId = id.trim();
      const index = slots.length;

      slots.push({
        type,
        id: trimmedId,
        label: label?.trim() || trimmedId,
        raw,
      });

      return `${SENTINEL_OPEN}${index}${SENTINEL_CLOSE}`;
    }
  );

  return { text, slots };
};

export const expandReferences = (
  text: string,
  slots: AiReferenceSlot[]
): AiTextSegment[] => {
  const segments: AiTextSegment[] = [];
  let index = 0;

  SENTINEL_PATTERN.lastIndex = 0;

  for (
    let match = SENTINEL_PATTERN.exec(text);
    match !== null;
    match = SENTINEL_PATTERN.exec(text)
  ) {
    if (match.index > index) {
      segments.push({ kind: 'text', value: text.slice(index, match.index) });
    }

    const slot = slots[Number(match[1])];

    if (slot) {
      segments.push({
        kind: 'reference',
        type: slot.type,
        id: slot.id,
        label: slot.label,
      });
    }

    index = match.index + match[0].length;
  }

  if (index < text.length) {
    segments.push({ kind: 'text', value: text.slice(index) });
  }

  return segments;
};

/** Code is shown as the model wrote it, so a reference inside it stays literal. */
export const restoreReferences = (
  text: string,
  slots: AiReferenceSlot[]
): string => {
  SENTINEL_PATTERN.lastIndex = 0;

  return text.replace(SENTINEL_PATTERN, (match: string, index: string) => {
    return slots[Number(index)]?.raw ?? match;
  });
};

export const parseAssistantText = (
  text: string,
  isStreaming = false
): AiTextSegment[] => {
  const source = isStreaming ? dropPartialReference(text) : text;
  const segments: AiTextSegment[] = [];
  let index = 0;

  REFERENCE_PATTERN.lastIndex = 0;

  for (
    let match = REFERENCE_PATTERN.exec(source);
    match !== null;
    match = REFERENCE_PATTERN.exec(source)
  ) {
    if (match.index > index) {
      segments.push({ kind: 'text', value: source.slice(index, match.index) });
    }

    const id = match[2].trim();

    segments.push({
      kind: 'reference',
      type: match[1],
      id,
      label: match[3]?.trim() || id,
    });

    index = match.index + match[0].length;
  }

  if (index < source.length) {
    segments.push({ kind: 'text', value: source.slice(index) });
  }

  return segments;
};

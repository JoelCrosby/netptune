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

/**
 * A reference the model has only half-written is dropped so a partly streamed
 * token never flashes as literal text before it closes.
 */
export const dropPartialReference = (text: string): string => {
  const opened = text.lastIndexOf('[[');

  if (opened < 0) {
    return text;
  }

  const closed = text.indexOf(']]', opened);

  return closed < 0 ? text.slice(0, opened) : text;
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

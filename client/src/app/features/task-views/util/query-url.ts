import {
  TaskQueryCondition,
  TaskQueryGroup,
  TaskQueryGroupOperator,
  TaskQueryOperator,
} from '../models/task-view.models';

// A link has to survive being pasted into chat clients and mail, so the query travels
// base64url encoded rather than as raw JSON in the query string.
const maximumEncodedLength = 4096;

interface CompactGroup {
  o: TaskQueryGroupOperator;
  c?: CompactCondition[];
  g?: CompactGroup[];
}

interface CompactCondition {
  f: string;
  o: TaskQueryOperator;
  v?: string[];
}

export function encodeQuery(query: TaskQueryGroup): string | null {
  try {
    const json = JSON.stringify(toCompact(query));
    const encoded = toBase64Url(json);

    return encoded.length > maximumEncodedLength ? null : encoded;
  } catch {
    return null;
  }
}

export function decodeQuery(encoded: string | null): TaskQueryGroup | null {
  if (!encoded || encoded.length > maximumEncodedLength) return null;

  try {
    const json = fromBase64Url(encoded);
    const parsed = JSON.parse(json) as CompactGroup;

    return fromCompact(parsed);
  } catch {
    return null;
  }
}

function toCompact(group: TaskQueryGroup): CompactGroup {
  const compact: CompactGroup = { o: group.operator };

  if (group.conditions.length) {
    compact.c = group.conditions.map(toCompactCondition);
  }

  if (group.groups.length) {
    compact.g = group.groups.map(toCompact);
  }

  return compact;
}

function toCompactCondition(condition: TaskQueryCondition): CompactCondition {
  const compact: CompactCondition = {
    f: condition.field,
    o: condition.operator,
  };

  if (condition.values.length) {
    compact.v = condition.values;
  }

  return compact;
}

function fromCompact(compact: CompactGroup): TaskQueryGroup {
  return {
    operator: compact.o ?? TaskQueryGroupOperator.all,
    conditions: (compact.c ?? []).map((condition) => ({
      field: condition.f,
      operator: condition.o,
      values: condition.v ?? [],
    })),
    groups: (compact.g ?? []).map(fromCompact),
  };
}

function toBase64Url(value: string): string {
  const bytes = new TextEncoder().encode(value);
  const binary = Array.from(bytes, (byte) => String.fromCharCode(byte)).join(
    ''
  );

  return btoa(binary)
    .replace(/\+/g, '-')
    .replace(/\//g, '_')
    .replace(/=+$/, '');
}

function fromBase64Url(value: string): string {
  const padded = value.replace(/-/g, '+').replace(/_/g, '/');
  const binary = atob(padded);
  const bytes = Uint8Array.from(binary, (character) => character.charCodeAt(0));

  return new TextDecoder().decode(bytes);
}

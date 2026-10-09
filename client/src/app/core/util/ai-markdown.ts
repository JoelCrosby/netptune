import { Token, Tokens, marked } from 'marked';
import {
  AiReferenceSlot,
  dropPartialReference,
  expandReferences,
  protectReferences,
  restoreReferences,
} from './ai-references';

export type AiMarkdownInline =
  | { kind: 'text'; value: string }
  | { kind: 'strong'; children: AiMarkdownInline[] }
  | { kind: 'em'; children: AiMarkdownInline[] }
  | { kind: 'strike'; children: AiMarkdownInline[] }
  | { kind: 'code'; value: string }
  | { kind: 'link'; href: string; children: AiMarkdownInline[] }
  | { kind: 'reference'; type: string; id: string; label: string }
  | { kind: 'break' };

export type AiMarkdownBlock =
  | { kind: 'paragraph'; inline: AiMarkdownInline[] }
  | { kind: 'heading'; level: number; inline: AiMarkdownInline[] }
  | { kind: 'code'; value: string; lang: string | null }
  | {
      kind: 'list';
      ordered: boolean;
      start: number;
      items: AiMarkdownBlock[][];
    }
  | { kind: 'quote'; blocks: AiMarkdownBlock[] }
  | { kind: 'table'; head: AiMarkdownInline[][]; rows: AiMarkdownInline[][][] }
  | { kind: 'rule' };

const FENCE = '```';
const CODE_MARK = '`';
const PAIRED_MARKERS = ['**', '~~'];
const PARTIAL_LINK_TARGET = /\[([^\]]*)\]\([^)]*$/;
const PARTIAL_LINK_LABEL = /\[([^\]]*)$/;
const CODE_SPAN = /`[^`]*`/g;
const TABLE_ROW = /^\s*\|/;
const TABLE_DELIMITER = /^\s*\|?\s*:?-+:?\s*(\|\s*:?-+:?\s*)*\|?\s*$/;
const TRAILING_SPACE = /\s*$/;

interface UnclosedMarker {
  index: number;
  marker: string;
}

const countOf = (text: string, marker: string): number => {
  return text.split(marker).length - 1;
};

// Code spans are blanked out at the same length, so marker positions still line up with
// the real text while markers inside code stop counting.
const maskCodeSpans = (text: string): string => {
  return text.replace(CODE_SPAN, (span) => 'x'.repeat(span.length));
};

const unclosedPair = (masked: string, marker: string): UnclosedMarker[] => {
  const isBalanced = countOf(masked, marker) % 2 === 0;

  if (isBalanced) {
    return [];
  }

  return [{ index: masked.lastIndexOf(marker), marker }];
};

// A lone star opens emphasis only when it hugs the word after it, which leaves out list
// bullets and arithmetic.
const unclosedStar = (masked: string): UnclosedMarker[] => {
  const singles = masked.replaceAll('**', 'xx');
  const openers: number[] = [];

  for (let index = 0; index < singles.length; index++) {
    const isStar = singles[index] === '*';
    const before = singles[index - 1] ?? ' ';
    const after = singles[index + 1] ?? ' ';
    const isStandalone = /\s/.test(before) && /\s/.test(after);
    const isBullet = singles.slice(0, index).trim() === '' && after === ' ';

    if (isStar && !isStandalone && !isBullet) {
      openers.push(index);
    }
  }

  const isBalanced = openers.length % 2 === 0;

  if (isBalanced) {
    return [];
  }

  return [{ index: openers[openers.length - 1], marker: '*' }];
};

const unclosedCode = (line: string): UnclosedMarker[] => {
  const isBalanced = countOf(line, CODE_MARK) % 2 === 0;

  if (isBalanced) {
    return [];
  }

  return [{ index: line.lastIndexOf(CODE_MARK), marker: CODE_MARK }];
};

// The label is worth showing as it arrives; the half-typed address is not.
const healPartialLink = (line: string): string => {
  return line
    .replace(PARTIAL_LINK_TARGET, '$1')
    .replace(PARTIAL_LINK_LABEL, '$1');
};

// Closing a span early styles its text from the first word, where dropping the opener
// would show it plain and then restyle it once the closer arrives.
const healInline = (line: string): string => {
  const linked = healPartialLink(line);
  const code = unclosedCode(linked);
  const masked = maskCodeSpans(
    code.length > 0 ? linked.slice(0, code[0].index) : linked
  );
  const emphasis = [
    ...PAIRED_MARKERS.flatMap((marker) => unclosedPair(masked, marker)),
    ...unclosedStar(masked),
  ];
  const unclosed = [...code, ...emphasis].sort((a, b) => b.index - a.index);

  let healed = linked;
  let closers = '';

  for (const { index, marker } of unclosed) {
    const content = healed.slice(index + marker.length).trim();

    if (content.length === 0) {
      healed = `${healed.slice(0, index)}${healed.slice(index + marker.length)}`;

      continue;
    }

    closers += marker;
  }

  const trailing = TRAILING_SPACE.exec(healed)?.[0] ?? '';

  return `${healed.trimEnd()}${closers}${trailing}`;
};

const cellCount = (row: string): number => {
  return row.trim().replace(/^\|/, '').replace(/\|$/, '').split('|').length;
};

// A table's header row reads as a line of pipes until the delimiter row under it arrives.
const holdPartialTable = (lines: string[]): string[] => {
  let start = lines.length;

  while (start > 0 && TABLE_ROW.test(lines[start - 1])) {
    start--;
  }

  const rows = lines.slice(start);
  const hasRows = rows.length > 0;
  const hasDelimiter =
    rows.length > 1 &&
    TABLE_DELIMITER.test(rows[1]) &&
    cellCount(rows[1]) === cellCount(rows[0]);

  if (!hasRows || hasDelimiter) {
    return lines;
  }

  return lines.slice(0, start);
};

const closePartialSyntax = (text: string): string => {
  const isFenceOpen = countOf(text, FENCE) % 2 === 1;

  if (isFenceOpen) {
    return `${text}\n${FENCE}`;
  }

  const lines = holdPartialTable(text.split('\n'));
  const last = lines.length - 1;

  if (last < 0) {
    return '';
  }

  lines[last] = healInline(lines[last]);

  return lines.join('\n');
};

const toInline = (
  tokens: Token[] | undefined,
  slots: AiReferenceSlot[]
): AiMarkdownInline[] => {
  if (!tokens) {
    return [];
  }

  return tokens.flatMap((token) => {
    return toInlineToken(token, slots);
  });
};

const toInlineToken = (
  token: Token,
  slots: AiReferenceSlot[]
): AiMarkdownInline[] => {
  switch (token.type) {
    case 'strong':
      return [{ kind: 'strong', children: toInline(token.tokens, slots) }];
    case 'em':
      return [{ kind: 'em', children: toInline(token.tokens, slots) }];
    case 'del':
      return [{ kind: 'strike', children: toInline(token.tokens, slots) }];
    case 'codespan':
      return [
        {
          kind: 'code',
          value: restoreReferences((token as Tokens.Codespan).text, slots),
        },
      ];
    case 'br':
      return [{ kind: 'break' }];
    case 'link':
      return [
        {
          kind: 'link',
          href: (token as Tokens.Link).href,
          children: toInline(token.tokens, slots),
        },
      ];
    case 'text':
    case 'escape':
    case 'html':
      return toReferenceAware(token, slots);
    default:
      return toReferenceAware(token, slots);
  }
};

/** Workspace references live inside plain text, so they are split out here. */
const toReferenceAware = (
  token: Token,
  slots: AiReferenceSlot[]
): AiMarkdownInline[] => {
  const nested = 'tokens' in token ? token.tokens : undefined;
  const hasNested = Array.isArray(nested) && nested.length > 0;

  if (hasNested) {
    return toInline(nested, slots);
  }

  const text = 'text' in token ? token.text : token.raw;

  return expandReferences(text ?? '', slots).map((segment) => {
    if (segment.kind === 'text') {
      return { kind: 'text', value: segment.value };
    }

    return {
      kind: 'reference',
      type: segment.type,
      id: segment.id,
      label: segment.label,
    };
  });
};

const toBlocks = (
  tokens: Token[],
  slots: AiReferenceSlot[]
): AiMarkdownBlock[] => {
  return tokens.flatMap((token) => {
    return toBlock(token, slots);
  });
};

const toBlock = (token: Token, slots: AiReferenceSlot[]): AiMarkdownBlock[] => {
  switch (token.type) {
    case 'space':
      return [];
    case 'heading':
      return [
        {
          kind: 'heading',
          level: (token as Tokens.Heading).depth,
          inline: toInline(token.tokens, slots),
        },
      ];
    case 'code':
      return [
        {
          kind: 'code',
          value: restoreReferences((token as Tokens.Code).text, slots),
          lang: (token as Tokens.Code).lang || null,
        },
      ];
    case 'blockquote':
      return [{ kind: 'quote', blocks: toBlocks(token.tokens ?? [], slots) }];
    case 'hr':
      return [{ kind: 'rule' }];
    case 'list':
      return [toList(token as Tokens.List, slots)];
    case 'table':
      return [toTable(token as Tokens.Table, slots)];
    default:
      return [{ kind: 'paragraph', inline: toInlineToken(token, slots) }];
  }
};

const toList = (
  token: Tokens.List,
  slots: AiReferenceSlot[]
): AiMarkdownBlock => {
  const start = typeof token.start === 'number' ? token.start : 1;

  return {
    kind: 'list',
    ordered: token.ordered,
    start,
    items: token.items.map((item) => {
      return toBlocks(item.tokens ?? [], slots);
    }),
  };
};

const toTable = (
  token: Tokens.Table,
  slots: AiReferenceSlot[]
): AiMarkdownBlock => {
  return {
    kind: 'table',
    head: token.header.map((cell) => {
      return toInline(cell.tokens, slots);
    }),
    rows: token.rows.map((row) => {
      return row.map((cell) => {
        return toInline(cell.tokens, slots);
      });
    }),
  };
};

export const parseAssistantMarkdown = (
  text: string,
  isStreaming = false
): AiMarkdownBlock[] => {
  const settled = isStreaming ? dropPartialReference(text) : text;
  const protectedText = protectReferences(settled);
  const source = isStreaming
    ? closePartialSyntax(protectedText.text)
    : protectedText.text;
  const tokens = marked.lexer(source, { gfm: true, breaks: true });

  return toBlocks(tokens, protectedText.slots);
};

const toPlainInline = (nodes: AiMarkdownInline[]): string => {
  return nodes
    .map((node) => {
      switch (node.kind) {
        case 'text':
        case 'code':
          return node.value;
        case 'reference':
          return node.label;
        case 'break':
          return ' ';
        default:
          return toPlainInline(node.children);
      }
    })
    .join('');
};

const toPlainBlock = (block: AiMarkdownBlock): string => {
  switch (block.kind) {
    case 'paragraph':
    case 'heading':
      return toPlainInline(block.inline);
    case 'list':
      return block.items.map(toPlainBlocks).join(' ');
    case 'quote':
      return toPlainBlocks(block.blocks);
    default:
      return '';
  }
};

const toPlainBlocks = (blocks: AiMarkdownBlock[]): string => {
  return blocks.map(toPlainBlock).join(' ');
};

export const summarizeAssistantMarkdown = (
  text: string,
  limit = 160
): string => {
  const blocks = parseAssistantMarkdown(text);
  const prose = blocks
    .map((block) => {
      return toPlainBlock(block).replace(/\s+/g, ' ').trim();
    })
    .find((value) => value.length > 0);

  if (!prose) {
    return '';
  }

  if (prose.length <= limit) {
    return prose;
  }

  return `${prose.slice(0, limit).trimEnd()}…`;
};

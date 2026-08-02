import { Token, Tokens, marked } from 'marked';
import { dropPartialReference, parseAssistantText } from './ai-references';

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

/**
 * Markers the model has opened but not yet closed, dropped so the text reads as
 * plain prose until its closing marker arrives rather than flashing as syntax.
 * Lone `*` and `_` are left alone — stripping them would eat the underscores in
 * identifiers far more often than it would hide an emphasis marker.
 */
const UNCLOSED_MARKERS = ['**', '~~', '`'];

const dropUnclosedMarker = (text: string, marker: string): string => {
  const isBalanced = (text.split(marker).length - 1) % 2 === 0;

  if (isBalanced) {
    return text;
  }

  const opened = text.lastIndexOf(marker);

  return `${text.slice(0, opened)}${text.slice(opened + marker.length)}`;
};

const closePartialSyntax = (text: string): string => {
  const fences = text.split(FENCE).length - 1;
  const isFenceOpen = fences % 2 === 1;

  if (isFenceOpen) {
    return `${text}\n${FENCE}`;
  }

  const lastLineBreak = text.lastIndexOf('\n');
  const head = text.slice(0, lastLineBreak + 1);
  const tail = UNCLOSED_MARKERS.reduce(
    dropUnclosedMarker,
    text.slice(lastLineBreak + 1)
  );

  return `${head}${tail}`;
};

const toInline = (tokens: Token[] | undefined): AiMarkdownInline[] => {
  if (!tokens) {
    return [];
  }

  return tokens.flatMap((token) => {
    return toInlineToken(token);
  });
};

const toInlineToken = (token: Token): AiMarkdownInline[] => {
  switch (token.type) {
    case 'strong':
      return [{ kind: 'strong', children: toInline(token.tokens) }];
    case 'em':
      return [{ kind: 'em', children: toInline(token.tokens) }];
    case 'del':
      return [{ kind: 'strike', children: toInline(token.tokens) }];
    case 'codespan':
      return [{ kind: 'code', value: (token as Tokens.Codespan).text }];
    case 'br':
      return [{ kind: 'break' }];
    case 'link':
      return [
        {
          kind: 'link',
          href: (token as Tokens.Link).href,
          children: toInline(token.tokens),
        },
      ];
    case 'text':
    case 'escape':
    case 'html':
      return toReferenceAware(token);
    default:
      return toReferenceAware(token);
  }
};

/** Workspace references live inside plain text, so they are split out here. */
const toReferenceAware = (token: Token): AiMarkdownInline[] => {
  const nested = 'tokens' in token ? token.tokens : undefined;
  const hasNested = Array.isArray(nested) && nested.length > 0;

  if (hasNested) {
    return toInline(nested);
  }

  const text = 'text' in token ? token.text : token.raw;

  return parseAssistantText(text ?? '').map((segment) => {
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

const toBlocks = (tokens: Token[]): AiMarkdownBlock[] => {
  return tokens.flatMap((token) => {
    return toBlock(token);
  });
};

const toBlock = (token: Token): AiMarkdownBlock[] => {
  switch (token.type) {
    case 'space':
      return [];
    case 'heading':
      return [
        {
          kind: 'heading',
          level: (token as Tokens.Heading).depth,
          inline: toInline(token.tokens),
        },
      ];
    case 'code':
      return [
        {
          kind: 'code',
          value: (token as Tokens.Code).text,
          lang: (token as Tokens.Code).lang || null,
        },
      ];
    case 'blockquote':
      return [{ kind: 'quote', blocks: toBlocks(token.tokens ?? []) }];
    case 'hr':
      return [{ kind: 'rule' }];
    case 'list':
      return [toList(token as Tokens.List)];
    case 'table':
      return [toTable(token as Tokens.Table)];
    default:
      return [{ kind: 'paragraph', inline: toInlineToken(token) }];
  }
};

const toList = (token: Tokens.List): AiMarkdownBlock => {
  const start = typeof token.start === 'number' ? token.start : 1;

  return {
    kind: 'list',
    ordered: token.ordered,
    start,
    items: token.items.map((item) => {
      return toBlocks(item.tokens ?? []);
    }),
  };
};

const toTable = (token: Tokens.Table): AiMarkdownBlock => {
  return {
    kind: 'table',
    head: token.header.map((cell) => {
      return toInline(cell.tokens);
    }),
    rows: token.rows.map((row) => {
      return row.map((cell) => {
        return toInline(cell.tokens);
      });
    }),
  };
};

export const parseAssistantMarkdown = (
  text: string,
  isStreaming = false
): AiMarkdownBlock[] => {
  const source = isStreaming
    ? closePartialSyntax(dropPartialReference(text))
    : text;
  const tokens = marked.lexer(source, { gfm: true, breaks: true });

  return toBlocks(tokens);
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

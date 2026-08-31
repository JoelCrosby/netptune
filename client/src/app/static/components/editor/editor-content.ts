import type { JSONContent } from '@tiptap/core';
import { marked, type Token, type Tokens } from 'marked';

// Descriptions are stored as markdown, so the editor parses one on the way in and writes one back
// out. Only the marks markdown can express are enabled on the editor, so nothing is lost in between.

const lexerOptions = { gfm: true, breaks: false };

interface Mark {
  type: string;
  attrs?: Record<string, unknown>;
}

export function markdownToDocument(
  markdown: string | null | undefined
): JSONContent {
  if (!markdown?.trim()) {
    return { type: 'doc', content: [{ type: 'paragraph' }] };
  }

  const tokens = marked.lexer(markdown, lexerOptions);
  const content = toBlocks(tokens);

  if (!content.length) {
    return { type: 'doc', content: [{ type: 'paragraph' }] };
  }

  return { type: 'doc', content };
}

export function documentToMarkdown(document: JSONContent): string {
  return blocksToMarkdown(document.content ?? []).trim();
}

function toBlocks(tokens: Token[]): JSONContent[] {
  const blocks: JSONContent[] = [];

  for (const token of tokens) {
    const block = toBlock(token);

    if (!block) continue;

    blocks.push(...block);
  }

  return blocks;
}

function toBlock(token: Token): JSONContent[] | null {
  switch (token.type) {
    case 'heading':
      return [heading(token as Tokens.Heading)];
    case 'paragraph':
      return [paragraph(toInline((token as Tokens.Paragraph).tokens))];
    case 'text':
      return [paragraph(toInline(textTokens(token as Tokens.Text)))];
    case 'code':
      return [codeBlock(token as Tokens.Code)];
    case 'hr':
      return [{ type: 'horizontalRule' }];
    case 'blockquote':
      return [blockquote(token as Tokens.Blockquote)];
    case 'list':
      return lists(token as Tokens.List);
    case 'table':
      return table(token as Tokens.Table);
    case 'html':
      return [paragraph([text((token as Tokens.HTML).raw.trim())])];
    default:
      return null;
  }
}

function textTokens(token: Tokens.Text): Token[] {
  return (
    token.tokens ?? [
      { type: 'text', raw: token.raw, text: token.text } as Tokens.Text,
    ]
  );
}

function heading(token: Tokens.Heading): JSONContent {
  return {
    type: 'heading',
    attrs: { level: Math.min(Math.max(token.depth, 1), 6) },
    content: toInline(token.tokens),
  };
}

function codeBlock(token: Tokens.Code): JSONContent {
  const language = token.lang?.split(/\s+/)[0] ?? '';
  const block: JSONContent = {
    type: 'codeBlock',
    attrs: { language: language || null },
  };

  if (token.text.length) {
    block.content = [text(token.text)];
  }

  return block;
}

function blockquote(token: Tokens.Blockquote): JSONContent {
  const content = toBlocks(token.tokens);

  return {
    type: 'blockquote',
    content: content.length ? content : [{ type: 'paragraph' }],
  };
}

// A task list is a bullet list in markdown, so one markdown list can hold both kinds. They are
// different nodes here, and splitting the runs keeps what is written matching what is read back.
function lists(token: Tokens.List): JSONContent[] {
  const blocks: JSONContent[] = [];

  let run: Tokens.ListItem[] = [];
  let runIsTask = false;

  for (const item of token.items) {
    const isTask = Boolean(item.task);

    if (run.length && isTask !== runIsTask) {
      blocks.push(list(token, run, runIsTask));
      run = [];
    }

    runIsTask = isTask;
    run.push(item);
  }

  if (run.length) {
    blocks.push(list(token, run, runIsTask));
  }

  return blocks;
}

function list(
  token: Tokens.List,
  items: Tokens.ListItem[],
  isTaskList: boolean
): JSONContent {
  if (isTaskList) {
    return { type: 'taskList', content: items.map(taskItem) };
  }

  if (!token.ordered) {
    return { type: 'bulletList', content: items.map(listItem) };
  }

  return {
    type: 'orderedList',
    attrs: { start: Number(token.start) || 1 },
    content: items.map(listItem),
  };
}

function listItem(item: Tokens.ListItem): JSONContent {
  return { type: 'listItem', content: itemContent(item) };
}

function taskItem(item: Tokens.ListItem): JSONContent {
  return {
    type: 'taskItem',
    attrs: { checked: Boolean(item.checked) },
    content: itemContent(item),
  };
}

function itemContent(item: Tokens.ListItem): JSONContent[] {
  const blocks = toBlocks(item.tokens);
  const startsWithParagraph = blocks[0]?.type === 'paragraph';

  if (!startsWithParagraph) {
    return [{ type: 'paragraph' }, ...blocks];
  }

  return blocks;
}

// The schema has no table node, so a table keeps its text as one paragraph per row.
function table(token: Tokens.Table): JSONContent[] {
  const rows = [token.header, ...token.rows];

  return rows.map((row) => {
    const cells = row.map((cell) => toInline(cell.tokens));

    return paragraph(joinCells(cells));
  });
}

function joinCells(cells: JSONContent[][]): JSONContent[] {
  const content: JSONContent[] = [];

  for (const cell of cells) {
    if (content.length) {
      content.push(text(' | '));
    }

    content.push(...cell);
  }

  return content;
}

function paragraph(content: JSONContent[]): JSONContent {
  const isLoneImage = content.length === 1 && content[0].type === 'image';

  if (isLoneImage) {
    return content[0];
  }

  if (!content.length) {
    return { type: 'paragraph' };
  }

  return { type: 'paragraph', content };
}

function toInline(
  tokens: Token[] | undefined,
  marks: Mark[] = []
): JSONContent[] {
  if (!tokens) return [];

  const content: JSONContent[] = [];

  for (const token of tokens) {
    content.push(...inlineToken(token, marks));
  }

  return content;
}

function inlineToken(token: Token, marks: Mark[]): JSONContent[] {
  switch (token.type) {
    case 'text':
    case 'escape':
      return [text((token as Tokens.Text).text, marks)];
    case 'strong':
      return toInline((token as Tokens.Strong).tokens, [
        ...marks,
        { type: 'bold' },
      ]);
    case 'em':
      return toInline((token as Tokens.Em).tokens, [
        ...marks,
        { type: 'italic' },
      ]);
    case 'del':
      return toInline((token as Tokens.Del).tokens, [
        ...marks,
        { type: 'strike' },
      ]);
    case 'codespan':
      return [
        text((token as Tokens.Codespan).text, [...marks, { type: 'code' }]),
      ];
    case 'br':
      return [{ type: 'hardBreak' }];
    case 'link':
      return link(token as Tokens.Link, marks);
    case 'image':
      return [image(token as Tokens.Image)];
    case 'html':
      return [text((token as Tokens.HTML).raw, marks)];
    default:
      return [];
  }
}

function link(token: Tokens.Link, marks: Mark[]): JSONContent[] {
  const linked = [...marks, { type: 'link', attrs: { href: token.href } }];
  const content = toInline(token.tokens, linked);

  if (content.length) return content;

  return [text(token.href, linked)];
}

function image(token: Tokens.Image): JSONContent {
  return {
    type: 'image',
    attrs: { src: token.href, alt: token.text, title: token.title },
  };
}

function text(value: string, marks: Mark[] = []): JSONContent {
  const node: JSONContent = { type: 'text', text: value };

  if (marks.length) {
    node.marks = marks;
  }

  return node;
}

function blocksToMarkdown(blocks: JSONContent[]): string {
  const segments: string[] = [];

  for (const block of blocks) {
    const segment = blockToMarkdown(block);

    if (!segment.length) continue;

    segments.push(segment);
  }

  return segments.join('\n\n');
}

function blockToMarkdown(block: JSONContent): string {
  switch (block.type) {
    case 'heading':
      return headingToMarkdown(block);
    case 'paragraph':
      return escapeBlockStarts(inlineToMarkdown(block.content));
    case 'codeBlock':
      return codeBlockToMarkdown(block);
    case 'blockquote':
      return quoteToMarkdown(block);
    case 'horizontalRule':
      return '---';
    case 'image':
      return imageToMarkdown(block);
    case 'bulletList':
    case 'orderedList':
    case 'taskList':
      return listToMarkdown(block, 0);
    default:
      return blocksToMarkdown(block.content ?? []);
  }
}

function headingToMarkdown(block: JSONContent): string {
  const level = Number(block.attrs?.['level']) || 2;

  return `${'#'.repeat(level)} ${inlineToMarkdown(block.content)}`;
}

function codeBlockToMarkdown(block: JSONContent): string {
  const language = (block.attrs?.['language'] as string | null) ?? '';
  const code = (block.content ?? []).map((node) => node.text ?? '').join('');

  return `\`\`\`${language}\n${code}\n\`\`\``;
}

function quoteToMarkdown(block: JSONContent): string {
  const inner = blocksToMarkdown(block.content ?? []);

  return inner
    .split('\n')
    .map((line) => (line.length ? `> ${line}` : '>'))
    .join('\n');
}

function imageToMarkdown(block: JSONContent): string {
  const source = (block.attrs?.['src'] as string) ?? '';
  const alt = (block.attrs?.['alt'] as string) ?? '';

  return `![${alt}](${source})`;
}

function listToMarkdown(block: JSONContent, depth: number): string {
  const isOrdered = block.type === 'orderedList';
  const indent = '  '.repeat(depth);
  const lines: string[] = [];

  let number = Number(block.attrs?.['start']) || 1;

  for (const item of block.content ?? []) {
    const marker = itemMarker(item, isOrdered, number);
    const children = item.content ?? [];
    const body = children.filter((child) => !isList(child));
    const nested = children.filter(isList);
    const inner = blocksToMarkdown(body).split('\n');

    lines.push(`${indent}${marker}${inner[0] ?? ''}`);
    lines.push(...inner.slice(1).map((line) => `${indent}  ${line}`));

    for (const child of nested) {
      lines.push(listToMarkdown(child, depth + 1));
    }

    number++;
  }

  return lines.join('\n');
}

function itemMarker(
  item: JSONContent,
  isOrdered: boolean,
  number: number
): string {
  if (isOrdered) return `${number}. `;

  if (item.type !== 'taskItem') return '- ';

  return item.attrs?.['checked'] ? '- [x] ' : '- [ ] ';
}

function isList(node: JSONContent): boolean {
  return (
    node.type === 'bulletList' ||
    node.type === 'orderedList' ||
    node.type === 'taskList'
  );
}

function inlineToMarkdown(content: JSONContent[] | undefined): string {
  if (!content) return '';

  return content.map(inlineNodeToMarkdown).join('');
}

function inlineNodeToMarkdown(node: JSONContent): string {
  if (node.type === 'hardBreak') return '  \n';

  if (node.type === 'image') return imageToMarkdown(node);

  if (node.type !== 'text') return inlineToMarkdown(node.content);

  const marks = new Set((node.marks ?? []).map((mark) => mark.type));
  const value = node.text ?? '';

  let body = marks.has('code') ? `\`${value}\`` : escapeText(value);

  if (marks.has('bold')) body = `**${body}**`;
  if (marks.has('italic')) body = `*${body}*`;
  if (marks.has('strike')) body = `~~${body}~~`;

  const href = (node.marks ?? []).find((mark) => mark.type === 'link')?.attrs?.[
    'href'
  ] as string | undefined;

  if (href) body = `[${body}](${href})`;

  return body;
}

function escapeText(value: string): string {
  return value.replace(/([\\`*[~])/g, '\\$1');
}

// Prose that happens to open with a block marker would come back as that block, so the marker is
// escaped on every line the paragraph occupies.
function escapeBlockStarts(paragraph: string): string {
  return paragraph
    .split('\n')
    .map((line) => line.replace(blockStartPattern, '$1\\$2'))
    .join('\n');
}

const blockStartPattern =
  /^(\s*)(#{1,6}[ \t]|>|[-+][ \t]|\d+[.)][ \t]|-{3,}$|={3,}$)/;

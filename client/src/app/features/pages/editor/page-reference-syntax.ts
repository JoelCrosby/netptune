import type { JSONContent } from '@tiptap/core';
import { InlineSyntax } from '@static/components/editor/editor-content';

export type PageReferenceKind =
  'task' | 'page' | 'user' | 'project' | 'sprint' | 'board';

export interface PageReferenceAttrs {
  kind: PageReferenceKind;
  key: string;
  label: string | null;
}

export const pageReferenceNodeType = 'pageReference';

// Same grammar as the server's PageReferenceParser and the assistant's [[task:ID|label]] links.
const referencePattern =
  /\[\[(task|page|user|project|sprint|board):([^\]|\s]+)(?:\|([^\]]*))?\]\]/gi;

export const pageReferenceSyntax: InlineSyntax = {
  nodeType: pageReferenceNodeType,
  pattern: referencePattern,
  toNode: (match) => {
    const attrs: PageReferenceAttrs = {
      kind: match[1].toLowerCase() as PageReferenceKind,
      key: match[2],
      label: match[3] ?? null,
    };

    return { type: pageReferenceNodeType, attrs };
  },
  toMarkdown: (node: JSONContent) => {
    const attrs = node.attrs as PageReferenceAttrs;
    const label = sanitiseLabel(attrs.label);

    return label
      ? `[[${attrs.kind}:${attrs.key}|${label}]]`
      : `[[${attrs.kind}:${attrs.key}]]`;
  },
};

// A label is display text only. Characters markdown would read as syntax are dropped, so the token reads
// the same to every markdown parser, the server's included.
function sanitiseLabel(label: string | null): string {
  return (label ?? '')
    .replace(/[[\]|\n`*_~<>\\]/g, ' ')
    .replace(/:\/\//g, ' ')
    .replace(/\s+/g, ' ')
    .trim();
}

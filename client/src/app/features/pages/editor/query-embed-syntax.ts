import type { JSONContent } from '@tiptap/core';
import { BlockSyntax } from '@static/components/editor/editor-content';

export const queryEmbedNodeType = 'pageQueryEmbed';

export const queryEmbedSyntax: BlockSyntax = {
  nodeType: queryEmbedNodeType,
  pattern: /\{\{query:([a-zA-Z0-9-]+)\}\}/,
  toNode: (match) => {
    return {
      type: queryEmbedNodeType,
      attrs: { slug: match[1] },
    };
  },
  toMarkdown: (node: JSONContent) => {
    const slug = (node.attrs?.['slug'] as string | undefined) ?? '';

    return `{{query:${slug}}}`;
  },
};

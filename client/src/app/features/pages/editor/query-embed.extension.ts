import { Node } from '@tiptap/core';
import type { JSONContent } from '@tiptap/core';
import { queryEmbedNodeType, queryEmbedSyntax } from './query-embed-syntax';

export interface QueryEmbedMount {
  update(slug: string): void;
  destroy(): void;
}

// Draws a saved query's live results into the element it is given; the page supplies it.
export type QueryEmbedRenderer = (
  slug: string,
  element: HTMLElement
) => QueryEmbedMount;

export function createQueryEmbedExtension(render: QueryEmbedRenderer) {
  return Node.create({
    name: queryEmbedNodeType,
    group: 'block',
    atom: true,
    selectable: true,
    draggable: true,

    addAttributes() {
      return {
        slug: { default: '' },
      };
    },

    parseHTML() {
      return [
        {
          tag: 'div[data-query-embed]',
          getAttrs: (element) => ({
            slug: element.getAttribute('data-query-embed'),
          }),
        },
      ];
    },

    renderHTML({ node }) {
      return ['div', { 'data-query-embed': node.attrs['slug'] as string }];
    },

    renderText({ node }) {
      return queryEmbedSyntax.toMarkdown(node.toJSON() as JSONContent);
    },

    addNodeView() {
      return ({ node: initialNode }) => {
        let node = initialNode;
        const dom = document.createElement('div');
        const mount = render(node.attrs['slug'] as string, dom);

        return {
          dom,
          update: (updated) => {
            if (updated.type !== node.type) return false;

            node = updated;
            mount.update(node.attrs['slug'] as string);

            return true;
          },
          // Links inside the embed handle their own clicks; everything else, drags included, goes to the
          // editor so the block can be selected, dragged and deleted.
          stopEvent: (event) => isLinkClick(event),
          ignoreMutation: () => true,
          destroy: () => mount.destroy(),
        };
      };
    },
  });
}

function isLinkClick(event: Event): boolean {
  const target = event.target instanceof Element ? event.target : null;
  const onLink = target?.closest('a') != null;
  const isPointer = event.type === 'mousedown' || event.type === 'click';

  return isPointer && onLink;
}

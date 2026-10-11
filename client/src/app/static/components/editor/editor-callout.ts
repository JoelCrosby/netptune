import { Node } from '@tiptap/core';

// GitHub's alert types, plus info, which other markdown tools use for the same thing.
const calloutVariants: ReadonlySet<string> = new Set([
  'note',
  'info',
  'tip',
  'important',
  'warning',
  'caution',
]);

export function isCalloutVariant(variant: string): boolean {
  return calloutVariants.has(variant);
}

// A highlighted block of prose, written to markdown as a GitHub-style alert.
export const Callout = Node.create({
  name: 'callout',
  group: 'block',
  content: 'block+',
  defining: true,

  addAttributes() {
    return {
      variant: {
        default: 'note',
        parseHTML: (element) => element.getAttribute('data-callout') ?? 'note',
        renderHTML: (attributes) => ({
          'data-callout': attributes['variant'] as string,
        }),
      },
    };
  },

  parseHTML() {
    return [{ tag: 'div[data-callout]' }];
  },

  renderHTML({ HTMLAttributes }) {
    return ['div', { ...HTMLAttributes, class: 'editor-callout' }, 0];
  },
});

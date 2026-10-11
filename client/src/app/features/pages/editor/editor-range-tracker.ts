import type { Editor, EditorEvents } from '@tiptap/core';

export interface TrackedRange {
  from: number;
  to: number;
}

export function trackRange(editor: Editor, start: TrackedRange) {
  let range: TrackedRange | null = { ...start };

  const follow = ({ transaction }: EditorEvents['transaction']) => {
    if (!range) return;

    // Mapped inward, so text typed right at either edge stays outside the range.
    const from = transaction.mapping.mapResult(range.from, 1);
    const to = transaction.mapping.mapResult(range.to, -1);
    const isCollapsed = start.from === start.to;
    // deletedAcross, not deleted: removing the character at an edge must not lose the range, only a
    // deletion that swallows the position whole, as replacing the document does.
    const wasDeleted = from.deletedAcross || to.deletedAcross;

    range = wasDeleted
      ? null
      : {
          from: from.pos,
          to: isCollapsed ? from.pos : Math.max(from.pos, to.pos),
        };
  };

  editor.on('transaction', follow);

  return {
    current: (): TrackedRange | null => {
      if (!range) return null;

      const size = editor.state.doc.content.size;

      return { from: Math.min(range.from, size), to: Math.min(range.to, size) };
    },
    stop: () => editor.off('transaction', follow),
  };
}

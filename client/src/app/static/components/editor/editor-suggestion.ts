import type { Range } from '@tiptap/core';
import { EditorState, Plugin, PluginKey } from '@tiptap/pm/state';
import { exitSuggestion } from '@tiptap/suggestion';

// The suggestion menu caps its list at max-h-72 (288px); this leaves room for its padding.
const menuHeight = 300;
const caretGap = 6;

export interface SuggestionMenuPosition {
  left: number;
  top: number;
  // Shown above the caret, for the caller to translate the menu up by its own height.
  above: boolean;
}

export const initialSuggestionMenuPosition: SuggestionMenuPosition = {
  left: 0,
  top: 0,
  above: false,
};

export function suggestionMenuPosition(rect: DOMRect): SuggestionMenuPosition {
  const fitsBelow = rect.bottom + caretGap + menuHeight <= window.innerHeight;

  if (fitsBelow) {
    return { left: rect.left, top: rect.bottom + caretGap, above: false };
  }

  return { left: rect.left, top: rect.top - caretGap, above: true };
}

export function isOutsideCode({
  state,
  range,
}: {
  state: EditorState;
  range: Range;
}): boolean {
  const $from = state.doc.resolve(range.from);
  const inCodeBlock = !!$from.parent.type.spec.code;
  const codeMark = state.schema.marks['code'];
  const inInlineCode = !!codeMark?.isInSet($from.marks());

  return !inCodeBlock && !inInlineCode;
}

// Suggestion plugins ignore blur, which would leave their menu floating over whatever took focus.
export function closeSuggestionOnBlur(suggestionKey: PluginKey): Plugin {
  return new Plugin({
    props: {
      handleDOMEvents: {
        blur: (view) => {
          const suggestion = suggestionKey.getState(view.state) as
            { active?: boolean } | undefined;

          if (suggestion?.active) {
            exitSuggestion(view, suggestionKey);
          }

          return false;
        },
      },
    },
  });
}

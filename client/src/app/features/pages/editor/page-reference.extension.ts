import { Node } from '@tiptap/core';
import { PluginKey } from '@tiptap/pm/state';
import {
  closeSuggestionOnBlur,
  isOutsideCode,
} from '@static/components/editor/editor-suggestion';
import {
  Suggestion,
  SuggestionKeyDownProps,
  SuggestionProps,
} from '@tiptap/suggestion';
import {
  PageReferenceAttrs,
  pageReferenceNodeType,
} from './page-reference-syntax';

export interface PageReferenceDisplay {
  text: string;
  key: string | null;
  resolved: boolean;
}

export interface PageReferenceSuggestionHandlers {
  start(props: SuggestionProps<unknown, PageReferenceAttrs>): void;
  update(props: SuggestionProps<unknown, PageReferenceAttrs>): void;
  keyDown(props: SuggestionKeyDownProps): boolean;
  exit(): void;
}

// What the node needs from the page around it: names for references, and somewhere to send hovers,
// clicks and the @ menu.
export interface PageReferenceHost {
  display(attrs: PageReferenceAttrs): PageReferenceDisplay;
  subscribe(listener: () => void): () => void;
  hovered(attrs: PageReferenceAttrs, element: HTMLElement): void;
  unhovered(element: HTMLElement): void;
  opened(attrs: PageReferenceAttrs): void;
  suggestion: PageReferenceSuggestionHandlers;
}

// Each suggestion plugin needs its own key, or the @ and / menus would share one state.
const pageReferenceSuggestionKey = new PluginKey('pageReferenceSuggestion');

export function createPageReferenceExtension(host: PageReferenceHost) {
  return Node.create({
    name: pageReferenceNodeType,
    group: 'inline',
    inline: true,
    atom: true,
    selectable: true,

    addAttributes() {
      return {
        kind: { default: 'task' },
        key: { default: '' },
        label: { default: null },
      };
    },

    parseHTML() {
      return [
        {
          tag: 'span[data-reference-kind]',
          getAttrs: (element) => ({
            kind: element.getAttribute('data-reference-kind'),
            key: element.getAttribute('data-reference-key'),
            label: element.getAttribute('data-reference-label'),
          }),
        },
      ];
    },

    renderHTML({ node }) {
      const attrs = node.attrs as PageReferenceAttrs;

      return [
        'span',
        {
          'data-reference-kind': attrs.kind,
          'data-reference-key': attrs.key,
          'data-reference-label': attrs.label,
        },
        attrs.label ?? attrs.key,
      ];
    },

    renderText({ node }) {
      const attrs = node.attrs as PageReferenceAttrs;

      return `[[${attrs.kind}:${attrs.key}]]`;
    },

    addNodeView() {
      return ({ node: initialNode }) => {
        let node = initialNode;
        const dom = document.createElement('span');

        const render = () => {
          renderReference(dom, node.attrs as PageReferenceAttrs, host);
        };

        render();

        const unsubscribe = host.subscribe(render);

        dom.addEventListener('mouseenter', () => {
          host.hovered(node.attrs as PageReferenceAttrs, dom);
        });
        dom.addEventListener('mouseleave', () => host.unhovered(dom));
        dom.addEventListener('click', (event) => {
          event.preventDefault();
          host.opened(node.attrs as PageReferenceAttrs);
        });

        return {
          dom,
          update: (updated) => {
            if (updated.type !== node.type) return false;

            node = updated;
            render();

            return true;
          },
          destroy: () => {
            unsubscribe();
            host.unhovered(dom);
          },
        };
      };
    },

    addProseMirrorPlugins() {
      return [
        Suggestion<unknown, PageReferenceAttrs>({
          pluginKey: pageReferenceSuggestionKey,
          editor: this.editor,
          char: '@',
          allow: isOutsideCode,
          items: () => [],
          command: ({ editor, range, props }) => {
            editor
              .chain()
              .focus()
              .insertContentAt(range, [
                { type: pageReferenceNodeType, attrs: props },
                { type: 'text', text: ' ' },
              ])
              .run();
          },
          render: () => ({
            onStart: (props) => host.suggestion.start(props),
            onUpdate: (props) => host.suggestion.update(props),
            onKeyDown: (props) => host.suggestion.keyDown(props),
            onExit: () => host.suggestion.exit(),
          }),
        }),
        closeSuggestionOnBlur(pageReferenceSuggestionKey),
      ];
    },
  });
}

function renderReference(
  dom: HTMLElement,
  attrs: PageReferenceAttrs,
  host: PageReferenceHost
) {
  const display = host.display(attrs);

  dom.className = 'editor-reference';
  dom.setAttribute('data-kind', attrs.kind);
  dom.toggleAttribute('data-unresolved', !display.resolved);
  dom.replaceChildren();

  if (display.key) {
    const key = document.createElement('span');

    key.className = 'editor-reference-key';
    key.textContent = display.key;
    dom.append(key);
  }

  if (display.text) {
    dom.append(document.createTextNode(display.text));
  }
}

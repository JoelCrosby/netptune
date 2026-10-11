import type { Editor } from '@tiptap/core';
import { CodeBlock } from '@tiptap/extension-code-block';
import type { Node as ProseMirrorNode } from '@tiptap/pm/model';
import type { NodeView, ViewMutationRecord } from '@tiptap/pm/view';
import { createHighlightPlugin, type Parser } from 'prosemirror-highlight';

interface CodeLanguage {
  value: string;
  label: string;
}

const codeLanguages: readonly CodeLanguage[] = [
  { value: 'bash', label: 'Bash' },
  { value: 'csharp', label: 'C#' },
  { value: 'css', label: 'CSS' },
  { value: 'diff', label: 'Diff' },
  { value: 'xml', label: 'HTML / XML' },
  { value: 'javascript', label: 'JavaScript' },
  { value: 'json', label: 'JSON' },
  { value: 'markdown', label: 'Markdown' },
  { value: 'python', label: 'Python' },
  { value: 'sql', label: 'SQL' },
  { value: 'typescript', label: 'TypeScript' },
  { value: 'yaml', label: 'YAML' },
];

const languageAliases: Readonly<Record<string, string>> = {
  cs: 'csharp',
  html: 'xml',
  js: 'javascript',
  md: 'markdown',
  py: 'python',
  sh: 'bash',
  shell: 'bash',
  ts: 'typescript',
  yml: 'yaml',
};

const copyIcon =
  '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect width="14" height="14" x="8" y="8" rx="2" ry="2"/><path d="M4 16c-1.1 0-2-.9-2-2V4c0-1.1.9-2 2-2h10c1.1 0 2 .9 2 2"/></svg>';

const copiedIcon =
  '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M20 6 9 17l-5-5"/></svg>';

const copiedResetMs = 1500;

const labels = {
  copy: $localize`:Button on a code block that copies its code:Copy code`,
  copied: $localize`:Shown on a code block's copy button once the code is copied:Copied`,
  language: $localize`:Accessible label for the language picker on a code block:Code language`,
  plainText: $localize`:Language option for a code block that is not highlighted:Plain text`,
};

let loadedParser: Parser | null = null;
let parserLoading: Promise<void> | null = null;

const lazyParser: Parser = (options) => {
  if (!options.language) return [];

  if (loadedParser) return loadedParser(options);

  parserLoading ??= import('./editor-code-languages').then((module) => {
    loadedParser = module.codeParser;
  });

  return parserLoading;
};

export const HighlightedCodeBlock = CodeBlock.extend({
  addProseMirrorPlugins() {
    return [
      ...(this.parent?.() ?? []),
      createHighlightPlugin({ parser: lazyParser, nodeTypes: [this.name] }),
    ];
  },

  addNodeView() {
    return ({ node, editor, getPos }) => {
      return new CodeBlockView(
        node,
        editor,
        getPos,
        this.options.languageClassPrefix ?? ''
      );
    };
  },
});

class CodeBlockView implements NodeView {
  readonly dom: HTMLElement;
  readonly contentDOM: HTMLElement;

  private readonly toolbar: HTMLElement;
  private readonly languageSelect: HTMLSelectElement;
  private readonly languageLabel: HTMLElement;
  private readonly copyButton: HTMLButtonElement;
  private copiedTimer: ReturnType<typeof setTimeout> | undefined;

  constructor(
    private node: ProseMirrorNode,
    private readonly editor: Editor,
    private readonly getPos: () => number | undefined,
    private readonly languageClassPrefix: string
  ) {
    this.dom = document.createElement('div');
    this.dom.className = 'editor-code-block';

    this.toolbar = document.createElement('div');
    this.toolbar.className = 'editor-code-toolbar';
    this.toolbar.contentEditable = 'false';

    this.languageSelect = document.createElement('select');
    this.languageSelect.className = 'editor-code-language';
    this.languageSelect.setAttribute('aria-label', labels.language);
    this.languageSelect.addEventListener('change', () => {
      this.setLanguage(this.languageSelect.value);
    });

    this.languageLabel = document.createElement('span');
    this.languageLabel.className = 'editor-code-language-label';

    this.copyButton = document.createElement('button');
    this.copyButton.type = 'button';
    this.copyButton.className = 'editor-code-copy';
    this.copyButton.addEventListener('click', () => this.copy());
    this.showCopyState(false);

    this.toolbar.append(
      this.languageSelect,
      this.languageLabel,
      this.copyButton
    );

    const pre = document.createElement('pre');

    this.contentDOM = document.createElement('code');
    pre.append(this.contentDOM);
    this.dom.append(this.toolbar, pre);

    this.syncLanguage();
  }

  update(node: ProseMirrorNode): boolean {
    if (node.type !== this.node.type) return false;

    this.node = node;
    this.syncLanguage();

    return true;
  }

  stopEvent(event: Event): boolean {
    return this.toolbar.contains(event.target as Node);
  }

  ignoreMutation(mutation: ViewMutationRecord): boolean {
    if (mutation.type === 'selection') return false;

    return this.toolbar.contains(mutation.target);
  }

  destroy() {
    clearTimeout(this.copiedTimer);
  }

  private syncLanguage() {
    const language = (this.node.attrs['language'] as string | null) ?? '';
    const selected = languageAliases[language] ?? language;
    const known = codeLanguages.find((option) => option.value === selected);
    const options =
      known || !language
        ? codeLanguages
        : [...codeLanguages, { value: language, label: language }];

    this.languageSelect.replaceChildren(
      new Option(labels.plainText, ''),
      ...options.map((option) => new Option(option.label, option.value))
    );
    this.languageSelect.value = selected;
    this.languageLabel.textContent = known?.label ?? language;
    this.contentDOM.className = language
      ? `${this.languageClassPrefix}${language}`
      : '';
  }

  private setLanguage(language: string) {
    const pos = this.getPos();

    if (pos === undefined) return;

    const transaction = this.editor.state.tr.setNodeAttribute(
      pos,
      'language',
      language || null
    );

    this.editor.view.dispatch(transaction);
  }

  private async copy() {
    try {
      await navigator.clipboard.writeText(this.node.textContent);
    } catch {
      return;
    }

    this.showCopyState(true);
    clearTimeout(this.copiedTimer);
    this.copiedTimer = setTimeout(
      () => this.showCopyState(false),
      copiedResetMs
    );
  }

  private showCopyState(copied: boolean) {
    const label = copied ? labels.copied : labels.copy;

    this.copyButton.innerHTML = copied ? copiedIcon : copyIcon;
    this.copyButton.title = label;
    this.copyButton.setAttribute('aria-label', label);
    this.copyButton.toggleAttribute('data-copied', copied);
  }
}

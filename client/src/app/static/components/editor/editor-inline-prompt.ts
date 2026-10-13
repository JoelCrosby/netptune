import { formatElapsed } from '@core/util/duration';
import { Typewriter } from '@core/util/typewriter';
import type { LucideIconInput } from '@lucide/angular';
import { Extension, type Editor, type JSONContent } from '@tiptap/core';
import { closeHistory } from '@tiptap/pm/history';
import {
  Fragment,
  Slice,
  type Node as ProseMirrorNode,
} from '@tiptap/pm/model';
import {
  EditorState,
  Plugin,
  PluginKey,
  Selection,
  type Transaction,
} from '@tiptap/pm/state';
import type { Mapping } from '@tiptap/pm/transform';
import { Decoration, DecorationSet, type EditorView } from '@tiptap/pm/view';

const settleMs = 600;

// Typed a character at a time, a line's opening markup ("- [ ] ", "## ") or half of a "**"
// would flash up as stray characters before it takes effect, so the reveal waits for it.
const partialMarkup =
  /(?:^|\n)(?:#{1,6}|[-*+](?: \[[ xX]?\]?)?|\d+\.?|>|`{1,3}\w*) ?$|[*~`]$/;

const inlineMarks = ['**', '~~', '`'];

const followMarginPx = 56;

const minimumReportedMs = 1000;
const elapsedRefreshMs = 250;

const spinnerIcon =
  '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M21 12a9 9 0 1 1-6.219-8.56"/></svg>';

const sparklesIcon =
  '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M11.017 2.814a1 1 0 0 1 1.966 0l1.051 5.558a2 2 0 0 0 1.594 1.594l5.558 1.051a1 1 0 0 1 0 1.966l-5.558 1.051a2 2 0 0 0-1.594 1.594l-1.051 5.558a1 1 0 0 1-1.966 0l-1.051-5.558a2 2 0 0 0-1.594-1.594l-5.558-1.051a1 1 0 0 1 0-1.966l5.558-1.051a2 2 0 0 0 1.594-1.594z"/><path d="M20 2v4"/><path d="M22 4h-4"/><circle cx="4" cy="20" r="2"/></svg>';

export interface InlinePromptRequest {
  prompt: string;
  before: string;
  after: string;
}

// Resolves once the whole reply has gone through write. A rejection's message is shown inline.
export type InlinePromptRunner = (
  request: InlinePromptRequest,
  write: (text: string) => void,
  signal: AbortSignal
) => Promise<void>;

export interface InlinePromptLabels {
  placeholder: string;
  send: string;
  cancel: string;
  thinking: string;
  retry: string;
  dismiss: string;
  failed: string;
}

export interface InlinePromptCommand {
  label: string;
  group: string;
  icon: LucideIconInput;
  keywords?: readonly string[];
}

export interface InlinePromptConfig {
  // Typed at the start of a word, opens the prompt.
  trigger: string;
  run: InlinePromptRunner;
  labels: InlinePromptLabels;
  // Offers the prompt in the slash menu too, when the editor has one.
  command: InlinePromptCommand;
  maxRevealPerSecond?: number;
}

export interface InlinePromptHost {
  config: () => InlinePromptConfig | null;
  toDocument: (markdown: string) => JSONContent;
  markdownBetween: (from: number, to: number) => string;
  busyChanged: (busy: boolean) => void;
}

type InlinePromptPhase =
  'idle' | 'composing' | 'working' | 'error' | 'settling';

interface InlinePromptState {
  phase: InlinePromptPhase;
  from: number;
  to: number;
  // Where the cursor sits, relative to from, once the range is put back as it was.
  offset: number;
  writing: boolean;
  message: string | null;
}

interface PromptRange {
  from: number;
  to: number;
}

interface Placement extends PromptRange {
  offset: number;
  replaced: Slice;
}

const noLabels: InlinePromptLabels = {
  placeholder: '',
  send: '',
  cancel: '',
  thinking: '',
  retry: '',
  dismiss: '',
  failed: '',
};

const idleState: InlinePromptState = {
  phase: 'idle',
  from: 0,
  to: 0,
  offset: 0,
  writing: false,
  message: null,
};

const inlinePromptKey = new PluginKey<InlinePromptState>('inlinePrompt');

export function isInlinePromptActive(state: EditorState): boolean {
  const phase = inlinePromptKey.getState(state)?.phase ?? 'idle';

  return phase !== 'idle';
}

export class InlinePrompt {
  private editor: Editor | null = null;
  private request: InlinePromptRequest | null = null;
  private abort: AbortController | null = null;
  private original = Slice.empty;
  private written = '';
  private finished = false;
  private typewriter: Typewriter | null = null;
  private settleTimer: ReturnType<typeof setTimeout> | null = null;
  private busy = false;
  private startedAt = 0;
  // The config a prompt opened with, so a config arriving mid-prompt does not swap its runner or copy.
  private session: InlinePromptConfig | null = null;

  constructor(private readonly host: InlinePromptHost) {}

  get labels(): InlinePromptLabels {
    return this.session?.labels ?? noLabels;
  }

  get workingSince(): number {
    return this.startedAt;
  }

  get trigger(): string | null {
    return this.host.config()?.trigger ?? null;
  }

  attach(editor: Editor) {
    this.editor = editor;
  }

  open(end: number) {
    const editor = this.editor;

    if (!editor) return;

    const trigger = this.trigger;

    if (!editor || !trigger) return;

    const isStillTyped = triggerEndAt(editor.state, trigger) === end;

    if (!isStillTyped) return;

    this.openAt(end - trigger.length, end);
  }

  openAt(from: number, to: number) {
    const editor = this.editor;
    const config = this.host.config();
    const isIdle = this.state.phase === 'idle';

    if (!editor || !editor.isEditable || !config || !isIdle) return;

    this.session = config;

    const tr = editor.state.tr.delete(from, to);

    this.dispatch(tr, { ...idleState, phase: 'composing', from, to: from });
    this.setBusy(true);
  }

  submit() {
    const editor = this.editor;
    const { phase, from, to } = this.state;

    if (!editor || phase !== 'composing') return;

    const doc = editor.state.doc;
    const prompt = doc.textBetween(from, to, '\n', '\n').trim();

    if (!prompt) {
      this.cancel();

      return;
    }

    this.request = {
      prompt,
      before: this.host.markdownBetween(0, from),
      after: this.host.markdownBetween(to, doc.content.size),
    };

    const tr = editor.state.tr.delete(from, to);

    this.startWorking(tr, from);
  }

  retry() {
    const editor = this.editor;
    const { phase, from, offset } = this.state;

    if (!editor || phase !== 'error') return;

    this.setBusy(true);
    this.startWorking(editor.state.tr, from + offset);
  }

  cancel() {
    const editor = this.editor;
    const { phase, from, to } = this.state;

    if (!editor || phase === 'idle') return;

    this.stopRun();

    if (phase === 'composing') {
      this.dispatch(editor.state.tr.delete(from, to), idleState);
    } else if (phase === 'working') {
      this.dispatch(
        editor.state.tr.replace(from, to, this.original),
        idleState
      );
    } else {
      this.dispatch(editor.state.tr, idleState);
    }

    this.setBusy(false);
  }

  // The prompt's range went with an edit that replaced it, so there is nothing left to restore.
  release() {
    this.stopRun();
    this.setBusy(false);
  }

  private get state(): InlinePromptState {
    const editor = this.editor;

    if (!editor) return idleState;

    return inlinePromptKey.getState(editor.state) ?? idleState;
  }

  private startWorking(tr: Transaction, anchor: number) {
    this.original = Slice.empty;
    this.startedAt = performance.now();
    this.dispatch(tr, {
      ...idleState,
      phase: 'working',
      from: anchor,
      to: anchor,
    });

    void this.run();
  }

  private async run() {
    const runner = this.session?.run;
    const request = this.request;

    if (!runner || !request) {
      this.fail(this.labels.failed);

      return;
    }

    const abort = new AbortController();

    this.abort = abort;
    this.written = '';
    this.finished = false;
    this.typewriter?.discard();

    const typewriter = this.createTypewriter();

    this.typewriter = typewriter;

    try {
      await runner(request, (text) => this.receive(text, abort), abort.signal);

      if (abort.signal.aborted) return;

      this.finished = true;

      if (typewriter.isIdle) {
        this.complete();
      }
    } catch (error) {
      if (abort.signal.aborted) return;

      const message = error instanceof Error ? error.message : '';

      this.fail(message || this.labels.failed);
    }
  }

  private receive(text: string, abort: AbortController) {
    if (abort.signal.aborted) return;

    this.typewriter?.push(text);
  }

  private createTypewriter(): Typewriter {
    return new Typewriter((text) => this.reveal(text), {
      drained: () => this.onDrained(),
      maxCharactersPerSecond: this.session?.maxRevealPerSecond,
      byCharacter: true,
    });
  }

  private reveal(text: string) {
    if (this.state.phase !== 'working') return;

    this.written += text;

    const isMidMarker = partialMarkup.test(this.written);

    if (isMidMarker) return;

    this.render(closeOpenMarks(this.written));
  }

  private onDrained() {
    const isWorking = this.state.phase === 'working';

    if (this.finished && isWorking) {
      this.complete();
    }
  }

  private render(markdown: string) {
    const editor = this.editor;
    const current = this.state;

    if (!editor) return;

    const tr = editor.state.tr;
    const anchor = this.restore(tr, current);
    const placement = this.place(tr, anchor, markdown);

    this.dispatch(tr, { ...current, ...placement, writing: true });
  }

  // The streamed text never enters the undo history: the original content goes back first,
  // then the reply goes in as one recorded step, so a single undo takes it all out.
  private complete() {
    const editor = this.editor;
    const current = this.state;

    if (!editor) return;

    const restore = editor.state.tr;
    const restoredTo = current.from + this.original.size;
    const anchor = this.restore(restore, current);

    this.dispatch(restore, { ...current, to: restoredTo, writing: false });

    const insert = editor.state.tr;
    const placement = this.place(insert, anchor, this.written.trim());

    closeHistory(insert);
    this.dispatch(
      insert,
      { ...idleState, phase: 'settling', ...placement },
      true
    );

    this.abort = null;
    this.settleTimer = setTimeout(() => this.settle(), settleMs);
    this.setBusy(false);
  }

  private settle() {
    const editor = this.editor;

    this.settleTimer = null;

    if (!editor || this.state.phase !== 'settling') return;

    this.dispatch(editor.state.tr, idleState);
  }

  private fail(message: string) {
    const editor = this.editor;
    const current = this.state;

    this.stopRun();

    if (!editor || current.phase !== 'working') return;

    const tr = editor.state.tr;
    const restoredTo = current.from + this.original.size;

    this.restore(tr, current);
    this.dispatch(tr, {
      ...current,
      phase: 'error',
      to: restoredTo,
      writing: false,
      message,
    });
    this.setBusy(false);
  }

  private restore(tr: Transaction, current: InlinePromptState): number {
    tr.replace(current.from, current.to, this.original);
    this.original = Slice.empty;

    return current.from + current.offset;
  }

  private place(tr: Transaction, anchor: number, markdown: string) {
    const content = this.parse(markdown);

    if (!content.size) {
      return { from: anchor, to: anchor, offset: 0 };
    }

    const placement = placeReply(tr, anchor, content);

    this.original = placement.replaced;

    return {
      from: placement.from,
      to: placement.to,
      offset: placement.offset,
    };
  }

  private parse(markdown: string): Fragment {
    const editor = this.editor;

    if (!editor || !markdown) return Fragment.empty;

    try {
      return editor.schema.nodeFromJSON(this.host.toDocument(markdown)).content;
    } catch {
      const paragraph = editor.schema.nodes['paragraph'].create(
        null,
        editor.schema.text(markdown)
      );

      return Fragment.from(paragraph);
    }
  }

  private stopRun() {
    this.abort?.abort();
    this.abort = null;

    this.typewriter?.discard();

    if (this.settleTimer) {
      clearTimeout(this.settleTimer);
      this.settleTimer = null;
    }
  }

  private dispatch(
    tr: Transaction,
    state: InlinePromptState,
    recordHistory = false
  ) {
    const editor = this.editor;

    if (!editor || editor.isDestroyed) return;

    tr.setMeta(inlinePromptKey, state);

    if (!recordHistory) {
      tr.setMeta('addToHistory', false);
    }

    editor.view.dispatch(tr);
  }

  private setBusy(busy: boolean) {
    if (this.busy === busy) return;

    this.busy = busy;
    this.host.busyChanged(busy);
  }
}

export function createInlinePromptExtension(prompt: InlinePrompt) {
  return Extension.create({
    name: 'inlinePrompt',
    // Ahead of the keymaps, so Enter sends the prompt rather than splitting the paragraph.
    priority: 1000,

    addProseMirrorPlugins() {
      prompt.attach(this.editor);

      return [createInlinePromptPlugin(prompt)];
    },
  });
}

function createInlinePromptPlugin(prompt: InlinePrompt): Plugin {
  return new Plugin<InlinePromptState>({
    key: inlinePromptKey,

    state: {
      init: () => idleState,
      apply: (tr, value) => {
        const next = tr.getMeta(inlinePromptKey) as
          InlinePromptState | undefined;

        if (next) return next;

        if (!tr.docChanged || value.phase === 'idle') return value;

        return mapState(value, tr.mapping);
      },
    },

    // While the reply is being written its range belongs to the assistant: edits elsewhere go through.
    filterTransaction: (tr, state) => {
      const current = inlinePromptKey.getState(state);
      const isOwn = !!tr.getMeta(inlinePromptKey);

      if (current?.phase !== 'working' || !tr.docChanged || isOwn) return true;

      return !touchesInside(tr.mapping, current);
    },

    props: {
      handleKeyDown: (view, event) => handleKeyDown(prompt, view, event),
      decorations: (state) => decorationsFor(prompt, state),
    },

    view: (editorView) => {
      const pointer = new WritingPointer(editorView);
      let wasComposing = false;

      return {
        destroy: () => pointer.destroy(),
        update: (view, previous) => {
          const before = inlinePromptKey.getState(previous) ?? idleState;
          const after = inlinePromptKey.getState(view.state) ?? idleState;
          const compositionEnded = wasComposing && !view.composing;

          pointer.follow(after);

          wasComposing = view.composing;

          if (before.phase !== 'idle' && after.phase === 'idle') {
            prompt.release();

            return;
          }

          if (after.phase === 'composing') {
            const { from, to } = view.state.selection;
            const leftPrompt = from < after.from || to > after.to;

            if (leftPrompt) {
              queueMicrotask(() => prompt.cancel());
            }

            return;
          }

          const docChanged = !previous.doc.eq(view.state.doc);
          const mayHaveTyped = docChanged || compositionEnded;

          if (after.phase !== 'idle' || view.composing || !mayHaveTyped) {
            return;
          }

          const trigger = prompt.trigger;

          if (!trigger) return;

          const end = triggerEndAt(view.state, trigger);
          const wasTypedBefore = triggerEndAt(previous, trigger) !== null;
          const isNewTrigger = end !== null && !wasTypedBefore;

          if (isNewTrigger) {
            queueMicrotask(() => prompt.open(end));
          }
        },
      };
    },
  });
}

function handleKeyDown(
  prompt: InlinePrompt,
  view: EditorView,
  event: KeyboardEvent
): boolean {
  const current = inlinePromptKey.getState(view.state) ?? idleState;
  const hasModifier = event.metaKey || event.ctrlKey || event.altKey;

  if (current.phase === 'idle' || event.isComposing) return false;

  if (event.key === 'Escape') {
    const isCancellable = current.phase !== 'settling';

    if (!isCancellable) return false;

    // Stops a surrounding dialog from closing on the same key.
    event.stopPropagation();
    prompt.cancel();

    return true;
  }

  if (current.phase !== 'composing') return false;

  if (event.key === 'Enter' && !event.shiftKey && !hasModifier) {
    prompt.submit();

    return true;
  }

  const isPromptEmpty = current.from === current.to;

  if (event.key === 'Backspace' && isPromptEmpty) {
    prompt.cancel();

    return true;
  }

  return false;
}

function mapState(
  value: InlinePromptState,
  mapping: Mapping
): InlinePromptState {
  if (value.phase === 'composing') {
    const from = mapping.mapResult(value.from, -1);
    const to = mapping.mapResult(value.to, 1);

    if (from.deletedAcross && to.deletedAcross) return idleState;

    return { ...value, from: from.pos, to: Math.max(from.pos, to.pos) };
  }

  // Mapped inward, so text typed right at either edge stays outside the range.
  const from = mapping.mapResult(value.from, 1);
  const to = mapping.mapResult(value.to, -1);

  if (from.deletedAcross || to.deletedAcross) return idleState;

  return { ...value, from: from.pos, to: Math.max(from.pos, to.pos) };
}

function touchesInside(mapping: Mapping, range: PromptRange): boolean {
  let { from, to } = range;
  let touches = false;

  for (const map of mapping.maps) {
    map.forEach((oldStart, oldEnd) => {
      if (oldStart < to && oldEnd > from) {
        touches = true;
      }
    });

    from = map.map(from, 1);
    to = Math.max(from, map.map(to, -1));
  }

  return touches;
}

function triggerEndAt(state: EditorState, trigger: string): number | null {
  const { selection } = state;
  const $cursor = selection.$from;
  const parent = $cursor.parent;

  if (!selection.empty || !parent.isTextblock || parent.type.spec.code) {
    return null;
  }

  const codeMark = state.schema.marks['code'];
  const inInlineCode = !!codeMark?.isInSet($cursor.marks());

  if (inInlineCode) return null;

  const textBefore = parent.textBetween(
    0,
    $cursor.parentOffset,
    undefined,
    '\ufffc'
  );
  const precedingText = textBefore.slice(0, -trigger.length);
  const endsWithTrigger = textBefore.endsWith(trigger);
  const startsWord = precedingText === '' || /\s$/.test(precedingText);

  return endsWithTrigger && startsWord ? $cursor.pos : null;
}

// A single paragraph joins the sentence at the cursor. Anything longer goes in as whole blocks:
// in place of an empty line, or before, after or between the halves of the line it was asked from.
function placeReply(
  tr: Transaction,
  anchor: number,
  content: Fragment
): Placement {
  const $anchor = tr.doc.resolve(anchor);
  const parent = $anchor.parent;
  const isTopLevel = $anchor.depth === 1;
  const isEmptyLine = parent.content.size === 0;
  const isSingleParagraph =
    content.childCount === 1 && content.firstChild?.type.name === 'paragraph';
  const joinsText = isSingleParagraph && (!isEmptyLine || !isTopLevel);

  if (joinsText) {
    const inline = new Slice(content, 1, 1);

    tr.replace(anchor, anchor, inline);

    return {
      from: anchor,
      to: anchor + inline.size,
      offset: 0,
      replaced: Slice.empty,
    };
  }

  if (!isTopLevel) {
    const steps = tr.steps.length;

    tr.replaceRange(anchor, anchor, openSlice(content));

    const mapping = tr.mapping.slice(steps);
    const from = mapping.map(anchor, -1);

    return {
      from,
      to: mapping.map(anchor, 1),
      offset: anchor - from,
      replaced: Slice.empty,
    };
  }

  const before = $anchor.before();
  const after = $anchor.after();
  const blocks = (from: number, offset: number, replaced = Slice.empty) => {
    return { from, to: from + content.size, offset, replaced };
  };

  if (isEmptyLine) {
    const replaced = tr.doc.slice(before, after);

    tr.replaceWith(before, after, content);

    return blocks(before, 1, replaced);
  }

  if ($anchor.parentOffset === 0) {
    tr.insert(before, content);

    return blocks(before, 1);
  }

  if ($anchor.parentOffset === parent.content.size) {
    tr.insert(after, content);

    return blocks(after, -1);
  }

  tr.split(anchor);
  tr.insert(anchor + 1, content);

  return {
    from: anchor,
    to: anchor + content.size + 2,
    offset: 0,
    replaced: Slice.empty,
  };
}

function closeOpenMarks(markdown: string): string {
  const fences = markdown.match(/^```/gm)?.length ?? 0;
  const isInCodeBlock = fences % 2 === 1;

  if (isInCodeBlock) return markdown;

  const line = markdown.slice(markdown.lastIndexOf('\n') + 1);
  const open = inlineMarks
    .map((mark) => ({ mark, at: lastOpening(line, mark) }))
    .filter((entry) => entry.at >= 0)
    .sort((a, b) => b.at - a.at);

  if (!open.length) return markdown;

  const body = markdown.trimEnd();
  const trailing = markdown.slice(body.length);
  const closing = open.map((entry) => entry.mark).join('');

  return `${body}${closing}${trailing}`;
}

function lastOpening(line: string, mark: string): number {
  const positions: number[] = [];
  let index = line.indexOf(mark);

  while (index >= 0) {
    positions.push(index);
    index = line.indexOf(mark, index + mark.length);
  }

  const isOpen = positions.length % 2 === 1;

  return isOpen ? positions[positions.length - 1] : -1;
}

function openSlice(content: Fragment): Slice {
  const openStart = content.firstChild?.isTextblock ? 1 : 0;
  const openEnd = content.lastChild?.isTextblock ? 1 : 0;

  return new Slice(content, openStart, openEnd);
}

function decorationsFor(
  prompt: InlinePrompt,
  state: EditorState
): DecorationSet | null {
  const current = inlinePromptKey.getState(state) ?? idleState;
  const decorations = widgetsFor(prompt, current);

  if (!decorations.length) return null;

  return DecorationSet.create(state.doc, decorations);
}

function widgetsFor(
  prompt: InlinePrompt,
  current: InlinePromptState
): Decoration[] {
  const anchor = current.from + current.offset;

  switch (current.phase) {
    case 'composing': {
      const isEmpty = current.from === current.to;
      const text = isEmpty
        ? []
        : [
            Decoration.inline(current.from, current.to, {
              class: 'editor-inline-prompt',
            }),
          ];

      return [
        Decoration.widget(current.from, () => badge(), {
          side: -1,
          key: 'inline-prompt-badge',
        }),
        ...text,
        Decoration.widget(current.to, () => hint(prompt, isEmpty), {
          side: 1,
          key: `inline-prompt-hint-${isEmpty}`,
          ignoreSelection: true,
        }),
      ];
    }
    case 'working':
      if (current.writing) return [];

      return [
        Decoration.widget(anchor, () => thinking(prompt), {
          side: -1,
          key: 'inline-prompt-thinking',
          ignoreSelection: true,
        }),
      ];
    case 'error':
      return [
        Decoration.widget(anchor, () => failure(prompt, current.message), {
          side: -1,
          key: `inline-prompt-error-${current.message}`,
          ignoreSelection: true,
        }),
      ];
    default:
      return [];
  }
}

// The end of the reply. Where it split a line, its range ends inside the second half.
function caretAt(doc: ProseMirrorNode, position: number): number {
  const $position = doc.resolve(Math.min(position, doc.content.size));
  const isLineStart = $position.depth > 0 && $position.parentOffset === 0;
  const end = isLineStart ? $position.before() : $position.pos;

  return Selection.near(doc.resolve(end), -1).from;
}

// The gap after the icon is padding on its wrapper rather than a margin: an empty prompt's caret
// sits against the wrapper's edge, and a margin falls outside it.
function badge(): HTMLElement {
  const element = span('editor-inline-prompt-badge');
  const icon = span('editor-inline-prompt-badge-icon');

  icon.innerHTML = sparklesIcon;
  element.append(icon);

  return element;
}

function hint(prompt: InlinePrompt, isEmpty: boolean): HTMLElement {
  const labels = prompt.labels;
  const element = span('editor-inline-prompt-hint');

  if (isEmpty) {
    element.append(
      span('editor-inline-prompt-placeholder', labels.placeholder)
    );
  }

  element.append(
    button(labels.send, () => prompt.submit()),
    button(labels.cancel, () => prompt.cancel())
  );

  return element;
}

function thinking(prompt: InlinePrompt): HTMLElement {
  const element = span('editor-inline-prompt-status');
  const spinner = span('editor-inline-prompt-spinner');
  const elapsed = span('editor-inline-prompt-elapsed');

  spinner.innerHTML = spinnerIcon;
  element.setAttribute('role', 'status');
  element.append(
    spinner,
    span('editor-inline-prompt-status-text', prompt.labels.thinking),
    elapsed,
    button(prompt.labels.cancel, () => prompt.cancel())
  );

  countUp(element, elapsed, prompt.workingSince);

  return element;
}

// Runs until the widget leaves the document, which it does as soon as the reply starts.
function countUp(widget: HTMLElement, target: HTMLElement, since: number) {
  const show = () => {
    const elapsedMs = performance.now() - since;
    const hasStarted = elapsedMs >= minimumReportedMs;

    target.textContent = hasStarted ? formatElapsed(elapsedMs) : '';
  };

  show();

  const timer = setInterval(() => {
    if (!widget.isConnected) {
      clearInterval(timer);

      return;
    }

    show();
  }, elapsedRefreshMs);
}

function failure(prompt: InlinePrompt, message: string | null): HTMLElement {
  const labels = prompt.labels;
  const element = span('editor-inline-prompt-status');

  element.dataset['tone'] = 'error';
  element.setAttribute('role', 'alert');
  element.append(
    span('editor-inline-prompt-status-text', message ?? labels.failed),
    button(labels.retry, () => prompt.retry()),
    button(labels.dismiss, () => prompt.cancel())
  );

  return element;
}

// Floats over the editor rather than sitting in the text: a widget in the reply is rebuilt with
// every frame of it, so it jumps and restarts its animation where this one glides.
class WritingPointer {
  private readonly element = document.createElement('div');
  private isVisible = false;
  private lineTop: number | null = null;
  private isFollowing = false;

  // Scrolling by hand while a reply is written means reading something else, so stop following.
  private readonly stopFollowing = () => {
    this.isFollowing = false;
  };

  constructor(private readonly view: EditorView) {
    const avatar = span('editor-inline-prompt-avatar');

    avatar.innerHTML = sparklesIcon;
    this.element.className = 'editor-inline-prompt-pointer';
    this.element.setAttribute('aria-hidden', 'true');
    this.element.append(avatar);
    view.dom.parentElement?.append(this.element);
  }

  follow(current: InlinePromptState) {
    const isWriting = current.phase === 'working' && current.writing;

    if (!isWriting) {
      this.hide();

      return;
    }

    const host = this.element.offsetParent;

    if (!host) return;

    const position = caretAt(this.view.state.doc, current.to);
    const caret = this.view.coordsAtPos(position, -1);
    const origin = host.getBoundingClientRect();
    const x = caret.left - origin.left + host.scrollLeft;
    const y = caret.top - origin.top + host.scrollTop;
    const isAppearing = !this.isVisible;
    const isNewLine = this.lineTop !== null && this.lineTop !== y;

    this.lineTop = y;

    // Glides along a line, but jumps when it appears or wraps: gliding there would sweep it back
    // across the text.
    if (isAppearing || isNewLine) {
      this.element.style.transition = 'opacity 0.4s ease-out';
    }

    this.element.style.transform = `translate(${x}px, ${y}px)`;

    if (isAppearing || isNewLine) {
      void this.element.offsetWidth;
      this.element.style.transition = '';
    }

    if (isAppearing) {
      this.element.dataset['visible'] = '';
      this.isVisible = true;
      this.startFollowing();
    }

    if (this.isFollowing) {
      keepInView(this.view.dom, caret.bottom);
    }
  }

  destroy() {
    this.endFollowing();
    this.element.remove();
  }

  private hide() {
    if (!this.isVisible) return;

    this.isVisible = false;
    this.lineTop = null;
    this.endFollowing();
    delete this.element.dataset['visible'];
  }

  private startFollowing() {
    this.isFollowing = true;
    window.addEventListener('wheel', this.stopFollowing, { passive: true });
    window.addEventListener('touchmove', this.stopFollowing, { passive: true });
  }

  private endFollowing() {
    this.isFollowing = false;
    window.removeEventListener('wheel', this.stopFollowing);
    window.removeEventListener('touchmove', this.stopFollowing);
  }
}

function keepInView(start: Element, lineBottom: number) {
  let bottom = lineBottom + followMarginPx;

  for (
    let element = start.parentElement;
    element;
    element = element.parentElement
  ) {
    if (!isScrollable(element)) continue;

    const overflow = bottom - element.getBoundingClientRect().bottom;

    if (overflow <= 0) continue;

    const before = element.scrollTop;

    element.scrollTop = before + overflow;
    bottom -= element.scrollTop - before;
  }

  const pageOverflow = bottom - window.innerHeight;

  if (pageOverflow > 0) {
    window.scrollBy(0, pageOverflow);
  }
}

function isScrollable(element: Element): boolean {
  const { overflowY } = getComputedStyle(element);
  const scrolls = overflowY === 'auto' || overflowY === 'scroll';

  return scrolls && element.scrollHeight > element.clientHeight;
}

function span(className: string, text?: string): HTMLElement {
  const element = document.createElement('span');

  element.className = className;
  element.contentEditable = 'false';

  if (text) {
    element.textContent = text;
  }

  return element;
}

// mousedown is cancelled so the editor keeps focus and the prompt keeps its selection.
function button(label: string, action: () => void): HTMLButtonElement {
  const element = document.createElement('button');

  element.type = 'button';
  element.className = 'editor-inline-prompt-action';
  element.textContent = label;
  element.addEventListener('mousedown', (event) => event.preventDefault());
  element.addEventListener('click', (event) => {
    event.preventDefault();
    action();
  });

  return element;
}

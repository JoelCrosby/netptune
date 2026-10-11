import {
  afterNextRender,
  Component,
  computed,
  effect,
  ElementRef,
  inject,
  Injector,
  input,
  OnDestroy,
  output,
  signal,
  untracked,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { reloadToken } from '@core/util/signals';
import { AnyExtension, Editor, JSONContent } from '@tiptap/core';
import { BubbleMenu } from '@tiptap/extension-bubble-menu';
import { FloatingMenu } from '@tiptap/extension-floating-menu';
import { Image } from '@tiptap/extension-image';
import { TaskItem, TaskList } from '@tiptap/extension-list';
import { Placeholder } from '@tiptap/extensions';
import type { EditorView } from '@tiptap/pm/view';
import { StarterKit } from '@tiptap/starter-kit';
import { Subject } from 'rxjs';
import { debounceTime } from 'rxjs/operators';
import { AbstractFormValueControl } from '../abstract-form-value-control';
import { cn } from '../button/button.variants';
import { EditorBubbleMenuComponent } from './editor-bubble-menu.component';
import { SuggestionMenuComponent } from '../suggestion-menu/suggestion-menu.component';
import { Callout } from './editor-callout';
import { EditorCommand } from './editor-commands';
import { createSlashExtension, SlashMenu } from './editor-slash';
import {
  documentToMarkdown,
  markdownToDocument,
  MarkdownSyntax,
} from './editor-content';
import { EditorFloatingMenuComponent } from './editor-floating-menu.component';

const saveDebounceMs = 2400;

export type EditorAppearance = 'boxed' | 'flat' | 'document';

export type EditorMode = 'rich' | 'markdown';

export interface EditorUpload {
  name: string;
  uri: string;
}

export type EditorUploader = (file: File) => Promise<EditorUpload | null>;

@Component({
  selector: 'app-editor',
  imports: [
    EditorBubbleMenuComponent,
    EditorFloatingMenuComponent,
    SuggestionMenuComponent,
  ],
  template: `
    <div
      class="editor w-full rounded"
      [class.hidden]="mode() === 'markdown'"
      #editorHost></div>

    @if (mode() === 'markdown') {
      <textarea
        class="text-foreground/75 field-sizing-content min-h-[50vh] w-full resize-none border-0 bg-transparent p-0 font-mono text-[13px] leading-[1.75] whitespace-pre-wrap outline-none"
        spellcheck="false"
        [value]="value()"
        [readOnly]="isReadOnly()"
        [attr.aria-label]="markdownLabel"
        (input)="onMarkdownInput(markdownInput.value)"
        #markdownInput></textarea>
    }

    @if (slash.open() && mode() === 'rich') {
      <app-suggestion-menu
        class="fixed z-50"
        [class.-translate-y-full]="slash.position().above"
        [style.left.px]="slash.position().left"
        [style.top.px]="slash.position().top"
        [ariaLabel]="slashLabel"
        [emptyLabel]="slashEmptyLabel"
        [items]="slash.items()"
        [activeIndex]="slash.activeIndex()"
        (picked)="slash.pick($event)"
        (hovered)="slash.activeIndex.set($event)" />
    }

    <app-editor-bubble-menu [editor]="editor()" [revision]="revision()" />

    <app-editor-floating-menu
      [editor]="editor()"
      [revision]="revision()"
      [canUpload]="!!uploader()"
      (fileRequested)="filePicker.click()" />

    <input
      #filePicker
      type="file"
      class="hidden"
      multiple
      (change)="onFilesPicked(filePicker)" />
  `,
  host: {
    class: 'relative flex overflow-visible',
    '[class]': 'appearanceClass()',
    '(focusout)': 'onFocusOut($event)',
  },
})
export class EditorComponent
  extends AbstractFormValueControl
  implements OnDestroy
{
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

  readonly el = viewChild.required<ElementRef<HTMLElement>>('editorHost');

  private readonly bubbleMenu = viewChild.required(EditorBubbleMenuComponent);
  private readonly floatingMenu = viewChild.required(
    EditorFloatingMenuComponent
  );

  readonly placeholder = input('');
  readonly isReadOnly = input(false);
  readonly appearance = input<EditorAppearance>('boxed');
  readonly mode = input<EditorMode>('rich');
  readonly hostClass = input('');
  readonly uploader = input<EditorUploader | null>(null);
  // Read once, when the editor is created.
  readonly extensions = input<readonly AnyExtension[]>([]);
  readonly syntax = input<MarkdownSyntax | undefined>(undefined);
  // What the / menu offers. Whether the menu exists at all is read once, when the editor is created;
  // the list itself follows the input.
  readonly commands = input<readonly EditorCommand[]>([]);

  protected readonly slash = new SlashMenu();
  protected readonly slashLabel = $localize`:Accessible label for the menu of blocks that typing / offers:Insert a block`;
  protected readonly slashEmptyLabel = $localize`:Shown in the slash command menu when nothing matches what was typed:No matching blocks`;

  protected readonly appearanceClass = computed(() => {
    switch (this.appearance()) {
      case 'document':
        return cn('flex-col', this.hostClass());
      case 'flat':
        return cn('-mx-1 max-h-[600px] rounded-lg px-1', this.hostClass());
      default:
        return cn(
          'bg-form-field-background mb-4 border-foreground/30 mt-2 max-h-[600px] rounded-sm border-2 px-4 py-1',
          this.hostClass()
        );
    }
  });

  protected readonly markdownLabel = $localize`:Accessible label for the raw markdown source of a document:Markdown source`;

  readonly loaded = output();
  readonly saved = output<string>();

  readonly finalSave = input<((value: string) => void) | null>(null);
  readonly injector = inject(Injector);

  readonly editor = signal<Editor | null>(null);

  // bumped on every transaction so the menus can recompute which marks and nodes
  // are active at the cursor
  protected readonly revision = reloadToken();

  private editorValue: string | null = null;

  // the markdown as of the last value handed to a consumer, so a save can tell a
  // real edit from the editor normalising the markdown it was given
  private savedContent: string | null = null;
  private destroyed = false;
  private readonly changed = new Subject<void>();
  private readonly markdownChanged = new Subject<void>();
  private readonly emitSaved = (value: string) => this.saved.emit(value);

  // the source as of the last markdown-mode save, which bypasses the rich editor
  private markdownSaved: string | null = null;
  private previousMode: EditorMode = 'rich';

  constructor() {
    super();

    this.changed
      .pipe(debounceTime(saveDebounceMs), takeUntilDestroyed())
      .subscribe(() => this.persist(this.emitSaved));

    this.markdownChanged
      .pipe(debounceTime(saveDebounceMs), takeUntilDestroyed())
      .subscribe(() => this.persistMarkdown(this.emitSaved));

    afterNextRender(() => {
      this.createEditor();

      effect(
        () => {
          const value = this.value();
          const editingSource = untracked(this.mode) === 'markdown';

          if (value === this.editorValue || editingSource) return;

          this.setValue(value);
        },
        { injector: this.injector }
      );

      effect(
        () => {
          const mode = this.mode();

          untracked(() => this.onModeChanged(mode));
        },
        { injector: this.injector }
      );

      effect(
        () => {
          const commands = this.commands();

          this.slash.setCommands(commands);
        },
        { injector: this.injector }
      );

      effect(
        () => {
          const readOnly = this.isReadOnly();

          this.editor()?.setEditable(!readOnly, false);
        },
        { injector: this.injector }
      );
    });
  }

  ngOnDestroy() {
    const editor = this.editor();
    const deliver = this.finalSave();

    this.destroyed = true;

    if (!editor) return;

    if (deliver) {
      this.persistCurrentMode(deliver);
    }

    editor.destroy();
  }

  // hands the current document over when it differs from the last save
  private persist(deliver: (value: string) => void) {
    const serialised = this.takePendingEdit();

    if (serialised === null) return;

    // writing to the model would emit its change output, which angular has
    // already torn down by the time a final save runs
    if (!this.destroyed) {
      this.value.set(serialised);
    }

    deliver(serialised);
  }

  // returns the document when it holds an edit that has not been handed over
  // yet, marking it as saved. null when the content matches the last save
  private takePendingEdit(): string | null {
    const editor = this.editor();

    if (!editor || editor.isDestroyed) return null;

    const serialised = this.toMarkdown(editor.getJSON());

    if (serialised === this.savedContent) return null;

    this.savedContent = serialised;
    this.editorValue = serialised;

    return serialised;
  }

  onFocusOut(event: FocusEvent) {
    const next = event.relatedTarget as Node | null;
    const holder = this.host.nativeElement;

    if (next && holder.contains(next)) return;

    this.persistCurrentMode(this.emitSaved);
  }

  scrollToHeading(index: number) {
    const headings = this.el().nativeElement.querySelectorAll(
      'h1, h2, h3, h4, h5, h6'
    );

    headings
      .item(index)
      ?.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }

  protected onMarkdownInput(source: string) {
    this.value.set(source);
    this.markdownChanged.next();
  }

  private onModeChanged(mode: EditorMode) {
    const previous = this.previousMode;

    this.previousMode = mode;

    if (mode === previous) return;

    if (mode === 'markdown') {
      this.persist(this.emitSaved);
      this.markdownSaved = this.value();

      return;
    }

    this.persistMarkdown(this.emitSaved);
    this.setValue(this.value());
  }

  private persistCurrentMode(deliver: (value: string) => void) {
    if (this.mode() === 'markdown') {
      this.persistMarkdown(deliver);

      return;
    }

    this.persist(deliver);
  }

  private persistMarkdown(deliver: (value: string) => void) {
    const source = this.value();

    if (source === this.markdownSaved) return;

    this.markdownSaved = source;
    deliver(source);
  }

  setValue(value?: string) {
    const editor = this.editor();

    if (!editor) return;

    // an incoming value replaces what is on screen, so hand over any edit the
    // editor still holds before it goes. content the editor was given and never
    // touched is not an edit, so a value arriving from elsewhere is not written
    // straight back over the update that produced it
    const pending = this.takePendingEdit();

    if (pending !== null) {
      this.saved.emit(pending);
    }

    this.editorValue = value ?? null;

    editor.commands.setContent(this.toDocument(value), {
      emitUpdate: false,
    });

    this.savedContent = this.toMarkdown(editor.getJSON());
  }

  private createEditor() {
    const editor = new Editor({
      element: this.el().nativeElement,
      content: this.toDocument(this.value()),
      editable: !this.isReadOnly(),
      extensions: this.allExtensions(),
      editorProps: {
        attributes: this.contentAttributes(),
        handlePaste: (_view, event) => this.onPaste(event),
        handleDrop: (view, event, _slice, moved) => {
          return this.onDrop(view, event as DragEvent, moved);
        },
      },
      onCreate: () => this.loaded.emit(),
      onUpdate: ({ editor: updated }) => this.onUpdate(updated),
      onTransaction: () => this.revision.bump(),
    });

    this.editorValue = this.value();
    this.savedContent = this.toMarkdown(editor.getJSON());

    this.editor.set(editor);
  }

  private allExtensions() {
    const slash = this.commands().length
      ? [createSlashExtension(this.slash)]
      : [];

    return [
      ...slash,
      ...this.extensions(),
      Callout,
      StarterKit.configure({
        link: { openOnClick: false, autolink: true, defaultProtocol: 'https' },
        codeBlock: { languageClassPrefix: 'language-' },
        underline: false,
      }),
      TaskList,
      TaskItem.configure({ nested: true }),
      Image.configure({ allowBase64: false }),
      Placeholder.configure({
        showOnlyCurrent: false,
        placeholder: ({ pos }) => (pos === 0 ? this.placeholder() : ''),
      }),
      BubbleMenu.configure({
        element: this.bubbleMenu().el.nativeElement,
        options: { placement: 'top', offset: 8 },
        shouldShow: ({ editor, state, from, to }) => {
          const selectedText = state.doc.textBetween(from, to).trim();

          return editor.isEditable && selectedText.length > 0;
        },
      }),
      FloatingMenu.configure({
        element: this.floatingMenu().el.nativeElement,
        options: { placement: 'bottom-start', offset: 8 },
        shouldShow: ({ editor, state }) => {
          const { $anchor, empty } = state.selection;
          const onEmptyParagraph =
            $anchor.parent.type.name === 'paragraph' &&
            $anchor.parent.content.size === 0;

          return (
            editor.isEditable && editor.isFocused && empty && onEmptyParagraph
          );
        },
      }),
    ];
  }

  private contentAttributes(): Record<string, string> {
    const contentClass = contentClassFor(this.appearance());
    const attributes: Record<string, string> = {
      class: `editor-content w-full outline-none ${contentClass}`,
      role: 'textbox',
      'aria-multiline': 'true',
    };

    const labelledBy = this.host.nativeElement.getAttribute('aria-labelledby');

    if (labelledBy) {
      attributes['aria-labelledby'] = labelledBy;
    }

    return attributes;
  }

  private onUpdate(editor: Editor) {
    const serialised = this.toMarkdown(editor.getJSON());

    this.editorValue = serialised;
    this.value.set(serialised);
    this.changed.next();
  }

  private onPaste(event: ClipboardEvent): boolean {
    const files = Array.from(event.clipboardData?.files ?? []);

    if (!files.length || !this.uploader()) return false;

    void this.uploadFiles(files);

    return true;
  }

  private onDrop(view: EditorView, event: DragEvent, moved: boolean): boolean {
    if (moved) return false;

    const files = Array.from(event.dataTransfer?.files ?? []);

    if (!files.length || !this.uploader()) return false;

    event.preventDefault();

    const coords = view.posAtCoords({
      left: event.clientX,
      top: event.clientY,
    });

    void this.uploadFiles(files, coords?.pos);

    return true;
  }

  protected onFilesPicked(picker: HTMLInputElement) {
    const files = Array.from(picker.files ?? []);

    picker.value = '';

    if (!files.length) return;

    void this.uploadFiles(files);
  }

  private async uploadFiles(files: File[], position?: number) {
    const uploader = this.uploader();

    if (!uploader) return;

    let insertAt = position;

    for (const file of files) {
      const upload = await uploader(file).catch(() => null);

      if (!upload) continue;

      insertAt = this.insertUpload(upload, file, insertAt);
    }
  }

  private insertUpload(
    upload: EditorUpload,
    file: File,
    position?: number
  ): number | undefined {
    const editor = this.editor();

    if (!editor || editor.isDestroyed) return position;

    const at = position ?? editor.state.selection.to;
    const content = file.type.startsWith('image/')
      ? { type: 'image', attrs: { src: upload.uri, alt: upload.name } }
      : {
          type: 'text',
          text: upload.name,
          marks: [{ type: 'link', attrs: { href: upload.uri } }],
        };

    editor.chain().focus().insertContentAt(at, content).run();

    return editor.state.selection.to;
  }

  private toDocument(markdown: string | null | undefined): JSONContent {
    return markdownToDocument(markdown, this.syntax());
  }

  private toMarkdown(document: JSONContent): string {
    return documentToMarkdown(document, this.syntax());
  }
}

function contentClassFor(appearance: EditorAppearance): string {
  switch (appearance) {
    case 'document':
      return 'editor-document min-h-[50vh]';
    case 'flat':
      return 'min-h-6';
    default:
      return 'min-h-[100px]';
  }
}

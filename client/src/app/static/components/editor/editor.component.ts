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
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { UploadResponse } from '@core/models/upload-result';
import { StorageService } from '@core/services/storage.service';
import { unwrapClientResponse } from '@core/util/rxjs-operators';
import { Editor } from '@tiptap/core';
import { BubbleMenu } from '@tiptap/extension-bubble-menu';
import { FloatingMenu } from '@tiptap/extension-floating-menu';
import { Image } from '@tiptap/extension-image';
import { TaskItem, TaskList } from '@tiptap/extension-list';
import { Placeholder } from '@tiptap/extensions';
import type { EditorView } from '@tiptap/pm/view';
import { StarterKit } from '@tiptap/starter-kit';
import { firstValueFrom, Subject } from 'rxjs';
import { debounceTime } from 'rxjs/operators';
import { AbstractFormValueControl } from '../abstract-form-value-control';
import { cn } from '../button/button.variants';
import { EditorBubbleMenuComponent } from './editor-bubble-menu.component';
import { documentToMarkdown, markdownToDocument } from './editor-content';
import { EditorFloatingMenuComponent } from './editor-floating-menu.component';

const saveDebounceMs = 750;

export type EditorAppearance = 'boxed' | 'flat';

@Component({
  selector: 'app-editor',
  imports: [EditorBubbleMenuComponent, EditorFloatingMenuComponent],
  template: `
    <div class="editor w-full rounded" #editorHost></div>

    <app-editor-bubble-menu [editor]="editor()" [revision]="revision()" />

    <app-editor-floating-menu
      [editor]="editor()"
      [revision]="revision()"
      (fileRequested)="filePicker.click()" />

    <input
      #filePicker
      type="file"
      class="hidden"
      multiple
      (change)="onFilesPicked(filePicker)" />
  `,
  host: {
    class: 'relative flex overflow-y-auto max-h-[600px]',
    '[class]': 'appearanceClass()',
    '(focusout)': 'onFocusOut($event)',
  },
})
export class EditorComponent
  extends AbstractFormValueControl
  implements OnDestroy
{
  private readonly storage = inject(StorageService);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);

  readonly el = viewChild.required<ElementRef<HTMLElement>>('editorHost');

  private readonly bubbleMenu = viewChild.required(EditorBubbleMenuComponent);
  private readonly floatingMenu = viewChild.required(
    EditorFloatingMenuComponent
  );

  readonly placeholder = input('');
  readonly isReadOnly = input(false);

  readonly appearance = input<EditorAppearance>('boxed');

  readonly hostClass = input('');

  protected readonly appearanceClass = computed(() => {
    if (this.appearance() !== 'flat') {
      return cn(
        'bg-form-field-background mb-4 border-foreground/30 mt-2 rounded-sm border-2 px-4 py-1',
        this.hostClass()
      );
    }

    return cn('-mx-1 rounded-lg px-1', this.hostClass());
  });

  readonly loaded = output();
  readonly saved = output<string>();

  // receives the editor's final content while the component is torn down. this
  // cannot go through `saved`: angular removes output listeners as part of the
  // same teardown. for the same reason the callback must not read state that the
  // teardown clears.
  readonly finalSave = input<((value: string) => void) | null>(null);

  readonly injector = inject(Injector);

  readonly editor = signal<Editor | null>(null);

  // bumped on every transaction so the menus can recompute which marks and nodes
  // are active at the cursor
  protected readonly revision = signal(0);

  private editorValue: string | null = null;

  // the markdown as of the last value handed to a consumer, so a save can tell a
  // real edit from the editor normalising the markdown it was given
  private savedContent: string | null = null;
  private deliveredValue: string | null = null;
  private destroyed = false;
  private readonly changed = new Subject<void>();
  private readonly emitSaved = (value: string) => this.saved.emit(value);

  constructor() {
    super();

    this.changed
      .pipe(debounceTime(saveDebounceMs), takeUntilDestroyed())
      .subscribe(() => this.persist(this.emitSaved));

    afterNextRender(() => {
      this.createEditor();

      effect(
        () => {
          const value = this.value();

          if (value === this.editorValue) return;

          this.setValue(value);
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
      this.persist(deliver);
    }

    editor.destroy();
  }

  // hands the current document over when it differs from the last save
  private persist(deliver: (value: string) => void) {
    const editor = this.editor();

    if (!editor || editor.isDestroyed) return;

    const serialised = documentToMarkdown(editor.getJSON());

    if (serialised === this.savedContent) return;

    this.savedContent = serialised;
    this.deliveredValue = serialised;
    this.editorValue = serialised;

    // writing to the model would emit its change output, which angular has
    // already torn down by the time a final save runs
    if (!this.destroyed) {
      this.value.set(serialised);
    }

    deliver(serialised);
  }

  onFocusOut(event: FocusEvent) {
    const next = event.relatedTarget as Node | null;
    const holder = this.host.nativeElement;

    if (next && holder.contains(next)) return;

    this.persist(this.emitSaved);
  }

  setValue(value?: string) {
    const editor = this.editor();

    if (!editor) return;

    // an incoming value replaces what is on screen, so hand over the latest
    // content the editor produced before it goes
    if (this.editorValue !== null && this.editorValue !== this.deliveredValue) {
      this.deliveredValue = this.editorValue;
      this.saved.emit(this.editorValue);
    }

    this.editorValue = value ?? null;

    editor.commands.setContent(markdownToDocument(value), {
      emitUpdate: false,
    });

    this.savedContent = documentToMarkdown(editor.getJSON());
  }

  private createEditor() {
    const editor = new Editor({
      element: this.el().nativeElement,
      content: markdownToDocument(this.value()),
      editable: !this.isReadOnly(),
      extensions: this.extensions(),
      editorProps: {
        attributes: this.contentAttributes(),
        handlePaste: (_view, event) => this.onPaste(event),
        handleDrop: (view, event, _slice, moved) => {
          return this.onDrop(view, event as DragEvent, moved);
        },
      },
      onCreate: () => this.loaded.emit(),
      onUpdate: ({ editor: updated }) => this.onUpdate(updated),
      onTransaction: () => this.revision.update((value) => value + 1),
    });

    this.editorValue = this.value();
    this.savedContent = documentToMarkdown(editor.getJSON());

    this.editor.set(editor);
  }

  private extensions() {
    return [
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
    const minHeight =
      this.appearance() === 'flat' ? 'min-h-6' : 'min-h-[100px]';
    const attributes: Record<string, string> = {
      class: `editor-content w-full outline-none ${minHeight}`,
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
    const serialised = documentToMarkdown(editor.getJSON());

    this.editorValue = serialised;
    this.value.set(serialised);
    this.changed.next();
  }

  private onPaste(event: ClipboardEvent): boolean {
    const files = Array.from(event.clipboardData?.files ?? []);

    if (!files.length) return false;

    void this.uploadFiles(files);

    return true;
  }

  private onDrop(view: EditorView, event: DragEvent, moved: boolean): boolean {
    if (moved) return false;

    const files = Array.from(event.dataTransfer?.files ?? []);

    if (!files.length) return false;

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
    let insertAt = position;

    for (const file of files) {
      const upload = await this.uploadFile(file);

      if (!upload) continue;

      insertAt = this.insertUpload(upload, file, insertAt);
    }
  }

  private async uploadFile(file: File): Promise<UploadResponse | null> {
    return firstValueFrom(
      this.storage.uploadMedia(file).pipe(unwrapClientResponse())
    ).catch(() => null);
  }

  private insertUpload(
    upload: UploadResponse,
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
}

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
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { StorageService } from '@core/services/storage.service';
import { unwrapClientResponse } from '@core/util/rxjs-operators';
import Attaches from '@editorjs/attaches';
import Checklist from '@editorjs/checklist';
import Code from '@editorjs/code';
import type { LogLevels, OutputData } from '@editorjs/editorjs';
import EditorJS from '@editorjs/editorjs';
import Embed from '@editorjs/embed';
import Header from '@editorjs/header';
import ImageTool from '@editorjs/image';
import InlineCode from '@editorjs/inline-code';
import Link from '@editorjs/link';
import List from '@editorjs/list';
import Marker from '@editorjs/marker';
import Underline from '@editorjs/underline';
import { environment } from '@env/environment';
import { firstValueFrom, Subject } from 'rxjs';
import { debounceTime } from 'rxjs/operators';
import { AbstractFormValueControl } from '../abstract-form-value-control';
import { cn } from '../button/button.variants';

const saveDebounceMs = 750;

export type EditorAppearance = 'boxed' | 'flat';

@Component({
  selector: 'app-editor',
  template: ` <div class="editor w-full rounded" #editorJs></div> `,
  host: {
    class: 'flex overflow-y-auto max-h-[600px]',
    '[class]': 'appearanceClass()',
    '(focusout)': 'onFocusOut($event)',
  },
})
export class EditorComponent
  extends AbstractFormValueControl
  implements OnDestroy
{
  private storage = inject(StorageService);

  readonly el = viewChild.required<ElementRef>('editorJs');

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

    const hover = this.isReadOnly() ? '' : 'hover:bg-hover transition-colors';

    return cn('-mx-1 rounded-lg px-1', hover, this.hostClass());
  });

  readonly loaded = output();
  readonly saved = output<string>();

  // receives the editor's final content while the component is torn down. this
  // cannot go through `saved`: angular removes output listeners as part of the
  // same teardown, and reading the content back out of editorjs is asynchronous,
  // so the emit would land after the listener is gone. for the same reason the
  // callback must not read state that the teardown clears.
  readonly finalSave = input<((value: string) => void) | null>(null);

  readonly injector = inject(Injector);

  editor!: EditorJS;

  private editorValue: string | null = null;

  // the blocks as of the last value handed to a consumer. editorjs stamps every
  // serialisation with a fresh `time`, so the blocks alone say whether the
  // content actually changed.
  private savedBlocks: string | null = null;
  private deliveredValue: string | null = null;
  private destroyed = false;
  private readonly changed = new Subject<void>();
  private readonly emitSaved = (value: string) => this.saved.emit(value);

  constructor() {
    super();

    this.changed
      .pipe(debounceTime(saveDebounceMs), takeUntilDestroyed())
      .subscribe(() => void this.persist(this.emitSaved));

    afterNextRender(() => {
      effect(
        () => {
          const value = this.value();

          if (value === this.editorValue) return;

          this.setValue(value);
        },
        { injector: this.injector }
      );
    });
  }

  ngOnDestroy() {
    const editor = this.editor;
    const deliver = this.finalSave();

    this.destroyed = true;

    if (!editor) return;

    if (!deliver) {
      editor.destroy?.();
      return;
    }

    // the editor has to outlive this hook long enough to read its content back
    void this.persist(deliver).finally(() => editor.destroy?.());
  }

  // serialises what is on screen right now and hands it over when it differs
  // from the last save. editorjs batches its own change events, so the value
  // carried by the last change can be several hundred milliseconds behind the
  // user and is never enough on its own.
  private async persist(deliver: (value: string) => void) {
    const editor = this.editor;

    if (!editor) return;

    const data = await editor.save().catch(() => null);

    if (!data) return;

    const blocks = JSON.stringify(data.blocks);

    if (blocks === this.savedBlocks) return;

    const serialised = JSON.stringify(data);

    this.savedBlocks = blocks;
    this.deliveredValue = serialised;
    this.editorValue = serialised;

    // writing to the model would emit its change output, which angular has
    // already torn down by the time a final save resolves
    if (!this.destroyed) {
      this.value.set(serialised);
    }

    deliver(serialised);
  }

  // records the editor's own serialisation of the content it was handed, so a
  // later save can tell a real edit from editorjs normalising what it loaded
  private async captureBaseline() {
    const editor = this.editor;

    if (!editor) return;

    const data = await editor.save().catch(() => null);

    if (!data) return;

    this.savedBlocks = JSON.stringify(data.blocks);
  }

  onFocusOut(event: FocusEvent) {
    const next = event.relatedTarget as Node | null;
    const holder = this.el().nativeElement as HTMLElement;

    if (next && holder.contains(next)) return;

    void this.persist(this.emitSaved);
  }

  setValue(value?: string) {
    // an incoming value replaces what is on screen, so hand over the latest
    // content the editor produced before the current instance goes
    if (this.editorValue !== null && this.editorValue !== this.deliveredValue) {
      this.deliveredValue = this.editorValue;
      this.saved.emit(this.editorValue);
    }

    this.editorValue = value ?? null;

    try {
      const parsed = value ? JSON.parse(value) : null;

      if (!parsed) throw Error('value not valid');

      const intialValue = parsed as OutputData;

      this.createEditor(intialValue);
    } catch {
      this.createEmptyEditor(value);
    }
  }

  createEmptyEditor(value?: string) {
    this.createEditor({
      time: Date.now(),
      blocks: [
        {
          data: {
            text: value ?? '',
          },
          type: 'paragraph',
        },
      ],
    });
  }

  createEditor(initialValue: OutputData | null = null) {
    if (this.editor) {
      this.editor.destroy();
    }

    const logLevel = environment.production
      ? ('ERROR' as LogLevels)
      : ('WARN' as LogLevels);

    this.editor = new EditorJS({
      logLevel: logLevel,
      placeholder: this.placeholder(),
      holder: this.el().nativeElement,
      minHeight: 100,
      readOnly: this.isReadOnly(),
      tools: {
        header: Header,
        list: List,
        code: Code,
        image: {
          class: ImageTool,
          config: {
            uploader: {
              uploadByFile: this.uploadFile.bind(this),
              uploadByUrl: this.uploadByUrl.bind(this),
            },
          },
        },
        checklist: {
          class: Checklist,
          inlineToolbar: true,
        },
        inlineCode: {
          class: InlineCode,
          shortcut: 'CMD+SHIFT+C',
        },
        marker: {
          class: Marker,
          shortcut: 'CMD+SHIFT+M',
        },
        embed: {
          class: Embed,
          config: {
            services: {
              youtube: true,
              coub: true,
            },
          },
        },
        underline: Underline,
        link: {
          class: Link,
          config: {
            endpoint: '/api/meta/uri-meta-info',
          },
        },
        attaches: {
          class: Attaches,
          config: {
            uploader: {
              uploadByFile: this.uploadFile.bind(this),
            },
          },
        },
      },
      data: initialValue || undefined,
      onReady: () => {
        void this.captureBaseline();
        this.loaded.emit();
      },
      onChange: () => {
        void this.editor.save().then((value) => {
          const serialised = JSON.stringify(value);

          this.editorValue = serialised;
          this.value.set(serialised);
          this.changed.next();
        });
      },
    });
  }

  async uploadFile(data: File) {
    const response = await firstValueFrom(
      this.storage.uploadMedia(data).pipe(unwrapClientResponse())
    ).catch(() => null);

    if (!response) {
      return { success: 0 };
    }

    // TODO: have to force an on change as the attaches
    // editorjs module does not emit an on change event

    setTimeout(
      () =>
        void this.editor.save().then((value) => {
          const serialised = JSON.stringify(value);

          this.editorValue = serialised;
          this.value.set(serialised);
          this.changed.next();
        }),
      0
    );

    return {
      success: 1,
      file: {
        url: response.uri,
        name: response.name,
        title: response.name,
        size: response.size,
      },
    };
  }

  async uploadByUrl(url: string) {
    return new Promise((res) => {
      res({
        success: 1,
        file: {
          url,
        },
      });
    });
  }
}

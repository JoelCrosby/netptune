import {
  afterNextRender,
  Component,
  ElementRef,
  inject,
  input,
  OnDestroy,
  output,
  viewChild,
} from '@angular/core';
import { StorageService } from '@core/services/storage.service';
import { unwrapClientReposne } from '@core/util/rxjs-operators';
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
import { firstValueFrom } from 'rxjs';
import { AbstractFormValueControl } from '../abstract-form-value-control';

@Component({
  selector: 'app-editor',
  template: `<div class="editor w-full rounded" #editorJs></div>`,
  host: {
    class:
      'bg-background mb-4 dark:bg-secondary-background border-foreground/30 mt-2 flex rounded-sm border-2 px-4 py-1 overflow-y-auto max-h-[600px]',
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
  readonly loaded = output();
  readonly saved = output<string>();

  editor!: EditorJS;

  constructor() {
    super();

    afterNextRender(() => {
      const value = this.value();

      console.log({ value });

      try {
        const parsed = value ? JSON.parse(value) : null;

        if (!parsed) throw Error('value not valid');

        const intialValue = parsed as OutputData;

        this.createEditor(intialValue);
      } catch {
        this.createEmptyEditor(value);
      }
    });
  }

  ngOnDestroy() {
    this.editor?.destroy?.();
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
      return;
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
      onReady: () => this.loaded.emit(),
      onChange: () => {
        void this.editor.save().then((value) => {
          const serialised = JSON.stringify(value);

          this.value.set(serialised);
          this.saved.emit(serialised);
        });
      },
    });
  }

  async uploadFile(data: File) {
    const response = await firstValueFrom(
      this.storage.uploadMedia(data).pipe(unwrapClientReposne())
    ).catch(() => null);

    if (!response) {
      return { success: 0 };
    }

    // TODO: have to force an on change as the attaches
    // editorjs module does not emit an on change event

    setTimeout(
      () =>
        void this.editor.save().then((value) => {
          this.value.set(JSON.stringify(value));
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

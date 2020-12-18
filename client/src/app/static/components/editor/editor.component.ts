import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  EventEmitter,
  forwardRef,
  Input,
  Output,
  ViewChild,
  ViewEncapsulation,
} from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { StorageService } from '@core/services/storage.service';
import Checklist from '@editorjs/checklist';
import Code from '@editorjs/code';
import EditorJS, { LogLevels, OutputData } from '@editorjs/editorjs';
import Embed from '@editorjs/embed';
import Header from '@editorjs/header';
import ImageTool from '@editorjs/image';
import InlineCode from '@editorjs/inline-code';
import Link from '@editorjs/link';
import List from '@editorjs/list';
import Marker from '@editorjs/marker';
import Underline from '@editorjs/underline';
import { environment } from '@env/environment';

@Component({
  selector: 'app-editor',
  templateUrl: './editor.component.html',
  styleUrls: ['./editor.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  encapsulation: ViewEncapsulation.None,
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => EditorComponent),
      multi: true,
    },
  ],
})
export class EditorComponent implements ControlValueAccessor {
  @ViewChild('editorJs', { static: true }) el: ElementRef;

  @Input() placeholder = '';
  @Output() loaded = new EventEmitter();

  editor: EditorJS;

  onChange: (value: string) => void;
  onTouch: () => void;

  constructor(private storage: StorageService) {}

  writeValue(obj: string) {
    const intialValue = obj && JSON.parse(obj);
    this.createEditor(intialValue);
  }

  registerOnChange(fn: (...args: unknown[]) => unknown) {
    this.onChange = fn;
  }

  registerOnTouched(fn: (...args: unknown[]) => unknown) {
    this.onTouch = fn;
  }

  setDisabledState?(isDisabled: boolean) {
    console.log('setDisabledState', isDisabled);
  }

  createEditor(initialValue: OutputData = null) {
    if (this.editor) {
      return;
    }

    this.editor = new EditorJS({
      logLevel: environment.production
        ? ('ERROR' as LogLevels)
        : ('INFO' as LogLevels),
      placeholder: this.placeholder,
      holder: this.el.nativeElement,
      minHeight: 100,
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
      },
      data: initialValue,
      onReady: () => this.loaded.emit(),
      onChange: () => {
        this.editor.save().then((value) => {
          this.onChange(JSON.stringify(value));
        });
      },
    });
  }

  async uploadFile(data: File) {
    const response = await this.storage.uploadMedia(data).toPromise();
    return {
      success: 1,
      file: {
        url: response.payload.uri,
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

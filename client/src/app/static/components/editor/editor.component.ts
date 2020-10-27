import { selectCurrentWorkspaceIdentifier } from '@core/store/workspaces/workspaces.selectors';
import { Store } from '@ngrx/store';
import { StorageService } from '@core/services/storage.service';
import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  forwardRef,
  ViewChild,
  ViewEncapsulation,
} from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import EditorJS, { OutputData } from '@editorjs/editorjs';
import Header from '@editorjs/header';
import List from '@editorjs/list';
import ImageTool from '@editorjs/image';
import { first } from 'rxjs/operators';

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
export class EditorComponent implements AfterViewInit, ControlValueAccessor {
  @ViewChild('editorJs', { static: true }) el: ElementRef;

  editor: EditorJS;

  onChange: (value: string) => void;
  onTouch: () => void;

  constructor(private storage: StorageService, private store: Store) {}

  ngAfterViewInit() {
    // this.createEditor();
  }

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
    this.editor = new EditorJS({
      placeholder: 'Description',
      holder: this.el.nativeElement,
      minHeight: 100,
      tools: {
        header: Header,
        list: List,
        image: {
          class: ImageTool,
          config: {
            uploader: {
              uploadByFile: this.uploadFile.bind(this),
              uploadByUrl: this.uploadByUrl.bind(this),
            },
          },
        },
      },
      data: initialValue,
      onChange: (args) => {
        console.log(' OnChange', args);

        this.editor.save().then((value) => {
          console.log('saved', value);
          this.onChange(JSON.stringify(value));
        });
      },
    });

    console.log({ editor: this.editor });
  }

  uploadFile(data: File) {
    return this.store
      .select(selectCurrentWorkspaceIdentifier)
      .pipe(first())
      .toPromise()
      .then((workspace) =>
        this.storage.uploadMedia(workspace, data).toPromise()
      )
      .then((response) => ({
        success: 1,
        file: {
          url: response.payload.uri,
        },
      }));
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

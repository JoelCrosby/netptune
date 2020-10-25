import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  ViewChild,
} from '@angular/core';
import { ControlValueAccessor } from '@angular/forms';
import EditorJS from '@editorjs/editorjs';
import Header from '@editorjs/header';
import List from '@editorjs/list';
import ImageTool from '@editorjs/image';

@Component({
  selector: 'app-editor',
  templateUrl: './editor.component.html',
  styleUrls: ['./editor.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EditorComponent implements AfterViewInit, ControlValueAccessor {
  private editor: EditorJS;

  @ViewChild('editorJs', { static: false }) el: ElementRef;

  ngAfterViewInit() {
    this.editor = this.createEditor();
  }

  writeValue(obj: any) {
    this.editor = this.createEditor(obj);
  }

  registerOnChange(fn: any) {
    throw new Error('Method not implemented.');
  }

  registerOnTouched(fn: any) {
    throw new Error('Method not implemented.');
  }

  setDisabledState?(isDisabled: boolean) {
    throw new Error('Method not implemented.');
  }

  createEditor(initialValue: any = null) {
    return new EditorJS({
      holder: this.el.nativeElement,
      tools: {
        header: Header,
        list: List,
        image: {
          class: ImageTool,
          config: {
            uploader: {
              uploadByFile: (data: File) => {
                console.log({ data });

                return new Promise((res) => {
                  res({
                    success: 1,
                    file: {
                      url: 'https://randomuser.me/api/portraits/women/25.jpg',
                      // any other image data you want to store, such as width, height, color, extension, etc
                    },
                  });
                });
              },
              uploadByUrl: (url: string) => {
                console.log({ url });

                return new Promise((res) => {
                  res({
                    success: 1,
                    file: {
                      url: 'https://randomuser.me/api/portraits/women/25.jpg',
                      // any other image data you want to store, such as width, height, color, extension, etc
                    },
                  });
                });
              },
            },
          },
        },
      },
      data: initialValue,
    });
  }
}

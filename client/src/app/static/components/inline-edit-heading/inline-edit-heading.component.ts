import {
  ChangeDetectionStrategy,
  Component,
  effect,
  ElementRef,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { AbstractFormValueControl } from '../abstract-form-value-control';

@Component({
  selector: 'app-inline-edit-heading',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [],
  template: `
    <div
      #editable
      tabindex="0"
      class="font-overpass w-full rounded px-4 py-4 text-3xl transition-colors outline-none"
      [attr.contenteditable]="isEditing() ? 'plaintext-only' : null"
      [class.cursor-text]="!disabled() && !isReadonly()"
      [class.hover:bg-black/5]="!disabled() && !isReadonly()"
      [class.dark:hover:bg-white/5]="!disabled() && !isReadonly()"
      (mousedown)="onMouseDown()"
      (focus)="startEditing()"
      (blur)="onBlur()"
      (input)="onContentInput($event)"
      (keydown.enter)="onEnter($event)"
      (keydown.escape)="onEscape()"></div>
  `,
})
export class InlineEditHeadingComponent extends AbstractFormValueControl {
  readonly submitted = output<string>();
  readonly cancelled = output();

  readonly editableRef = viewChild<ElementRef>('editable');

  isEditing = signal(false);

  private originalValue = '';
  private clickedIn = false;

  constructor() {
    super();

    // Sync external value changes into the DOM while not editing
    effect(() => {
      const val = this.value();
      const el = this.editableRef()?.nativeElement as HTMLElement | undefined;
      if (el && !this.isEditing()) {
        el.innerText = val ?? '';
      }
    });

    // Move cursor to end when editing starts via keyboard/programmatic focus
    effect(() => {
      const el = this.editableRef()?.nativeElement as HTMLElement | undefined;
      if (el && this.isEditing() && !this.clickedIn) {
        const range = document.createRange();
        const sel = window.getSelection();
        range.selectNodeContents(el);
        range.collapse(false);
        sel?.removeAllRanges();
        sel?.addRange(range);
      }
      this.clickedIn = false;
    });
  }

  onMouseDown() {
    this.clickedIn = true;
  }

  startEditing() {
    if (this.disabled() || this.isReadonly() || this.isEditing()) {
      return;
    }

    this.originalValue = this.value() ?? '';
    this.isEditing.set(true);
  }

  onContentInput(_: Event) {
    this.touched.set(true);
  }

  onBlur() {
    if (!this.isEditing()) {
      return;
    }

    this.touched.set(true);
    const el = this.editableRef()?.nativeElement as HTMLElement | undefined;
    this.commit(el?.innerText?.trim() ?? '');
  }

  onEnter(event: Event) {
    event.preventDefault();
    const el = this.editableRef()?.nativeElement as HTMLElement | undefined;
    this.commit(el?.innerText?.trim() ?? '');
  }

  onEscape() {
    const el = this.editableRef()?.nativeElement as HTMLElement | undefined;

    if (el) {
      el.innerText = this.originalValue;
    }

    this.value.set(this.originalValue);
    this.isEditing.set(false);
    this.cancelled.emit();

    el?.blur();
  }

  private commit(val: string) {
    this.value.set(val);
    this.isEditing.set(false);
    this.submitted.emit(val);
  }
}

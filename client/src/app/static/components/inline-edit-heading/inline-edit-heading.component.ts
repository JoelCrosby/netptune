import {
  Component,
  computed,
  effect,
  ElementRef,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { AbstractFormValueControl } from '../abstract-form-value-control';
import { cn } from '../button/button.variants';

@Component({
  selector: 'app-inline-edit-heading',
  imports: [],
  template: `
    <div
      #editable
      tabindex="0"
      [class]="headingClass()"
      [attr.contenteditable]="isEditing() ? 'plaintext-only' : null"
      [class.cursor-text]="isInteractive()"
      [class.select-text]="isInteractive()"
      [class.hover:bg-black/5]="isInteractive()"
      [class.dark:hover:bg-white/5]="isInteractive()"
      (mousedown)="onMouseDown($event)"
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

  readonly textClass = input('px-4 py-4 text-2xl');

  protected readonly headingClass = computed(() => {
    return cn(
      'font-overpass w-full rounded transition-colors outline-none',
      this.textClass()
    );
  });

  readonly editableRef = viewChild<ElementRef>('editable');

  readonly isInteractive = computed(
    () => !this.disabled() && !this.isReadonly()
  );

  isEditing = signal(false);

  private originalValue = '';
  private clickedIn = false;

  constructor() {
    super();

    effect(() => {
      const val = this.value();
      const el = this.editableRef()?.nativeElement as HTMLElement | undefined;
      if (el && !this.isEditing()) {
        el.innerText = val ?? '';
      }
    });

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

  onMouseDown(event: MouseEvent) {
    this.clickedIn = true;

    if (!this.isInteractive()) {
      return;
    }

    if (this.isEditing()) {
      event.stopPropagation();

      return;
    }

    if (event.button !== 0) {
      return;
    }

    const el = this.editableRef()?.nativeElement as HTMLElement | undefined;

    if (!el) {
      return;
    }

    event.preventDefault();

    el.setAttribute('contenteditable', 'plaintext-only');
    this.startEditing();

    el.focus();
    this.placeCaretFromPoint(el, event.clientX, event.clientY);
  }

  private placeCaretFromPoint(el: HTMLElement, x: number, y: number) {
    const selection = window.getSelection();

    if (!selection) {
      return;
    }

    const range = caretRangeFromPoint(x, y) ?? caretRangeAtEnd(el);

    selection.removeAllRanges();
    selection.addRange(range);
  }

  startEditing() {
    if (!this.isInteractive() || this.isEditing()) {
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

function caretRangeFromPoint(x: number, y: number): Range | null {
  const position = document.caretPositionFromPoint(x, y);

  if (!position) {
    return null;
  }

  const range = document.createRange();

  range.setStart(position.offsetNode, position.offset);
  range.collapse(true);

  return range;
}

function caretRangeAtEnd(el: HTMLElement): Range {
  const range = document.createRange();

  range.selectNodeContents(el);
  range.collapse(false);

  return range;
}

import {
  Component,
  ElementRef,
  computed,
  effect,
  input,
  model,
  output,
  signal,
  viewChild,
} from '@angular/core';

@Component({
  selector: 'app-inline-edit-input',
  imports: [],
  host: {
    class: 'block',
    '[class.edit-active]': 'isEditActive()',
  },
  template: `
    <div
      #editable
      class="hover:bg-hover box-border min-h-lh w-fit max-w-full rounded border-2 border-solid p-1 wrap-break-word transition-[min-width,background-color,border-color] duration-200 ease-out outline-none [font:inherit]"
      [attr.tabindex]="isInteractive() ? 0 : null"
      [attr.contenteditable]="isEditActive() ? 'plaintext-only' : null"
      [attr.aria-readonly]="isInteractive() ? null : 'true'"
      [style.min-width]="isEditActive() ? '16rem' : '0'"
      [style.border-color]="borderColor()"
      [class.cursor-text]="isInteractive()"
      (mousedown)="onMouseDown()"
      (focus)="startEditing()"
      (blur)="onBlur()"
      (input)="onContentInput()"
      (keydown.enter)="onEnter($event)"
      (keydown.escape)="onEscape()"></div>
  `,
})
export class InlineEditInputComponent {
  readonly value = model<string | null | undefined>('');
  readonly touched = model<boolean>(false);
  readonly disabled = input<boolean>(false);
  readonly required = input<boolean>(false);
  readonly size = input<number>();
  readonly activeBorder = input<boolean | string | null>();
  readonly readonly = input<boolean>(false);

  readonly submitted = output<string>();

  readonly editableRef = viewChild<ElementRef>('editable');

  isEditActive = signal(false);

  readonly isInteractive = computed(() => !this.disabled() && !this.readonly());

  readonly borderColor = computed(() => {
    if (!this.isEditActive() || !this.activeBorder()) return 'transparent';

    return 'var(--primary)';
  });

  private originalValue = '';
  private clickedIn = false;

  constructor() {
    // Sync external value changes into the DOM while not editing
    effect(() => {
      const val = this.value();
      const el = this.editableRef()?.nativeElement as HTMLElement | undefined;

      if (el && !this.isEditActive()) {
        el.innerText = val ?? '';
      }
    });

    // Move cursor to end when editing starts via keyboard/programmatic focus
    effect(() => {
      const el = this.editableRef()?.nativeElement as HTMLElement | undefined;

      if (el && this.isEditActive() && !this.clickedIn) {
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
    if (!this.isInteractive() || this.isEditActive()) {
      return;
    }

    this.originalValue = this.value() ?? '';
    this.isEditActive.set(true);
  }

  onContentInput() {
    this.touched.set(true);
  }

  onBlur() {
    if (!this.isEditActive()) {
      return;
    }

    this.touched.set(true);

    // Blurring commits rather than discards: the next click is usually the Save button, and
    // silently dropping what was just typed loses the edit.
    this.commit(this.readEditable());
  }

  onEnter(event: Event) {
    event.preventDefault();
    this.commit(this.readEditable());
    this.blurEditable();
  }

  onEscape() {
    const el = this.editableRef()?.nativeElement as HTMLElement | undefined;

    if (el) {
      el.innerText = this.originalValue;
    }

    this.value.set(this.originalValue);
    this.isEditActive.set(false);

    this.blurEditable();
  }

  private readEditable(): string {
    const el = this.editableRef()?.nativeElement as HTMLElement | undefined;

    return el?.innerText?.trim() ?? '';
  }

  private blurEditable() {
    const el = this.editableRef()?.nativeElement as HTMLElement | undefined;

    el?.blur();
  }

  private commit(val: string) {
    const changed = val !== this.originalValue;

    this.value.set(val);
    this.originalValue = val;
    this.isEditActive.set(false);

    if (changed) {
      this.submitted.emit(val);
    }
  }
}

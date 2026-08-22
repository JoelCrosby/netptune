import {
  ChangeDetectorRef,
  Component,
  ElementRef,
  effect,
  inject,
  input,
  model,
  output,
  signal,
  untracked,
  viewChild,
} from '@angular/core';
import { DocumentService } from '@static/services/document.service';

@Component({
  selector: 'app-inline-edit-input',
  imports: [],
  host: {
    '[class.edit-active]': 'isEditActive()',
  },
  template: `
    <div class="w-full rounded">
      @if (isEditActive()) {
        <input
          #input
          type="text"
          class="inline-edit-input box-border w-full border-0 bg-transparent p-1 text-inherit transition-all duration-200 [font:inherit]"
          [value]="value()"
          [readonly]="readonly()"
          [disabled]="disabled()"
          [class.active-border]="activeBorder()"
          (input)="onInput($event)"
          (keyup.enter)="onSubmit(input.value)" />
      } @else {
        <div class="border-2 border-transparent p-1 [font:inherit]">
          {{ value() }}
        </div>
      }
    </div>
  `,
})
export class InlineEditInputComponent {
  private elementRef = inject(ElementRef);
  private cd = inject(ChangeDetectorRef);
  private document = inject(DocumentService);

  readonly value = model<string | null | undefined>('');
  readonly touched = model<boolean>(false);
  readonly disabled = input<boolean>(false);
  readonly required = input<boolean>(false);
  readonly size = input<number>();
  readonly activeBorder = input<boolean | string | null>();
  readonly readonly = input<boolean>(false);

  readonly input = viewChild.required<ElementRef>('input');
  readonly submitted = output<string>();
  isEditActive = signal(false);

  private editingFrom = '';

  constructor() {
    effect(() => {
      const el = this.document.documentClicked();
      untracked(() => this.handleDocumentClick(el));
    });
  }

  onInput(event: Event) {
    const target = event.target as HTMLInputElement;
    const value = target.value;

    this.value.set(value);
  }

  handleDocumentClick(target: EventTarget) {
    if (this.isEditActive()) {
      if (!this.elementRef.nativeElement.contains(target)) {
        // Clicking away commits rather than discards: the next click is usually the Save button, and
        // silently dropping what was just typed loses the edit.
        return this.commit();
      }
    } else {
      if (this.elementRef.nativeElement.contains(target)) {
        this.editingFrom = this.value() ?? '';
        this.isEditActive.set(true);
        this.focusInput();
      }
    }
  }

  focusInput() {
    this.cd.detectChanges();
    const textarea = this.input();

    if (textarea) {
      textarea?.nativeElement.focus();
    }
  }

  onSubmit(value: string) {
    this.value.set(value);
    this.editingFrom = value;
    this.submitted.emit(value);
    this.isEditActive.set(false);
  }

  private commit() {
    const value = this.value() ?? '';
    const changed = value !== this.editingFrom;

    this.isEditActive.set(false);

    if (changed) {
      this.submitted.emit(value);
    }
  }
}

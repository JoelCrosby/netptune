import {
  ChangeDetectionStrategy,
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
  templateUrl: './inline-edit-input.component.html',
  styleUrls: ['./inline-edit-input.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [],
  host: {
    '[class.edit-active]': 'isEditActive()',
  },
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
        return this.isEditActive.set(false);
      }
    } else {
      if (this.elementRef.nativeElement.contains(target)) {
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
    this.submitted.emit(value);
    this.isEditActive.set(false);
  }
}

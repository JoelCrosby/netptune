import { CdkTextareaAutosize } from '@angular/cdk/text-field';
import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  ElementRef,
  inject,
  input,
  model,
  signal,
  viewChild,
} from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import {
  DisabledReason,
  FormValueControl,
  ValidationError,
  WithOptionalField,
} from '@angular/forms/signals';
import { DocumentService } from '@static/services/document.service';

@Component({
  selector: 'app-inline-text-area',
  templateUrl: './inline-text-area.component.html',
  styleUrls: ['./inline-text-area.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CdkTextareaAutosize, FormsModule, ReactiveFormsModule],
  host: { '[class.edit-active]': 'isEditActive()' },
})
export class InlineTextAreaComponent implements FormValueControl<string> {
  private elementRef = inject(ElementRef);
  private document = inject(DocumentService);
  private cd = inject(ChangeDetectorRef);

  readonly activeBorder = input.required<boolean | string>();

  readonly minRows = input(1);
  readonly maxRows = input(3);

  readonly autosize = viewChild<CdkTextareaAutosize>('autosize');
  readonly textarea = viewChild<ElementRef>('textarea');

  readonly value = model('');
  readonly touched = model<boolean>(false);
  readonly disabled = input<boolean>(false);
  readonly required = input<boolean>(false);
  readonly disabledReasons = input<
    readonly WithOptionalField<DisabledReason>[]
  >([]);
  readonly readonly = input<boolean>(false);
  readonly hidden = input<boolean>(false);
  readonly invalid = input<boolean>(false);
  readonly errors = input<readonly ValidationError.WithOptionalField[]>([]);

  isEditActive = signal(false);

  constructor() {
    this.document.documentClicked().subscribe({
      next: this.handleDocumentClick.bind(this),
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
    this.autosize()?.resizeToFitContent(true);

    const textarea = this.textarea();

    if (textarea) {
      textarea?.nativeElement.focus();
    }
  }
}

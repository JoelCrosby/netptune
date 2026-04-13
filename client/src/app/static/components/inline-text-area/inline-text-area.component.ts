import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  effect,
  ElementRef,
  inject,
  input,
  model,
  signal,
  untracked,
  viewChild,
} from '@angular/core';
import {
  DisabledReason,
  FormValueControl,
  ValidationError,
  WithOptionalFieldTree,
} from '@angular/forms/signals';
import { DocumentService } from '@static/services/document.service';

@Component({
  selector: 'app-inline-text-area',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '(input)': 'onInput($event)',
  },
  template: `
    <div
      class="hover:bg-hover font-overpass my-4 flex w-full rounded text-3xl transition"
      [class.hover:bg-hover]="!isReadonly()">
      <div
        #div
        (click)="!isReadonly() && onClicked()"
        class="inline-text-area-label whitespace-[inherit] w-full rounded p-3 leading-normal text-inherit transition-colors duration-200 [font:inherit]">
        {{ value() }}
      </div>
    </div>
  `,
})
export class InlineTextAreaComponent implements FormValueControl<string> {
  private elementRef = viewChild.required<ElementRef>('div');
  private document = inject(DocumentService);
  private cd = inject(ChangeDetectorRef);

  readonly activeBorder = input.required<boolean | string>();

  readonly value = model('');
  readonly touched = model<boolean>(false);
  readonly disabled = input<boolean>(false);
  readonly required = input<boolean>(false);
  readonly disabledReasons = input<
    readonly WithOptionalFieldTree<DisabledReason>[]
  >([]);
  readonly isReadonly = input<boolean>(false);
  readonly hidden = input<boolean>(false);
  readonly invalid = input<boolean>(false);
  readonly errors = input<readonly ValidationError.WithOptionalFieldTree[]>([]);

  isContentEditable = signal(false);

  constructor() {
    effect(() => {
      const el = this.document.documentClicked();
      untracked(() => this.handleDocumentClick(el));
    });

    effect(() => {
      const element = this.elementRef();

      if (!this.elementRef()) return;

      if (this.isContentEditable()) {
        element.nativeElement.setAttribute('contenteditable', 'true');
      } else {
        element.nativeElement.setAttribute('contenteditable', 'false');
      }

      this.cd.markForCheck();
    });
  }

  onInput(event: Event) {
    const target = event.target as HTMLInputElement;
    const value = target.value;

    this.value.set(value);
  }

  handleDocumentClick(target: EventTarget) {
    const element = this.elementRef();

    if (!element) return;

    if (this.isContentEditable()) {
      if (!element.nativeElement.contains(target as HTMLElement)) {
        return this.isContentEditable.set(false);
      }
    }
  }

  onClicked() {
    if (!this.isContentEditable()) {
      this.isContentEditable.set(true);
    }
  }
}

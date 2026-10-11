import {
  computed,
  afterRenderEffect,
  Component,
  ElementRef,
  input,
  model,
  output,
  viewChild,
} from '@angular/core';
import { cn } from '../button/button.variants';

@Component({
  selector: 'app-document-header',
  host: { '[class]': 'hostClass()' },
  template: `
    <div
      class="mb-3 flex min-h-[22px] flex-wrap items-center gap-2 text-xs empty:hidden">
      <ng-content select="[documentChips]" />
    </div>

    <textarea
      #titleInput
      rows="1"
      class="font-overpass placeholder:text-foreground/25 m-0 mb-2 w-full resize-none overflow-hidden border-0 bg-transparent p-0 text-[34px] leading-[1.2] font-semibold tracking-[-0.01em] outline-none"
      [value]="title()"
      [placeholder]="placeholder()"
      [readOnly]="readOnly()"
      [attr.aria-label]="titleLabel()"
      (input)="onInput(titleInput)"
      (blur)="committed.emit(title())"
      (keydown.enter)="onEnter($event)"></textarea>

    @if (byline()) {
      <span class="text-foreground/50 mb-7 text-[13px]">{{ byline() }}</span>
    }
  `,
})
export class DocumentHeaderComponent {
  readonly class = input('');

  protected readonly hostClass = computed(() =>
    cn('flex flex-col', this.class())
  );

  private readonly titleInput =
    viewChild.required<ElementRef<HTMLTextAreaElement>>('titleInput');

  readonly title = model.required<string>();
  readonly byline = input('');
  readonly placeholder = input('');
  readonly titleLabel = input<string | null>(null);
  readonly readOnly = input(false);

  readonly committed = output<string>();
  readonly continued = output();

  constructor() {
    afterRenderEffect(() => {
      this.title();
      this.fitHeight();
    });
  }

  protected onInput(element: HTMLTextAreaElement) {
    const singleLine = element.value.replace(/\n/g, ' ');

    this.title.set(singleLine);
  }

  protected onEnter(event: Event) {
    event.preventDefault();
    this.committed.emit(this.title());
    this.continued.emit();
  }

  private fitHeight() {
    const element = this.titleInput().nativeElement;

    element.style.height = 'auto';
    element.style.height = `${element.scrollHeight}px`;
  }
}

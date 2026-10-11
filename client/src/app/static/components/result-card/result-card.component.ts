import {
  afterRenderEffect,
  Component,
  computed,
  ElementRef,
  input,
  output,
  viewChild,
} from '@angular/core';
import { LucideX } from '@lucide/angular';
import { FlatButtonComponent } from '../button/flat-button.component';
import { cn } from '../button/button.variants';
import { IconButtonComponent } from '../button/icon-button.component';
import { StrokedButtonComponent } from '../button/stroked-button.component';
import { editorMenuList } from '../editor/editor-menu.styles';
import { SpinnerIconComponent } from '../spinner/spinner-icon.component';

export type ResultCardStatus = 'loading' | 'ready' | 'error';

export interface ResultCardAction {
  id: string;
  label: string;
  primary?: boolean;
}

let nextHeadingId = 0;

// A floating card for generated text: it waits, then shows the text with what can be done with it.
// Anything that shapes the request, such as a language picker, is projected into its header.
@Component({
  selector: 'app-result-card',
  imports: [
    FlatButtonComponent,
    StrokedButtonComponent,
    IconButtonComponent,
    SpinnerIconComponent,
    LucideX,
  ],
  host: {
    '[class]': 'hostClass()',
    '(keydown.escape)': 'dismissed.emit()',
  },
  template: `
    <section
      role="dialog"
      [class]="panelClass"
      [attr.aria-labelledby]="headingId">
      <header class="flex items-center gap-2 px-1.5 pt-0.5">
        <h2 class="text-foreground/60 text-xs font-semibold" [id]="headingId">
          {{ heading() }}
        </h2>
        <span class="flex-1"></span>
        <ng-content select="[resultCardControls]" />
        <button
          app-icon-button
          type="button"
          size="xs"
          [ariaLabel]="dismissLabel()"
          [title]="dismissLabel()"
          (click)="dismissed.emit()">
          <svg lucideX class="h-3.5 w-3.5"></svg>
        </button>
      </header>

      <div aria-live="polite">
        @switch (status()) {
          @case ('loading') {
            <div
              class="text-foreground/60 flex items-center gap-2 px-1.5 py-3 text-sm">
              <app-spinner-icon />
              <span>{{ loadingLabel() }}</span>
            </div>
          }
          @case ('error') {
            <p class="text-warn px-1.5 py-3 text-sm">{{ error() }}</p>
          }
          @default {
            <div
              class="custom-scroll bg-background border-border max-h-64 overflow-y-auto rounded-md border px-3 py-2 text-sm leading-relaxed whitespace-pre-wrap"
              [textContent]="text()"></div>
          }
        }
      </div>

      @if (status() === 'ready') {
        <footer class="flex justify-end gap-2 px-0.5 pt-1" #footer>
          @for (action of actions(); track action.id) {
            @if (action.primary) {
              <button
                app-flat-button
                type="button"
                size="small"
                (click)="acted.emit(action.id)">
                {{ action.label }}
              </button>
            } @else {
              <button
                app-stroked-button
                type="button"
                size="small"
                (click)="acted.emit(action.id)">
                {{ action.label }}
              </button>
            }
          }
        </footer>
      }
    </section>
  `,
})
export class ResultCardComponent {
  readonly heading = input.required<string>();
  readonly status = input<ResultCardStatus>('loading');
  readonly text = input('');
  readonly error = input('');
  readonly loadingLabel = input.required<string>();
  readonly dismissLabel = input.required<string>();
  readonly actions = input<readonly ResultCardAction[]>([]);
  readonly class = input('');

  readonly acted = output<string>();
  readonly dismissed = output();

  private readonly footer = viewChild<ElementRef<HTMLElement>>('footer');

  protected readonly headingId = `result-card-heading-${nextHeadingId++}`;
  protected readonly hostClass = computed(() => cn('block', this.class()));
  protected readonly panelClass = cn(
    editorMenuList,
    'w-[420px] max-w-[calc(100vw-32px)] gap-2 p-2'
  );

  constructor() {
    // The card opens away from the focus, so a keyboard user is taken to its first action once it has one.
    afterRenderEffect(() => {
      const footer = this.footer()?.nativeElement;

      footer?.querySelector<HTMLButtonElement>('button')?.focus();
    });
  }
}

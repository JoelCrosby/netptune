import { Component, computed, input, output, signal } from '@angular/core';
import { LucideArrowUp } from '@lucide/angular';
import { cn } from '../button/button.variants';

let nextHintId = 0;

@Component({
  selector: 'app-prompt-hero',
  imports: [LucideArrowUp],
  host: { '[class]': 'hostClass()' },
  template: `
    <section
      class="border-primary/20 bg-primary/5 flex flex-col items-center gap-[18px] rounded-[14px] border px-8 pt-10 pb-[30px] max-md:px-4 max-md:pt-6 max-md:pb-5">
      <div class="flex flex-col items-center gap-1.5 text-center">
        <h2 class="text-[26px] font-semibold tracking-[-0.01em] max-md:text-xl">
          {{ heading() }}
        </h2>
        @if (description()) {
          <p class="text-foreground/65 text-sm">{{ description() }}</p>
        }
      </div>

      <form
        class="border-primary/40 bg-card focus-within:border-primary flex w-full max-w-[800px] flex-col gap-2.5 rounded-[14px] border-[1.5px] p-3 shadow-[0_6px_20px_rgba(120,60,200,0.12)] transition-colors"
        (submit)="$event.preventDefault(); submit()">
        <input
          type="text"
          class="placeholder:text-foreground/45 h-9 w-full bg-transparent px-1.5 text-base outline-none"
          [placeholder]="placeholder()"
          [attr.aria-label]="ariaLabel()"
          [attr.aria-describedby]="hint() ? hintId : null"
          [value]="text()"
          (input)="text.set(field.value)"
          #field />

        <div class="flex items-center gap-2">
          <ng-content select="[promptHeroControls]" />
          @if (hint()) {
            <span
              class="text-foreground/60 min-w-0 truncate text-[12.5px]"
              [id]="hintId"
              >{{ hint() }}</span
            >
          }
          <span class="flex-1"></span>
          <button
            type="submit"
            class="bg-primary text-primary-foreground focus-visible:ring-primary flex h-9 w-9 shrink-0 cursor-pointer items-center justify-center rounded-full transition-opacity focus-visible:ring-2 focus-visible:ring-offset-2 focus-visible:outline-none disabled:cursor-default disabled:opacity-45"
            [attr.aria-label]="submitLabel()"
            [title]="submitLabel()"
            [disabled]="!canSubmit()">
            <svg lucideArrowUp class="h-4 w-4"></svg>
          </button>
        </div>
      </form>

      @if (suggestions().length) {
        <div class="flex flex-wrap justify-center gap-2">
          @for (suggestion of suggestions(); track suggestion) {
            <button
              type="button"
              class="border-primary/25 bg-card text-foreground/80 hover:border-primary hover:text-primary focus-visible:ring-primary h-[30px] max-w-full cursor-pointer truncate rounded-full border px-3 text-[13px] transition-colors focus-visible:ring-2 focus-visible:outline-none"
              (click)="useSuggestion(suggestion, field)">
              {{ suggestion }}
            </button>
          }
        </div>
      }
    </section>
  `,
})
export class PromptHeroComponent {
  readonly heading = input.required<string>();
  readonly description = input('');
  readonly placeholder = input('');
  readonly ariaLabel = input.required<string>();
  readonly submitLabel = input.required<string>();
  readonly hint = input('');
  readonly suggestions = input<readonly string[]>([]);
  readonly class = input('');

  readonly submitted = output<string>();

  protected readonly text = signal('');
  protected readonly hintId = `prompt-hero-hint-${nextHintId++}`;
  protected readonly hostClass = computed(() => cn('block', this.class()));
  protected readonly canSubmit = computed(() => this.text().trim().length > 0);

  protected useSuggestion(suggestion: string, field: HTMLInputElement) {
    this.text.set(suggestion);
    field.focus();
  }

  protected submit() {
    const prompt = this.text().trim();

    if (!prompt) return;

    this.submitted.emit(prompt);
    this.text.set('');
  }
}

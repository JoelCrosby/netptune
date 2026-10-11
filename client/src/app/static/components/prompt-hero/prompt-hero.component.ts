import { Component, computed, input, output, signal } from '@angular/core';
import { LucideArrowUp } from '@lucide/angular';
import { cn } from '../button/button.variants';
import { HeroPanelComponent } from '../hero-panel/hero-panel.component';

let nextHintId = 0;

@Component({
  selector: 'app-prompt-hero',
  imports: [HeroPanelComponent, LucideArrowUp],
  host: { '[class]': 'hostClass()' },
  template: `
    <app-hero-panel [heading]="heading()" [description]="description()">
      <form
        class="border-primary/40 bg-card focus-within:border-primary shadow-primary/15 flex w-full max-w-[800px] flex-col gap-2.5 rounded-[14px] border-[1.5px] p-3 shadow-lg transition-colors"
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
          @for (suggestion of suggestions(); track $index) {
            <button
              type="button"
              class="border-primary/25 bg-card text-foreground/80 hover:border-primary hover:text-primary focus-visible:ring-primary h-[30px] max-w-full cursor-pointer truncate rounded-full border px-3 text-[13px] transition-colors focus-visible:ring-2 focus-visible:outline-none"
              (click)="useSuggestion(suggestion, field)">
              {{ suggestion }}
            </button>
          }
        </div>
      }
    </app-hero-panel>
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

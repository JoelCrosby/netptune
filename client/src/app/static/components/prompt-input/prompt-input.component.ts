import { Component, computed, input, output, signal } from '@angular/core';
import {
  LucideArrowUp,
  LucideDynamicIcon,
  type LucideIconInput,
} from '@lucide/angular';
import { cn } from '../button/button.variants';
import { IconButtonComponent } from '../button/icon-button.component';

let nextHintId = 0;

@Component({
  selector: 'app-prompt-input',
  imports: [IconButtonComponent, LucideArrowUp, LucideDynamicIcon],
  host: { '[class]': 'hostClass()' },
  template: `
    <form
      class="bg-card border-border focus-within:border-primary/60 flex h-12 items-center gap-2 rounded-lg border pr-1.5 pl-3 transition-colors"
      (submit)="$event.preventDefault(); submit()">
      @if (icon(); as icon) {
        <svg
          [lucideIcon]="icon"
          class="text-foreground/50 h-4 w-4 shrink-0"
          aria-hidden="true"></svg>
      }
      <input
        type="text"
        class="placeholder:text-foreground/60 min-w-0 flex-1 bg-transparent text-sm outline-none"
        [placeholder]="placeholder()"
        [attr.aria-label]="ariaLabel()"
        [attr.aria-describedby]="hint() ? hintId : null"
        [value]="text()"
        (input)="text.set(field.value)"
        #field />
      @if (hint()) {
        <span
          class="text-foreground/60 shrink-0 text-xs max-md:sr-only"
          [class.sr-only]="!!text()"
          [id]="hintId"
          >{{ hint() }}</span
        >
      }
      <button
        app-icon-button
        type="submit"
        color="soft"
        size="small"
        [ariaLabel]="submitLabel()"
        [title]="submitLabel()"
        [disabled]="!canSubmit()">
        <svg lucideArrowUp class="h-4 w-4"></svg>
      </button>
    </form>
  `,
})
export class PromptInputComponent {
  readonly placeholder = input('');
  readonly ariaLabel = input.required<string>();
  readonly submitLabel = input.required<string>();
  readonly hint = input('');
  readonly icon = input<LucideIconInput | null>(null);
  readonly class = input('');

  readonly submitted = output<string>();

  protected readonly text = signal('');
  protected readonly hintId = `prompt-input-hint-${nextHintId++}`;
  protected readonly hostClass = computed(() => cn('block', this.class()));
  protected readonly canSubmit = computed(() => this.text().trim().length > 0);

  protected submit() {
    const question = this.text().trim();

    if (!question) return;

    this.submitted.emit(question);
    this.text.set('');
  }
}

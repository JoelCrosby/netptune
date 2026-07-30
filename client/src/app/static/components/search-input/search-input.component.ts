import {
  Component,
  effect,
  input,
  output,
  signal,
  untracked,
} from '@angular/core';
import {
  FormField,
  form,
  maxLength,
  minLength,
  required,
} from '@angular/forms/signals';
import { TooltipDirective } from '@static/directives/tooltip.directive';
import { LucideSearch, LucideX } from '@lucide/angular';

@Component({
  selector: 'app-search-input',
  imports: [LucideX, LucideSearch, TooltipDirective, FormField],
  template: `
    <div
      class="bg-secondary-background focus-within:border-primary [&.active]:border-primary [&.invalid]:border-warn flex flex-row items-center rounded border-2 border-solid border-[rgba(var(--foreground-rgb),.1)] pr-[0.4rem] transition-colors duration-[240ms] ease-out focus-within:bg-transparent [&_svg]:text-[rgba(var(--foreground-rgb),.1)] [&.active]:bg-transparent"
      [class.invalid]="
        termForm.term().value() &&
        termForm.term().touched() &&
        termForm.term().invalid()
      "
      [class.active]="
        termForm.term().value() &&
        termForm.term().touched() &&
        termForm.term().valid()
      ">
      <input
        type="text"
        i18n-placeholder="Placeholder in the search box"
        placeholder="Search"
        class="w-45 appearance-none border-0 bg-transparent px-[0.8rem] py-0 font-[inherit] text-[15px] leading-8.25 font-medium text-inherit transition-[width] duration-[140ms] ease-out outline-none placeholder:opacity-60 [&:placeholder-shown:not(:focus)]:w-[80px]"
        [formField]="termForm.term"
        (keydown.enter)="onSubmit()" />

      @if (termForm.term().value()) {
        <button
          type="button"
          class="hover:text-primary! focus-visible:ring-primary flex h-4 w-4 cursor-pointer items-center justify-center rounded focus-visible:ring-2 focus-visible:outline-none"
          i18n-aria-label="
            Accessible label for the button that clears the search box
          "
          aria-label="Clear search term"
          i18n-appTooltip="Tooltip on the button that clears the search box"
          appTooltip="Clear search term"
          (click)="onClearClicked()">
          <svg lucideX class="h-4 w-4" aria-hidden="true"></svg>
        </button>
      } @else {
        <svg
          lucideSearch
          aria-hidden="false"
          i18n-aria-label="Accessible name of the magnifying-glass search icon"
          aria-label="Search Icon"
          class="h-4 w-4"></svg>
      }
    </div>
  `,
})
export class SearchInputComponent {
  readonly term = input<string | null | undefined>('');
  readonly searchChange = output<string | null>();

  readonly termFormModel = signal({
    term: this.term() ?? '',
  });

  readonly termForm = form(this.termFormModel, (schema) => {
    required(schema.term);
    minLength(schema.term, 2);
    maxLength(schema.term, 64);
  });

  constructor() {
    effect(() => {
      const term = this.term() ?? '';

      untracked(() => {
        if (this.termForm.term().value() === term) return;

        this.termForm.term().value.set(term);

        if (!term) {
          this.termForm.term().reset();
        }
      });
    });
  }

  onSubmit() {
    if (!this.termForm.term().value()) {
      this.onClearClicked();
      return;
    }

    if (this.termForm().invalid()) {
      return;
    }

    this.searchChange.emit(this.termForm.term().value());
  }

  onClearClicked() {
    this.termForm.term().value.set('');
    this.termForm.term().reset();

    this.searchChange.emit(null);
  }
}

import { Component, effect, inject, signal, untracked } from '@angular/core';
import {
  FormField,
  form,
  maxLength,
  minLength,
  required,
} from '@angular/forms/signals';
import { setSearchTerm } from '@core/store/tasks/tasks.actions';
import { selectTaskSearchTerm } from '@core/store/tasks/tasks.selectors';
import { LucideSearch, LucideX } from '@lucide/angular';
import { Store } from '@ngrx/store';
import { TooltipDirective } from '@static/directives/tooltip.directive';

@Component({
  selector: 'app-task-list-search',
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
        placeholder="Search"
        class="w-45 appearance-none border-0 bg-transparent px-[0.8rem] py-0 [font-family:inherit] text-[15px] leading-[33px] font-medium text-inherit transition-[width] duration-[140ms] ease-out outline-none placeholder:opacity-60 [&:placeholder-shown:not(:focus)]:w-[80px]"
        [formField]="termForm.term"
        (keydown.enter)="onSubmit()" />

      @if (termForm.term().value()) {
        <svg
          lucideX
          aria-hidden="false"
          aria-label="Clear Search Icon"
          class="hover:text-primary! h-4 w-4 cursor-pointer"
          appTooltip="Clear search term"
          (click)="onClearClicked()"></svg>
      } @else {
        <svg
          lucideSearch
          aria-hidden="false"
          aria-label="Search Icon"
          class="h-4 w-4"></svg>
      }
    </div>
  `,
})
export class TaskListSearchComponent {
  private readonly store = inject(Store);

  readonly searchTerm = this.store.selectSignal(selectTaskSearchTerm);

  readonly termFormModel = signal({
    term: this.searchTerm() ?? '',
  });

  readonly termForm = form(this.termFormModel, (schema) => {
    required(schema.term);
    minLength(schema.term, 2);
    maxLength(schema.term, 64);
  });

  constructor() {
    effect(() => {
      const term = this.searchTerm() ?? '';

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

    const term = this.termForm.term().value();
    this.store.dispatch(setSearchTerm({ term }));
  }

  onClearClicked() {
    this.termForm.term().value.set('');
    this.termForm.term().reset();

    this.store.dispatch(setSearchTerm({ term: null }));
  }
}

import {
  ChangeDetectionStrategy,
  Component,
  inject,
  signal,
} from '@angular/core';
import {
  FormField,
  form,
  maxLength,
  minLength,
  required,
} from '@angular/forms/signals';
import { TooltipDirective } from '@app/static/directives/tooltip.directive';
import { setSearchTerm } from '@boards/store/groups/board-groups.actions';
import { selectSearchTerm } from '@boards/store/groups/board-groups.selectors';
import { LucideSearch, LucideX } from '@lucide/angular';
import { Store } from '@ngrx/store';

@Component({
  selector: 'app-board-groups-search',
  styleUrls: ['./board-groups-search.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LucideX, LucideSearch, TooltipDirective, FormField],
  template: `<div
    class="board-groups-search-input"
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
      [formField]="termForm.term"
      (keydown.enter)="onSubmit()" />

    @if (termForm.term().value()) {
      <svg
        lucideX
        aria-hidden="false"
        aria-label="Clear Search Icon"
        class="clear-search h-4 w-4"
        appTooltip="Clear search term"
        (click)="onClearClicked()"></svg>
    } @else {
      <svg
        lucideSearch
        aria-hidden="false"
        aria-label="Search Icon"
        class="h-4 w-4"></svg>
    }
  </div> `,
})
export class BoardGroupsSearchComponent {
  private store = inject(Store);

  searchTerm = this.store.selectSignal(selectSearchTerm);

  termFormModel = signal({
    term: this.searchTerm() ?? '',
  });

  termForm = form(this.termFormModel, (schema) => {
    required(schema.term);
    minLength(schema.term, 2);
    maxLength(schema.term, 64);
  });

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

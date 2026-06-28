import { Component, inject } from '@angular/core';
import { setSearchTerm } from '@core/store/tasks/tasks.actions';
import { selectTaskSearchTerm } from '@core/store/tasks/tasks.selectors';
import { Store } from '@ngrx/store';
import { SearchInputComponent } from '@static/components/search-input/search-input.component';

@Component({
  selector: 'app-task-list-search',
  imports: [SearchInputComponent],
  template: `
    <app-search-input [term]="searchTerm()" (search)="onSearch($event)" />
  `,
})
export class TaskListSearchComponent {
  private readonly store = inject(Store);

  readonly searchTerm = this.store.selectSignal(selectTaskSearchTerm);

  onSearch(term: string | null) {
    this.store.dispatch(setSearchTerm({ term }));
  }
}

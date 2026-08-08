import { Component, computed } from '@angular/core';
import { taskFilterRoute } from '@core/router/task-filter-route';
import { SearchInputComponent } from '@static/components/search-input/search-input.component';

@Component({
  selector: 'app-task-list-search',
  imports: [SearchInputComponent],
  template: `
    <app-search-input [term]="searchTerm()" (searchChange)="onSearch($event)" />
  `,
})
export class TaskListSearchComponent {
  private readonly filterRoute = taskFilterRoute();

  readonly searchTerm = computed(() => this.filterRoute.filters().term ?? null);

  onSearch(term: string | null) {
    this.filterRoute.set('term', term);
  }
}

import { Component, computed, inject } from '@angular/core';
import { TaskFilterService } from '@core/services/task-filter.service';
import { SearchInputComponent } from '@static/components/search-input/search-input.component';

@Component({
  selector: 'app-board-groups-search',
  imports: [SearchInputComponent],
  template: `
    <app-search-input [term]="searchTerm()" (searchChange)="onSearch($event)" />
  `,
})
export class BoardGroupsSearchComponent {
  private readonly filters = inject(TaskFilterService);

  readonly searchTerm = computed(() => this.filters.filters().term);

  onSearch(term: string | null) {
    this.filters.update({ term });
  }
}

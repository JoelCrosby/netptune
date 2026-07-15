import { Component, inject } from '@angular/core';
import { setSearchTerm } from '@app/core/store/groups/board-groups.actions';
import { selectSearchTerm } from '@app/core/store/groups/board-groups.selectors';
import { Store } from '@ngrx/store';
import { SearchInputComponent } from '@static/components/search-input/search-input.component';

@Component({
  selector: 'app-board-groups-search',
  imports: [SearchInputComponent],
  template: `
    <app-search-input [term]="searchTerm()" (searchChange)="onSearch($event)" />
  `,
})
export class BoardGroupsSearchComponent {
  private store = inject(Store);

  searchTerm = this.store.selectSignal(selectSearchTerm);

  onSearch(term: string | null) {
    this.store.dispatch(setSearchTerm({ term }));
  }
}

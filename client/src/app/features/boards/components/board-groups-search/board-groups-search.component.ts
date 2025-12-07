import {
  ChangeDetectionStrategy,
  Component,
  inject,
  signal,
} from '@angular/core';
import {
  Field,
  form,
  maxLength,
  minLength,
  required,
} from '@angular/forms/signals';
import { MatIcon } from '@angular/material/icon';
import { MatTooltip } from '@angular/material/tooltip';
import { setSearchTerm } from '@boards/store/groups/board-groups.actions';
import { selectSearchTerm } from '@boards/store/groups/board-groups.selectors';
import { Store } from '@ngrx/store';

@Component({
  selector: 'app-board-groups-search',
  templateUrl: './board-groups-search.component.html',
  styleUrls: ['./board-groups-search.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatIcon, MatTooltip, Field],
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

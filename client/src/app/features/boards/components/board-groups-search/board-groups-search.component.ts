import {
  ChangeDetectionStrategy,
  Component,
  effect,
  inject,
} from '@angular/core';
import {
  FormControl,
  FormsModule,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { setSearchTerm } from '@boards/store/groups/board-groups.actions';
import { selectSearchTerm } from '@boards/store/groups/board-groups.selectors';
import { Store } from '@ngrx/store';
import { MatIcon } from '@angular/material/icon';
import { MatTooltip } from '@angular/material/tooltip';

@Component({
  selector: 'app-board-groups-search',
  templateUrl: './board-groups-search.component.html',
  styleUrls: ['./board-groups-search.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, ReactiveFormsModule, MatIcon, MatTooltip],
})
export class BoardGroupsSearchComponent {
  private store = inject(Store);

  searchTerm = this.store.selectSignal(selectSearchTerm);

  termFormControl = new FormControl<string | null | undefined>('', [
    Validators.required,
    Validators.minLength(2),
    Validators.maxLength(64),
  ]);

  constructor() {
    effect(() =>
      this.termFormControl.setValue(this.searchTerm(), { emitEvent: false })
    );
  }

  onSubmit() {
    if (!this.termFormControl.value) {
      this.store.dispatch(setSearchTerm({ term: null }));
    }

    if (!this.termFormControl.valid) {
      return;
    }

    const term = this.termFormControl.value;
    this.store.dispatch(setSearchTerm({ term }));
  }

  onClearClicked() {
    this.termFormControl.setValue('');
    setTimeout(() => this.store.dispatch(setSearchTerm({ term: null })), 240);
  }
}

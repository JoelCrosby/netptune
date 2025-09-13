import {
  Component,
  OnInit,
  ChangeDetectionStrategy,
  OnDestroy,
} from '@angular/core';
import { FormControl, Validators, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { setSearchTerm } from '@boards/store/groups/board-groups.actions';
import { selectSearchTerm } from '@boards/store/groups/board-groups.selectors';
import { Store } from '@ngrx/store';
import { Observable, Subject } from 'rxjs';
import { takeUntil, tap } from 'rxjs/operators';

import { MatIcon } from '@angular/material/icon';
import { MatTooltip } from '@angular/material/tooltip';

@Component({
    selector: 'app-board-groups-search',
    templateUrl: './board-groups-search.component.html',
    styleUrls: ['./board-groups-search.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [FormsModule, ReactiveFormsModule, MatIcon, MatTooltip]
})
export class BoardGroupsSearchComponent implements OnInit, OnDestroy {
  term$!: Observable<string>;
  onDestroy$ = new Subject<void>();

  termFormControl = new FormControl<string | null | undefined>('', [
    Validators.required,
    Validators.minLength(2),
    Validators.maxLength(64),
  ]);

  constructor(private store: Store) {}

  ngOnInit() {
    this.store
      .select(selectSearchTerm)
      .pipe(
        takeUntil(this.onDestroy$),
        tap((value) =>
          this.termFormControl.setValue(value, { emitEvent: false })
        )
      )
      .subscribe();
  }

  ngOnDestroy() {
    this.onDestroy$.next();
    this.onDestroy$.complete();
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

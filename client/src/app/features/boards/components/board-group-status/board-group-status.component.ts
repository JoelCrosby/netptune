import { Component, inject } from '@angular/core';
import { statusResource } from '@app/core/resources/status.resources';
import { toggleStatusSelection } from '@app/core/store/groups/board-groups.actions';
import {
  selectBoardGroupStatusOptions,
  selectBoardGroupsSelectedStatusCount,
} from '@app/core/store/groups/board-groups.selectors';
import { Store } from '@ngrx/store';
import { StatusFilterComponent } from '@static/components/status-filter/status-filter.component';

@Component({
  selector: 'app-board-group-status',
  imports: [StatusFilterComponent],
  template: `
    <app-status-filter
      [statuses]="statuses.value()"
      [selected]="selected()"
      [selectedCount]="selectedCount()"
      (toggled)="onToggled($event)" />
  `,
})
export class BoardGroupStatusComponent {
  private readonly store = inject(Store);

  readonly selected = this.store.selectSignal(selectBoardGroupStatusOptions);
  readonly statuses = statusResource();
  readonly selectedCount = this.store.selectSignal(
    selectBoardGroupsSelectedStatusCount
  );

  onToggled(status: number) {
    this.store.dispatch(toggleStatusSelection({ status }));
  }
}

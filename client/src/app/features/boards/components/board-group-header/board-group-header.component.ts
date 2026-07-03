import { Component, inject } from '@angular/core';
import { BoardGroupUsersComponent } from '@boards/components/board-group-users/board-group-users.component';
import { BoardGroupsSearchComponent } from '@boards/components/board-groups-search/board-groups-search.component';
import { BoardGroupsSelectionComponent } from '@boards/components/board-groups-selection/board-groups-selection.component';
import { BoardGroupStatusComponent } from '@boards/components/board-group-status/board-group-status.component';
import { TagFilterContainerComponent } from '@shared/components/tag-filter/tag-filter-container.component';
import { BoardGroupHeaderSeperatorComponent } from './board-group-header-seperator.component';
import { netptunePermissions } from '@app/core/auth/permissions';
import { selectHasPermission } from '@app/core/store/auth/auth.selectors';
import { Store } from '@ngrx/store';

@Component({
  selector: 'app-board-group-header',
  imports: [
    BoardGroupsSearchComponent,
    BoardGroupUsersComponent,
    TagFilterContainerComponent,
    BoardGroupStatusComponent,
    BoardGroupsSelectionComponent,
    BoardGroupHeaderSeperatorComponent,
  ],
  template: `<div class="flex flex-row items-center gap-2">
    <app-board-group-header-seperator />
    <app-board-groups-search />
    <app-board-group-header-seperator />
    <app-board-group-users />
    <app-board-group-header-seperator />
    @if (readTags()) {
      <app-tag-filter-container />
    }
    <app-board-group-header-seperator />
    @if (readStatus()) {
      <app-board-group-status />
    }
    <app-board-groups-selection />
  </div> `,
})
export class BoardGroupHeaderComponent {
  readonly store = inject(Store);

  readStatus = this.store.selectSignal(
    selectHasPermission(netptunePermissions.statuses.read)
  );

  readTags = this.store.selectSignal(
    selectHasPermission(netptunePermissions.tags.read)
  );
}

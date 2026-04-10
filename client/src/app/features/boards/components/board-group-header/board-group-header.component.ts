import { ChangeDetectionStrategy, Component } from '@angular/core';
import { BoardGroupTagsComponent } from '@boards/components/board-group-tags/board-group-tags.component';
import { BoardGroupUsersComponent } from '@boards/components/board-group-users/board-group-users.component';
import { BoardGroupsFlaggedComponent } from '@boards/components/board-groups-flagged/board-groups-flagged.component';
import { BoardGroupsSearchComponent } from '@boards/components/board-groups-search/board-groups-search.component';
import { BoardGroupsSelectionComponent } from '@boards/components/board-groups-selection/board-groups-selection.component';
import { BoardGroupHeaderSeperatorComponent } from './board-group-header-seperator.component';

@Component({
  selector: 'app-board-group-header',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    BoardGroupsSearchComponent,
    BoardGroupUsersComponent,
    BoardGroupTagsComponent,
    BoardGroupsFlaggedComponent,
    BoardGroupsSelectionComponent,
    BoardGroupHeaderSeperatorComponent,
  ],
  template: `<div class="flex flex-row items-center gap-2">
    <app-board-group-header-seperator />
    <app-board-groups-search />
    <app-board-group-header-seperator />
    <app-board-group-users />
    <app-board-group-header-seperator />
    <app-board-group-tags />
    <app-board-groups-flagged />
    <app-board-groups-selection />
  </div> `,
})
export class BoardGroupHeaderComponent {}

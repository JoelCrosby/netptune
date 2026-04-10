import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { toggleOnlyFlagged } from '@boards/store/groups/board-groups.actions';
import { selectOnlyFlagged } from '@boards/store/groups/board-groups.selectors';
import { LucideFlag } from '@lucide/angular';
import { Store } from '@ngrx/store';
import { BoardGroupHeaderActionComponent } from '../board-group-header/board-group-header-action.component';
@Component({
  selector: 'app-board-groups-flagged',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [BoardGroupHeaderActionComponent],
  template: `
    <app-board-group-header-action
      label="Filter Flagged tasks"
      [icon]="lucideFlag"
      (action)="onClicked()"
      [color]="onlyFlagged() ? 'warn' : undefined" />
  `,
})
export class BoardGroupsFlaggedComponent {
  private store = inject(Store);
  lucideFlag = LucideFlag;

  onlyFlagged = this.store.selectSignal(selectOnlyFlagged);

  onClicked() {
    this.store.dispatch(toggleOnlyFlagged());
  }
}

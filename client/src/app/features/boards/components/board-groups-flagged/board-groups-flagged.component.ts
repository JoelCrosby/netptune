import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { MatTooltip } from '@angular/material/tooltip';
import { toggleOnlyFlagged } from '@boards/store/groups/board-groups.actions';
import { selectOnlyFlagged } from '@boards/store/groups/board-groups.selectors';
import { Store } from '@ngrx/store';

@Component({
  selector: 'app-board-groups-flagged',
  templateUrl: './board-groups-flagged.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButton, MatTooltip, MatIcon],
})
export class BoardGroupsFlaggedComponent {
  private store = inject(Store);

  onyFlagged = this.store.selectSignal(selectOnlyFlagged);

  onClicked() {
    this.store.dispatch(toggleOnlyFlagged());
  }
}

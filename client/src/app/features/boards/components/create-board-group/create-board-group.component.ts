import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatRipple } from '@angular/material/core';
import { LucidePlus } from '@lucide/angular';
import { selectBoardIdAndIdentifier } from '@boards/store/groups/board-groups.selectors';
import { DialogService } from '@core/services/dialog.service';
import { BoardGroupDialogComponent } from '@entry/dialogs/board-group-dialog/board-group-dialog.component';
import { Store } from '@ngrx/store';

@Component({
  selector: 'app-create-board-group',
  templateUrl: './create-board-group.component.html',
  styleUrls: ['./create-board-group.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatRipple, LucidePlus],
})
export class CreateBoardGroupComponent {
  private dialog = inject(DialogService);
  private store = inject(Store);

  boardIdAndIdentifier = this.store.selectSignal(selectBoardIdAndIdentifier);

  onClick() {
    const [boardId, identifier] = this.boardIdAndIdentifier();

    this.dialog.open(BoardGroupDialogComponent, {
      width: '600px',
      data: {
        identifier,
        boardId,
      },
    });
  }
}

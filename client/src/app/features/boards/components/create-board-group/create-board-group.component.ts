import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatRipple } from '@angular/material/core';
import { LucidePlus } from '@lucide/angular';
import { selectBoardIdAndIdentifier } from '@boards/store/groups/board-groups.selectors';
import { DialogService } from '@core/services/dialog.service';
import { BoardGroupDialogComponent } from '@entry/dialogs/board-group-dialog/board-group-dialog.component';
import { Store } from '@ngrx/store';

@Component({
  selector: 'app-create-board-group',
  template: `
    <div
      role="button"
      tabindex="0"
      class="bg-background border-border/5 hover:bg-primary/[.08] hover:text-primary/80 m-[0.9rem] flex h-full flex-1 cursor-pointer flex-col items-center justify-center rounded border-4 border-dashed p-[0.6rem] text-sm font-medium tracking-[0.125px] text-[rgba(var(--foreground-rgb),.4)] transition-[background-color,margin,color] duration-200 ease-in-out hover:m-[0.4rem] hover:border-solid"
      (click)="onClick()"
      matRipple>
      <svg lucidePlus class="h-4 w-4"></svg>
      <span class="ml-[0.4rem]">Create Group</span>
    </div>
  `,
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

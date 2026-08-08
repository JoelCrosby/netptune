import { Component, inject } from '@angular/core';
import { LucidePlus } from '@lucide/angular';
import { BoardViewService } from '@core/services/board-view.service';
import { DialogService } from '@core/services/dialog.service';
import { BoardGroupDialogComponent } from '@entry/dialogs/board-group-dialog/board-group-dialog.component';

@Component({
  selector: 'app-create-board-group',
  template: `
    <div
      role="button"
      tabindex="0"
      class="bg-background border-border/5 hover:bg-primary/8 hover:text-primary/80 m-[0.9rem] flex h-full flex-1 cursor-pointer flex-col items-center justify-center rounded border-4 border-dashed p-[0.6rem] text-sm font-medium tracking-[0.125px] text-[rgba(var(--foreground-rgb),.4)] transition-[background-color,margin,color] duration-200 ease-in-out hover:m-[0.4rem] hover:border-solid"
      (click)="onClick()">
      <svg lucidePlus class="h-4 w-4"></svg>
      <span class="ml-[0.4rem]" i18n="Button that creates a board group">
        Create Group
      </span>
    </div>
  `,
  imports: [LucidePlus],
})
export class CreateBoardGroupComponent {
  private dialog = inject(DialogService);
  private boardView = inject(BoardViewService);

  onClick() {
    const board = this.boardView.board();

    if (!board) return;

    const { id: boardId } = board;

    this.dialog.open(BoardGroupDialogComponent, {
      width: '600px',
      data: {
        boardId,
      },
    });
  }
}

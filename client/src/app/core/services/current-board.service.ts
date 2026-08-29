import { Service, signal } from '@angular/core';
import { BoardViewModel } from '@core/models/view-models/board-view-model';

@Service()
export class CurrentBoardService {
  private readonly open = signal<BoardViewModel | undefined>(undefined);

  readonly board = this.open.asReadonly();

  set(board: BoardViewModel | undefined) {
    this.open.set(board);
  }
}

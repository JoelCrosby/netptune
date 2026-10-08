import { inject, Service, signal } from '@angular/core';
import { BoardViewModel } from '@core/models/view-models/board-view-model';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';
import { onChange } from '@core/util/signals';

@Service()
export class CurrentBoardService {
  private readonly open = signal<BoardViewModel | undefined>(undefined);

  readonly board = this.open.asReadonly();

  constructor() {
    // The guard switches workspace before the old view is torn down.
    onChange(inject(CurrentWorkspaceService).id, () => this.set(undefined));
  }

  set(board: BoardViewModel | undefined) {
    this.open.set(board);
  }
}

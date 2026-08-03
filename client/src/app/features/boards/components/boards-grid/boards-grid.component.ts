import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import * as BoardSelectors from '@app/core/store/boards/boards.selectors';
import { Store } from '@ngrx/store';
import { BoardsGridCardComponent } from './boards-grid-card.component';

@Component({
  selector: 'app-boards-grid',
  imports: [RouterLink, BoardsGridCardComponent],
  host: { class: 'block' },
  template: `
    <div class="flex flex-col gap-8">
      @for (group of groups(); track group.projectName) {
        <section>
          <h2 class="font-overpass mb-3 text-[1.4rem] font-normal">
            {{ group.projectName }}
          </h2>

          <div
            class="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
            @for (board of group.boards; track board.id) {
              <a
                class="focus-visible:ring-primary block rounded-lg focus-visible:ring-2 focus-visible:ring-offset-2 focus-visible:outline-none"
                [routerLink]="['.', board.identifier]">
                <app-boards-grid-card [board]="board" />
              </a>
            }
          </div>
        </section>
      }
    </div>
  `,
})
export class BoardsGridComponent {
  private store = inject(Store);

  groups = this.store.selectSignal(BoardSelectors.selectAllBoards);
}

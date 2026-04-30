import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import * as BoardSelectors from '@app/core/store/boards/boards.selectors';
import { Store } from '@ngrx/store';
import { CardGroupComponent } from '@static/components/card/card-group.component';
import { BoardsGridCardComponent } from './boards-grid-card.component';

@Component({
  selector: 'app-boards-grid',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CardGroupComponent,
    RouterLinkActive,
    RouterLink,
    BoardsGridCardComponent,
  ],
  template: `
    @for (group of groups(); track group) {
      <div class="inline-flex flex-col gap-2">
        <h1 class="font-overpass my-2 text-[1.4rem] font-normal">
          {{ group.projectName }}
        </h1>
        <app-card-group>
          @for (board of group.boards; track board) {
            <a
              [routerLink]="['.', board.identifier]"
              routerLinkActive="router-link-active">
              <app-boards-grid-card [board]="board" />
            </a>
          }
        </app-card-group>
      </div>
    }
  `,
})
export class BoardsGridComponent {
  private store = inject(Store);

  groups = this.store.selectSignal(BoardSelectors.selectAllBoards);
}

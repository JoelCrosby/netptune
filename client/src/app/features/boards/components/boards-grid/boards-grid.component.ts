import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatIcon } from '@angular/material/icon';
import { RouterLink, RouterLinkActive } from '@angular/router';
import * as BoardSelectors from '@boards/store/boards/boards.selectors';
import { Store } from '@ngrx/store';
import { CardGroupComponent } from '@static/components/card/card-group.component';
import { CardHeaderImageComponent } from '@static/components/card/card-header-image.component';
import { CardHeaderComponent } from '@static/components/card/card-header.component';
import { CardSubtitleComponent } from '@static/components/card/card-subtitle.component';
import { CardTitleComponent } from '@static/components/card/card-title.component';
import { CardComponent } from '@static/components/card/card.component';

@Component({
  selector: 'app-boards-grid',
  templateUrl: './boards-grid.component.html',
  styleUrls: ['./boards-grid.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CardGroupComponent,
    RouterLinkActive,
    RouterLink,
    CardComponent,
    CardHeaderImageComponent,
    MatIcon,
    CardHeaderComponent,
    CardTitleComponent,
    CardSubtitleComponent,
  ],
})
export class BoardsGridComponent {
  private store = inject(Store);

  groups = this.store.selectSignal(BoardSelectors.selectAllBoards);
}

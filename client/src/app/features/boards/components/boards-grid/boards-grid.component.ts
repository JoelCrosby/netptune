import { ChangeDetectionStrategy, Component, OnInit } from '@angular/core';
import * as BoardSelectors from '@boards/store/boards/boards.selectors';
import { BoardsViewModel } from '@core/models/view-models/boards-view-model';
import { Store } from '@ngrx/store';
import { Observable } from 'rxjs';
import { NgFor, AsyncPipe } from '@angular/common';
import { CardGroupComponent } from '../../../../static/components/card/card-group.component';
import { RouterLinkActive, RouterLink } from '@angular/router';
import { CardComponent } from '../../../../static/components/card/card.component';
import { CardHeaderImageComponent } from '../../../../static/components/card/card-header-image.component';
import { MatIcon } from '@angular/material/icon';
import { CardHeaderComponent } from '../../../../static/components/card/card-header.component';
import { CardTitleComponent } from '../../../../static/components/card/card-title.component';
import { CardSubtitleComponent } from '../../../../static/components/card/card-subtitle.component';

@Component({
    selector: 'app-boards-grid',
    templateUrl: './boards-grid.component.html',
    styleUrls: ['./boards-grid.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [NgFor, CardGroupComponent, RouterLinkActive, RouterLink, CardComponent, CardHeaderImageComponent, MatIcon, CardHeaderComponent, CardTitleComponent, CardSubtitleComponent, AsyncPipe]
})
export class BoardsGridComponent implements OnInit {
  groups$!: Observable<BoardsViewModel[]>;

  constructor(private store: Store) {}

  ngOnInit() {
    this.groups$ = this.store.select(BoardSelectors.selectAllBoards);
  }
}

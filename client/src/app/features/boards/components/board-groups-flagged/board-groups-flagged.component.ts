import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { toggleOnlyFlagged } from '@boards/store/groups/board-groups.actions';
import { selectOnlyFlagged } from '@boards/store/groups/board-groups.selectors';
import { Store } from '@ngrx/store';
import { Observable } from 'rxjs';
import { MatButton } from '@angular/material/button';
import { MatTooltip } from '@angular/material/tooltip';
import { MatIcon } from '@angular/material/icon';
import { AsyncPipe } from '@angular/common';

@Component({
    selector: 'app-board-groups-flagged',
    templateUrl: './board-groups-flagged.component.html',
    styleUrls: ['./board-groups-flagged.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [MatButton, MatTooltip, MatIcon, AsyncPipe]
})
export class BoardGroupsFlaggedComponent implements OnInit {
  onyFlagged$!: Observable<boolean>;

  constructor(private store: Store) {}

  ngOnInit() {
    this.onyFlagged$ = this.store.select(selectOnlyFlagged);
  }

  onClicked() {
    this.store.dispatch(toggleOnlyFlagged());
  }
}

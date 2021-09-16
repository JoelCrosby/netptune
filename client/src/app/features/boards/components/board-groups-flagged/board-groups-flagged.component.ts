import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { toggleOnlyFlagged } from '@boards/store/groups/board-groups.actions';
import { selectOnlyFlagged } from '@boards/store/groups/board-groups.selectors';
import { AppState } from '@core/core.state';
import { Store } from '@ngrx/store';
import { Observable } from 'rxjs';

@Component({
  selector: 'app-board-groups-flagged',
  templateUrl: './board-groups-flagged.component.html',
  styleUrls: ['./board-groups-flagged.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BoardGroupsFlaggedComponent implements OnInit {
  onyFlagged$!: Observable<boolean>;

  constructor(private store: Store<AppState>) {}

  ngOnInit() {
    this.onyFlagged$ = this.store.select(selectOnlyFlagged);
  }

  onClicked() {
    this.store.dispatch(toggleOnlyFlagged());
  }
}

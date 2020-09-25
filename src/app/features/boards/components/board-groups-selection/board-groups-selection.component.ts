import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import {
  selectSelectedTasksCount,
  selectSelectedTasks,
} from '@boards/store/groups/board-groups.selectors';
import { Store } from '@ngrx/store';
import { Observable } from 'rxjs';

@Component({
  selector: 'app-board-groups-selection',
  templateUrl: './board-groups-selection.component.html',
  styleUrls: ['./board-groups-selection.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BoardGroupsSelectionComponent implements OnInit {
  selected$: Observable<number[]>;
  count$: Observable<number>;

  constructor(private store: Store) {}

  ngOnInit() {
    this.selected$ = this.store.select(selectSelectedTasks);
    this.count$ = this.store.select(selectSelectedTasksCount);
  }
}

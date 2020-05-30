import { clearSelectedTask } from '@project-tasks/store/tasks.actions';
import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { Store } from '@ngrx/store';
import { AppState } from '@core/core.state';
import { selectSelectedTask } from '@project-tasks/store/tasks.selectors';

@Component({
  selector: 'app-task-detail',
  templateUrl: './task-detail.component.html',
  styleUrls: ['./task-detail.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TaskDetailComponent implements OnInit {
  $task = this.store.select(selectSelectedTask);

  constructor(private store: Store<AppState>) {}

  ngOnInit() {}

  closeClicked() {
    this.store.dispatch(clearSelectedTask());
  }
}

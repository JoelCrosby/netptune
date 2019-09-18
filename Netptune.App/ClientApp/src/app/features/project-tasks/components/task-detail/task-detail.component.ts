import { Component, OnInit } from '@angular/core';
import { Store } from '@ngrx/store';
import { AppState } from '@core/core.state';
import { selectSelectedTask } from '../../store/project-tasks.selectors';

@Component({
  selector: 'app-task-detail',
  templateUrl: './task-detail.component.html',
  styleUrls: ['./task-detail.component.scss'],
})
export class TaskDetailComponent implements OnInit {
  $task = this.store.select(selectSelectedTask);

  constructor(private store: Store<AppState>) {}

  ngOnInit() {}
}

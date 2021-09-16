import {
  ChangeDetectionStrategy,
  Component,
  Input,
  OnInit,
} from '@angular/core';
import { AppState } from '@core/core.state';

import { TaskViewModel } from '@core/models/view-models/project-task-dto';
import { selectCurrentWorkspaceIdentifier } from '@core/store/workspaces/workspaces.selectors';
import { Store } from '@ngrx/store';
import { first, tap } from 'rxjs/operators';

@Component({
  selector: 'app-task-list-group',
  templateUrl: './task-list-group.component.html',
  styleUrls: ['./task-list-group.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TaskListGroupComponent implements OnInit {
  @Input() groupName!: string;
  @Input() tasks!: TaskViewModel[];
  @Input() header!: string;
  @Input() emptyMessage!: string;

  workspaceIdentifier?: string;

  constructor(private store: Store<AppState>) {}

  ngOnInit() {
    this.store
      .select(selectCurrentWorkspaceIdentifier)
      .pipe(
        first(),
        tap((identifier) => {
          this.workspaceIdentifier = identifier;
        })
      )
      .subscribe();
  }

  trackByTask(_: number, task: TaskViewModel) {
    return task.id;
  }
}

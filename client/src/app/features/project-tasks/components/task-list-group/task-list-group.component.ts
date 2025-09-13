import { ChangeDetectionStrategy, Component, Input } from '@angular/core';
import { TaskViewModel } from '@core/models/view-models/project-task-dto';

import { CdkVirtualScrollViewport, CdkFixedSizeVirtualScroll, CdkVirtualForOf } from '@angular/cdk/scrolling';
import { TaskListItemComponent } from '../task-list-item/task-list-item.component';
import { TaskInlineComponent } from '../task-inline/task-inline.component';

@Component({
    selector: 'app-task-list-group',
    templateUrl: './task-list-group.component.html',
    styleUrls: ['./task-list-group.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    imports: [CdkVirtualScrollViewport, CdkFixedSizeVirtualScroll, CdkVirtualForOf, TaskListItemComponent, TaskInlineComponent]
})
export class TaskListGroupComponent {
  @Input() groupName!: string;
  @Input() tasks!: TaskViewModel[] | null;
  @Input() header!: string;
  @Input() emptyMessage!: string;

  trackByTask(_: number, task: TaskViewModel) {
    return task.id;
  }
}

import { Component, inject, input, linkedSignal } from '@angular/core';
import { hasPermission } from '@core/auth/has-permission';
import { PERMISSIONS } from '@core/auth/permissions';
import { TaskCommandsService } from '@core/services/task-commands.service';
import { TaskTagRowComponent } from '../pickers/task-tag-row.component';
import { TaskDetailService } from '../task-detail.service';

@Component({
  selector: 'app-task-detail-tag-row',
  imports: [TaskTagRowComponent],
  // The host collapses so the inner row is the flex item, exactly as this component
  // rendered before it delegated to the shared tag row.
  host: { class: 'contents' },
  template: `
    <app-task-tag-row
      [tags]="selectedTags()"
      [size]="size()"
      [editable]="canUpdate()"
      (added)="addTag($event)"
      (removed)="removeTag($event)" />
  `,
})
export class TaskDetailTagRowComponent {
  readonly size = input<'sm' | 'md'>('sm');

  private readonly taskDetail = inject(TaskDetailService);
  private readonly taskCommands = inject(TaskCommandsService);

  readonly task = this.taskDetail.task;
  readonly canUpdate = hasPermission(PERMISSIONS.tasks.update);

  readonly selectedTags = linkedSignal(() => this.task()?.tags ?? []);

  protected addTag(tag: string) {
    const task = this.task();

    if (!task) return;

    this.selectedTags.update((tags) => [...tags, tag]);
    this.taskCommands.addTag({ systemId: task.systemId, tag });
  }

  protected removeTag(tag: string) {
    const task = this.task();

    if (!task) return;

    this.selectedTags.update((tags) => tags.filter((item) => item !== tag));
    this.taskCommands.removeTag({ systemId: task.systemId, tag });
  }
}

import { Component, computed, inject } from '@angular/core';
import { PinnedTask, TaskPin } from '@core/models/task-pin';
import { pinnedTasksResource } from '@core/resources/task-pin.resource';
import { DialogService } from '@core/services/dialog.service';
import { PinCommandsService } from '@core/services/pin-commands.service';
import { pinnedTaskFilter } from '@core/util/pinned-task-filter';
import { TaskDetailDialogComponent } from '@entry/dialogs/task-detail-dialog/task-detail-dialog.component';
import { LucidePin } from '@lucide/angular';
import { PinnedTaskRowComponent } from '@shared/components/pinned-task-row/pinned-task-row.component';
import { PanelComponent } from '@static/components/panel.component';
import { PanelHeaderComponent } from '@static/components/panel-header.component';
import { TabGroupComponent } from '@static/components/tab-group/tab-group.component';

@Component({
  selector: 'app-dashboard-pinned-card',
  imports: [
    PanelComponent,
    PanelHeaderComponent,
    PinnedTaskRowComponent,
    TabGroupComponent,
  ],
  template: `
    @if (pinnedTasks().length) {
      <app-panel surface="card">
        <app-panel-header
          density="comfortable"
          [icon]="pinIcon"
          i18n-heading="Heading of the dashboard card listing pinned tasks"
          heading="Pinned"
          i18n-description="
            Description of the dashboard card listing pinned tasks
          "
          description="Tasks you and your team are keeping in view">
          <app-tab-group
            panelHeaderActions
            variant="island"
            [tabs]="filter.tabs()"
            [(value)]="filter.filter" />
        </app-panel-header>

        @for (
          pinned of filter.visible();
          track pinned.task.id;
          let first = $first
        ) {
          <app-pinned-task-row
            [class.border-t]="!first"
            [class.border-border]="!first"
            [pinned]="pinned"
            (opened)="onTaskClicked(pinned)"
            (unpinned)="onUnpinClicked($event)" />
        }
      </app-panel>
    }
  `,
})
export class DashboardPinnedCardComponent {
  private readonly pinsRef = pinnedTasksResource();
  private readonly pinCommands = inject(PinCommandsService);
  private readonly dialog = inject(DialogService);

  protected readonly pinIcon = LucidePin;

  protected readonly pinnedTasks = computed(() => this.pinsRef.value() ?? []);

  protected readonly filter = pinnedTaskFilter(this.pinnedTasks);

  protected onUnpinClicked(pin: TaskPin) {
    this.pinCommands.unpin(pin);
  }

  protected onTaskClicked(pinned: PinnedTask) {
    this.dialog.open(TaskDetailDialogComponent, {
      width: TaskDetailDialogComponent.width,
      height: TaskDetailDialogComponent.height,
      data: { systemId: pinned.task.systemId },
      panelClass: TaskDetailDialogComponent.panelClass,
      autoFocus: false,
    });
  }
}

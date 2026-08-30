import { Component, computed, inject } from '@angular/core';
import { hasPermission } from '@core/auth/has-permission';
import { PERMISSIONS } from '@core/auth/permissions';
import { Status } from '@core/models/status';
import { statusResource } from '@core/resources/status.resource';
import { LucideEllipsis } from '@lucide/angular';
import { cn } from '@static/components/button/button.variants';
import { ColorSwatchComponent } from '@static/components/color-swatch/color-swatch.component';
import { DropdownMenuComponent } from '@static/components/dropdown-menu/dropdown-menu.component';
import { MenuItemComponent } from '@static/components/dropdown-menu/menu-item.component';
import { EYEBROW } from '../task-detail-styles';
import { TaskDetailService } from '../task-detail.service';

const VISIBLE_SEGMENTS = 3;

@Component({
  selector: 'app-task-detail-status-segments',
  imports: [
    ColorSwatchComponent,
    DropdownMenuComponent,
    MenuItemComponent,
    LucideEllipsis,
  ],
  host: { class: 'flex flex-col gap-2.5' },
  template: `
    @if (readStatus() && task(); as task) {
      <div [class]="eyebrowClass" id="task-status-eyebrow">
        <span i18n="Field heading for the task status">Status</span>
      </div>

      <div
        class="border-foreground/8 flex h-8 items-stretch overflow-hidden rounded-lg border"
        role="group"
        aria-labelledby="task-status-eyebrow">
        @for (status of segments(); track status.id; let first = $first) {
          <button
            type="button"
            [class]="segmentClass(status.id === task.statusId, first)"
            [attr.aria-pressed]="status.id === task.statusId"
            [disabled]="!canUpdate()"
            (click)="taskDetail.setStatus(status.id)">
            {{ status.name }}
          </button>
        }

        @if (overflow().length) {
          <button
            #overflowButton
            type="button"
            [class]="overflowClass()"
            aria-haspopup="menu"
            i18n-aria-label="
              Accessible label for the control that lists the remaining statuses
            "
            aria-label="More statuses"
            [disabled]="!canUpdate()"
            (click)="menu.toggle(overflowButton)">
            <svg lucideEllipsis class="h-4 w-4"></svg>
          </button>

          <app-dropdown-menu #menu xPosition="before">
            @for (status of overflow(); track status.id) {
              <button
                app-menu-item
                [disabled]="status.id === task.statusId"
                (click)="taskDetail.setStatus(status.id); menu.close()">
                @if (status.color) {
                  <app-color-swatch [color]="status.color" />
                }
                {{ status.name }}
              </button>
            }
          </app-dropdown-menu>
        }
      </div>
    }
  `,
})
export class TaskDetailStatusSegmentsComponent {
  readonly taskDetail = inject(TaskDetailService);

  readonly task = this.taskDetail.task;
  readonly eyebrowClass = EYEBROW;

  readonly canUpdate = hasPermission(PERMISSIONS.tasks.update);
  readonly readStatus = hasPermission(PERMISSIONS.statuses.read);

  private readonly statuses = statusResource();

  readonly segments = computed<Status[]>(() => {
    const statuses = this.statuses.value();
    const visible = statuses.slice(0, VISIBLE_SEGMENTS);
    const current = statuses.find(
      (status) => status.id === this.task()?.statusId
    );

    if (!current || visible.some((status) => status.id === current.id)) {
      return visible;
    }

    return [...visible.slice(0, VISIBLE_SEGMENTS - 1), current];
  });

  readonly overflow = computed(() => {
    const shown = new Set(this.segments().map((status) => status.id));

    return this.statuses.value().filter((status) => !shown.has(status.id));
  });

  readonly overflowClass = computed(() => {
    const selected = this.overflow().some(
      (status) => status.id === this.task()?.statusId
    );

    return cn(
      'border-foreground/8 text-muted hover:bg-hover hover:text-foreground flex w-9 shrink-0 cursor-pointer items-center justify-center border-l transition-colors',
      selected && 'bg-primary/22'
    );
  });

  segmentClass(selected: boolean, first: boolean) {
    return cn(
      'flex-1 cursor-pointer truncate px-2 text-xs transition-colors disabled:cursor-default',
      !first && 'border-foreground/8 border-l',
      selected
        ? 'bg-primary/22 text-foreground font-semibold'
        : 'text-muted hover:bg-hover hover:text-foreground font-medium'
    );
  }
}

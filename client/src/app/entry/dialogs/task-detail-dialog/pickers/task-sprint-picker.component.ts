import { Component, computed, input, model } from '@angular/core';
import { sprintStatusLabels } from '@core/enums/sprint-status';
import { sprintResource } from '@core/resources/sprint.resource';
import { DropdownMenuComponent } from '@static/components/dropdown-menu/dropdown-menu.component';
import { MenuItemComponent } from '@static/components/dropdown-menu/menu-item.component';

@Component({
  selector: 'app-task-sprint-picker',
  imports: [DropdownMenuComponent, MenuItemComponent],
  template: `
    <button
      type="button"
      [class]="buttonClass()"
      [disabled]="disabled()"
      [attr.aria-label]="ariaLabel"
      aria-haspopup="menu"
      (click)="menu.toggle($any($event.currentTarget))">
      <ng-content />
    </button>

    <app-dropdown-menu #menu>
      <small class="text-muted block px-3 py-1 text-xs">
        <span i18n="Heading above the sprint options">Change Sprint</span>
      </small>
      @if (value()) {
        <button app-menu-item (click)="selectSprint(null); menu.close()">
          <span i18n="Option that clears the task's sprint">No Sprint</span>
        </button>
      }
      @for (sprint of sprints(); track sprint.id) {
        <button
          app-menu-item
          [disabled]="sprint.id === value()"
          (click)="selectSprint(sprint.id); menu.close()">
          <span>{{ sprint.name }}</span>
          <span class="text-muted text-xs">
            {{ sprintStatusLabels[sprint.status] }}
          </span>
        </button>
      }
    </app-dropdown-menu>
  `,
})
export class TaskSprintPickerComponent {
  readonly value = model<number | null>(null);
  readonly projectId = input<number | null>(null);
  readonly disabled = input(false);
  readonly buttonClass = input('');

  readonly sprintStatusLabels = sprintStatusLabels;

  readonly ariaLabel = $localize`:Accessible label for the control that changes a task's sprint:Change sprint`;

  private readonly sprintsResource = sprintResource();

  readonly sprints = computed(() => {
    const projectId = this.projectId();
    const sprints = this.sprintsResource.value();

    if (projectId === null) return sprints;

    return sprints.filter((sprint) => sprint.projectId === projectId);
  });

  selectSprint(sprintId: number | null) {
    if (sprintId === this.value()) return;

    this.value.set(sprintId);
  }
}

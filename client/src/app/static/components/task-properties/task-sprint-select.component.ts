import { Component, computed, input, model } from '@angular/core';
import { sprintStatusLabels } from '@core/enums/sprint-status';
import { sprintResource } from '@core/resources/sprint.resource';
import { ChipListboxComponent } from '@static/components/chip/chip-listbox.component';
import { ChipOptionComponent } from '@static/components/chip/chip-option.component';
import { DropdownMenuComponent } from '@static/components/dropdown-menu/dropdown-menu.component';
import { MenuItemComponent } from '@static/components/dropdown-menu/menu-item.component';

@Component({
  selector: 'app-task-sprint-select',
  imports: [
    ChipListboxComponent,
    ChipOptionComponent,
    DropdownMenuComponent,
    MenuItemComponent,
  ],
  template: `
    <app-chip-listbox>
      <button
        app-chip-option
        [loading]="loading()"
        (click)="sprintsMenu.toggle($any($event.currentTarget))">
        {{ label() }}
      </button>
    </app-chip-listbox>
    <app-dropdown-menu #sprintsMenu>
      <small class="block px-3 py-1 text-xs text-neutral-500">
        Change Sprint
      </small>
      @if (value()) {
        <button app-menu-item (click)="selectSprint(null); sprintsMenu.close()">
          No Sprint
        </button>
      }
      @for (sprint of sprints(); track sprint.id) {
        <button
          app-menu-item
          [disabled]="sprint.id === value()"
          (click)="selectSprint(sprint.id); sprintsMenu.close()">
          <span>{{ sprint.name }}</span>
          <span class="text-xs text-neutral-500">
            {{ sprintStatusLabels[sprint.status] }}
          </span>
        </button>
      }
    </app-dropdown-menu>
  `,
})
export class TaskSprintSelectComponent {
  readonly value = model<number | null>(null);
  readonly projectId = input<number | null>(null);
  readonly loading = input(false);
  readonly fallbackLabel = input('No Sprint');

  readonly sprintStatusLabels = sprintStatusLabels;

  readonly sprintsResource = sprintResource();

  readonly sprints = computed(() => {
    const projectId = this.projectId();
    const sprints = this.sprintsResource.value();

    if (projectId === null) return sprints;

    return sprints.filter((sprint) => sprint.projectId === projectId);
  });

  readonly label = computed(() => {
    const sprint = this.sprintsResource
      .value()
      .find((sprint) => sprint.id === this.value());

    return sprint?.name ?? this.fallbackLabel();
  });

  selectSprint(sprintId: number | null) {
    if (sprintId === this.value()) return;
    this.value.set(sprintId);
  }
}

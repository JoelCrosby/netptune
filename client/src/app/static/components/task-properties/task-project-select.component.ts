import { Component, computed, input, model } from '@angular/core';
import { projectResource } from '@core/resources/project.resource';
import { ChipListboxComponent } from '@static/components/chip/chip-listbox.component';
import { ChipOptionComponent } from '@static/components/chip/chip-option.component';
import { DropdownMenuComponent } from '@static/components/dropdown-menu/dropdown-menu.component';
import { MenuItemComponent } from '@static/components/dropdown-menu/menu-item.component';

@Component({
  selector: 'app-task-project-select',
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
        (click)="projectsMenu.toggle($any($event.currentTarget))">
        {{ label() }}
      </button>
    </app-chip-listbox>
    <app-dropdown-menu #projectsMenu>
      <small class="block px-3 py-1 text-xs text-neutral-500">
        Change Project
      </small>
      @for (project of projects.value(); track project.id) {
        <button
          app-menu-item
          [disabled]="project.id === value()"
          (click)="value.set(project.id); projectsMenu.close()">
          {{ project.name }}
        </button>
      }
    </app-dropdown-menu>
  `,
})
export class TaskProjectSelectComponent {
  readonly value = model<number | null>(null);
  readonly fallbackLabel = input('Select Project');

  readonly projects = projectResource();

  readonly label = computed(() => {
    const project = this.projects
      .value()
      .find((project) => project.id === this.value());

    return project?.name ?? this.fallbackLabel();
  });
}

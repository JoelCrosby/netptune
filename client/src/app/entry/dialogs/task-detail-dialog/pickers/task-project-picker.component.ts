import { Component, input, model } from '@angular/core';
import { projectResource } from '@core/resources/project.resource';
import { DropdownMenuComponent } from '@static/components/dropdown-menu/dropdown-menu.component';
import { MenuItemComponent } from '@static/components/dropdown-menu/menu-item.component';

@Component({
  selector: 'app-task-project-picker',
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
        <span i18n="Heading above the project options">Change Project</span>
      </small>
      @for (project of projects.value(); track project.id) {
        <button
          app-menu-item
          [disabled]="project.id === value()"
          (click)="value.set(project.id); menu.close()">
          {{ project.name }}
        </button>
      }
    </app-dropdown-menu>
  `,
})
export class TaskProjectPickerComponent {
  readonly value = model<number | null>(null);
  readonly disabled = input(false);
  readonly buttonClass = input('');

  readonly projects = projectResource();

  readonly ariaLabel = $localize`:Accessible label for the control that moves a task to another project:Change project`;
}

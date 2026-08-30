import { Component, computed, input, model } from '@angular/core';
import { Status } from '@core/models/status';
import { statusResource } from '@core/resources/status.resource';
import { ColorSwatchComponent } from '@static/components/color-swatch/color-swatch.component';
import { DropdownMenuComponent } from '@static/components/dropdown-menu/dropdown-menu.component';
import { MenuItemComponent } from '@static/components/dropdown-menu/menu-item.component';

@Component({
  selector: 'app-task-status-picker',
  imports: [ColorSwatchComponent, DropdownMenuComponent, MenuItemComponent],
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
        <span i18n="Heading above the task status options">Change Status</span>
      </small>
      @for (status of statuses.value(); track status.id) {
        <button
          app-menu-item
          [disabled]="status.id === value()"
          (click)="selectStatus(status); menu.close()">
          @if (status.color) {
            <app-color-swatch [color]="status.color" />
          }
          {{ status.name }}
        </button>
      }
    </app-dropdown-menu>
  `,
})
export class TaskStatusPickerComponent {
  readonly value = model<number | null>(null);
  readonly disabled = input(false);
  readonly buttonClass = input('');

  readonly statuses = statusResource();

  readonly ariaLabel = $localize`:Accessible label for the control that changes a task's status:Change status`;

  readonly options = computed(() => this.statuses.value());

  selectStatus(status: Status) {
    if (status.id === this.value()) return;

    this.value.set(status.id);
  }
}

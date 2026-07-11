import { Component, computed, input, model } from '@angular/core';
import { Status } from '@core/models/status';
import { statusResource } from '@core/resources/status.resources';
import { ChipListboxComponent } from '@static/components/chip/chip-listbox.component';
import { ChipOptionComponent } from '@static/components/chip/chip-option.component';
import { DropdownMenuComponent } from '@static/components/dropdown-menu/dropdown-menu.component';
import { MenuItemComponent } from '@static/components/dropdown-menu/menu-item.component';

@Component({
  selector: 'app-task-status-select',
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
        [showChevron]="!loading()"
        [disabled]="loading()"
        [loading]="loading()"
        (click)="statusMenu.toggle($any($event.currentTarget))">
        {{ label() }}
      </button>
    </app-chip-listbox>
    <app-dropdown-menu #statusMenu>
      <small class="block px-3 py-1 text-xs text-neutral-500">
        Change Status
      </small>
      @for (status of statuses.value(); track status.id) {
        <button
          app-menu-item
          [disabled]="status.id === value() || loading()"
          (click)="selectStatus(status); statusMenu.close()">
          @if (status.color) {
            <span
              class="h-2.5 w-2.5 rounded-full"
              [style.background-color]="status.color"></span>
          }
          {{ status.name }}
        </button>
      }
    </app-dropdown-menu>
  `,
})
export class TaskStatusSelectComponent {
  readonly value = model<number | null>(null);
  readonly loading = input(false);
  readonly fallbackLabel = input('Default');

  readonly statuses = statusResource();

  readonly label = computed(() => {
    const status = this.statuses
      .value()
      .find((status) => status.id === this.value());

    return status?.name ?? this.fallbackLabel();
  });

  selectStatus(status: Status) {
    if (status.id === this.value()) return;
    this.value.set(status.id);
  }
}

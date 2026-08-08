import { Component, inject } from '@angular/core';
import { BoardViewService } from '@core/services/board-view.service';
import { TaskFilterService } from '@core/services/task-filter.service';
import {
  AvatarFilterComponent,
  AvatarFilterOption,
} from '@static/components/avatar-filter/avatar-filter.component';

@Component({
  selector: 'app-board-group-users',
  imports: [AvatarFilterComponent],
  template: `
    <app-avatar-filter
      [options]="users()"
      [onlineLabel]="viewingLabel"
      (optionClicked)="onUserClicked($event)" />
  `,
})
export class BoardGroupUsersComponent {
  private readonly boardView = inject(BoardViewService);
  private readonly filters = inject(TaskFilterService);

  /**
   * Angular disallows `i18n-onlineLabel` because the attribute name starts with
   * "on" and is treated as an event property, so the copy is localised here and
   * passed as a binding.
   */
  readonly viewingLabel = $localize`:Presence state appended after a person's name on a board:is viewing this board`;

  readonly users = this.boardView.userOptions;

  onUserClicked(option: AvatarFilterOption) {
    const selected = this.filters.filters().users ?? [];

    const users = selected.includes(option.id)
      ? selected.filter((id) => id !== option.id)
      : [...selected, option.id];

    this.filters.update({ users });
  }
}

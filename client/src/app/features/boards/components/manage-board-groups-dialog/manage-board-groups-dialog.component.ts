import { Component, computed, inject } from '@angular/core';
import {
  selectAllBoardGroupsWithSelection,
  selectSelectedBoard,
} from '@app/core/store/groups/board-groups.selectors';
import { BOARDS_HIDDEN_GROUP_IDS } from '@core/models/user-preferences';
import { UserPreferencesService } from '@core/services/user-preferences.service';
import {
  hiddenGroupIdsForBoard,
  withBoardHiddenGroups,
} from '@boards/util/hidden-board-groups';
import { Store } from '@ngrx/store';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { CheckboxComponent } from '@static/components/checkbox/checkbox.component';
import { DialogContentComponent } from '@static/components/dialog-content/dialog-content.component';
import { DialogTitleComponent } from '@static/components/dialog-title/dialog-title.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { DialogCloseDirective } from '@static/directives/dialog-close.directive';

@Component({
  selector: 'app-manage-board-groups-dialog',
  imports: [
    DialogTitleComponent,
    DialogContentComponent,
    DialogActionsDirective,
    DialogCloseDirective,
    CheckboxComponent,
    StrokedButtonComponent,
  ],
  template: `
    <app-dialog-title>Manage Groups</app-dialog-title>

    <app-dialog-content>
      <p class="text-foreground/60 mb-4 text-sm">
        Uncheck a group to hide it from this board. This only affects your view.
      </p>

      @if (groups().length) {
        <div class="flex flex-col gap-4">
          @for (group of groups(); track group.id) {
            <app-checkbox
              [checked]="isVisible(group.id)"
              (changed)="onToggle(group.id, $event)">
              {{ group.name }}
            </app-checkbox>
          }
        </div>
      } @else {
        <p class="text-foreground/60 text-sm">This board has no groups.</p>
      }
    </app-dialog-content>

    <div app-dialog-actions align="end">
      <button app-stroked-button app-dialog-close>Done</button>
    </div>
  `,
})
export class ManageBoardGroupsDialogComponent {
  private store = inject(Store);
  private preferences = inject(UserPreferencesService);

  protected groups = this.store.selectSignal(selectAllBoardGroupsWithSelection);
  private board = this.store.selectSignal(selectSelectedBoard);

  private hiddenIds = computed(() => {
    const boardId = this.board()?.id;

    if (boardId === undefined) return new Set<number>();

    const value = this.preferences.effectiveValueFor(BOARDS_HIDDEN_GROUP_IDS);

    return new Set(hiddenGroupIdsForBoard(value, boardId));
  });

  isVisible(groupId: number): boolean {
    return !this.hiddenIds().has(groupId);
  }

  onToggle(groupId: number, visible: boolean) {
    const boardId = this.board()?.id;

    if (boardId === undefined) return;

    const hidden = new Set(this.hiddenIds());

    if (visible) {
      hidden.delete(groupId);
    } else {
      hidden.add(groupId);
    }

    // Prune ids for groups that no longer exist so the preference stays
    // resilient against deleted or modified groups.
    const existing = new Set(this.groups().map((group) => group.id));
    const next = [...hidden].filter((id) => existing.has(id));

    const value = this.preferences.effectiveValueFor(BOARDS_HIDDEN_GROUP_IDS);

    this.preferences
      .updateValue(
        BOARDS_HIDDEN_GROUP_IDS,
        'workspace',
        withBoardHiddenGroups(value, boardId, next)
      )
      .subscribe();
  }
}

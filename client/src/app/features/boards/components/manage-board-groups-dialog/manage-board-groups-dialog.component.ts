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
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
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
    FlatButtonComponent,
    StrokedButtonComponent,
  ],
  template: `
    <app-dialog-title>Manage Groups</app-dialog-title>

    <app-dialog-content>
      <p class="text-foreground/60 mb-4 text-sm">
        Uncheck a group to hide it from this board. This only affects your view.
      </p>

      @if (groups().length) {
        <div
          class="border-border mb-2 flex flex-wrap items-center justify-between gap-x-4 gap-y-2 border-b pb-3">
          <span class="text-foreground/50 text-xs font-medium tracking-wide">
            {{ visibleCount() }} of {{ groups().length }} visible
          </span>

          <div class="flex items-center gap-1">
            <button
              class="h-8 min-w-0 px-2.5 text-xs"
              app-flat-button
              color="ghost"
              type="button"
              [disabled]="!canShowAll()"
              (click)="showAll()">
              Show all
            </button>
            <button
              class="h-8 min-w-0 px-2.5 text-xs"
              app-flat-button
              color="ghost"
              type="button"
              [disabled]="!canHideAll()"
              (click)="hideAll()">
              Hide all
            </button>
            <button
              class="h-8 min-w-0 px-2.5 text-xs"
              app-flat-button
              color="ghost"
              type="button"
              [disabled]="!canHideEmpty()"
              (click)="hideEmpty()">
              Hide empty
            </button>
          </div>
        </div>

        <div class="-mx-2 flex max-h-[50vh] flex-col overflow-y-auto py-1">
          @for (group of groups(); track group.id) {
            <div
              class="hover:bg-foreground/5 flex items-center gap-3 rounded px-2 py-2.5 transition-colors">
              <app-checkbox
                class="min-w-0 flex-1"
                [checked]="isVisible(group.id)"
                (changed)="onToggle(group.id, $event)">
                <span
                  [class]="
                    isVisible(group.id)
                      ? 'truncate'
                      : 'text-foreground/40 truncate'
                  ">
                  {{ group.name }}
                </span>
              </app-checkbox>

              <span
                [class]="
                  group.tasks.length
                    ? 'bg-foreground/8 text-foreground/60 shrink-0 rounded-full px-2 py-0.5 text-[11px] font-medium tabular-nums'
                    : 'text-foreground/40 border-border shrink-0 rounded-full border border-dashed px-2 py-0.5 text-[11px] font-medium tabular-nums'
                "
                [attr.aria-label]="taskCountLabel(group.tasks.length)"
                [title]="taskCountLabel(group.tasks.length)">
                {{ group.tasks.length }}
              </span>
            </div>
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

  private emptyGroupIds = computed(
    () =>
      new Set(
        this.groups()
          .filter((group) => !group.tasks.length)
          .map((group) => group.id)
      )
  );

  protected visibleCount = computed(
    () => this.groups().filter((group) => this.isVisible(group.id)).length
  );

  protected canShowAll = computed(() => this.hiddenIds().size > 0);

  protected canHideAll = computed(() =>
    this.groups().some((group) => this.isVisible(group.id))
  );

  protected canHideEmpty = computed(() =>
    this.groups().some(
      (group) => !group.tasks.length && this.isVisible(group.id)
    )
  );

  isVisible(groupId: number): boolean {
    return !this.hiddenIds().has(groupId);
  }

  taskCountLabel(count: number): string {
    return count === 1 ? '1 task' : `${count} tasks`;
  }

  onToggle(groupId: number, visible: boolean) {
    const hidden = new Set(this.hiddenIds());

    if (visible) {
      hidden.delete(groupId);
    } else {
      hidden.add(groupId);
    }

    this.setHidden(hidden);
  }

  showAll() {
    this.setHidden(new Set());
  }

  hideAll() {
    this.setHidden(new Set(this.groups().map((group) => group.id)));
  }

  hideEmpty() {
    this.setHidden(new Set([...this.hiddenIds(), ...this.emptyGroupIds()]));
  }

  private setHidden(hidden: ReadonlySet<number>) {
    const boardId = this.board()?.id;

    if (boardId === undefined) return;

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

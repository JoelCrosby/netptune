import { Dialog } from '@angular/cdk/dialog';
import { Component, computed, effect, inject, untracked } from '@angular/core';
import { SessionService } from '@core/services/session.service';
import { PERMISSIONS } from '@app/core/auth/permissions';
import { BoardGroupCommandsService } from '@core/services/board-group-commands.service';
import { BoardSelectionService } from '@core/services/board-selection.service';
import { DialogService } from '@core/services/dialog.service';
import {
  LucideCombine,
  LucideDynamicIcon,
  LucideIconInput,
  LucideTrash2,
  LucideUsers,
  LucideX,
} from '@lucide/angular';
import { ToolbarButtonComponent } from '@static/components/button/toolbar-button.component';
import { KeyboardService } from '@static/services/keyboard.service';
import { MoveTasksDialogComponent } from '../move-tasks-dialog/move-tasks-dialog.component';
import { ReassignTasksDialogComponent } from '../reassign-tasks-dialog/reassign-tasks-dialog.component';

interface SelectionAction {
  label: string;
  icon: LucideIconInput;
  destructive?: boolean;
  action: () => void;
}

@Component({
  selector: 'app-board-groups-selection',
  imports: [LucideDynamicIcon, LucideX, ToolbarButtonComponent],
  styles: [
    `
      /* Animates the individual translate/scale properties rather than
         transform: the -translate-x-1/2 utility sets translate, and a
         transform would compose with it instead of replacing it. */
      @keyframes selection-bar-in {
        from {
          opacity: 0;
          translate: -50% 12px;
          scale: 0.98;
        }
        to {
          opacity: 1;
          translate: -50% 0;
          scale: 1;
        }
      }

      .selection-bar {
        animation: selection-bar-in 160ms ease-out;
      }

      @media (prefers-reduced-motion: reduce) {
        .selection-bar {
          animation: none;
        }
      }
    `,
  ],
  template: `
    @if (count(); as count) {
      <div
        class="selection-bar border-border bg-dialog-background fixed bottom-6 left-1/2 z-40 flex max-w-[calc(100vw-2rem)] -translate-x-1/2 flex-wrap items-center gap-1 rounded-xl border p-1.5 shadow-lg"
        role="region"
        i18n-aria-label="
          Accessible label for the bar holding actions for the selected tasks
        "
        aria-label="Selected task actions">
        <div class="flex items-center gap-2 pr-2 pl-2">
          <span
            class="bg-primary text-primary-foreground flex h-6 min-w-6 items-center justify-center rounded-full px-1.5 text-xs font-semibold"
            aria-hidden="true">
            {{ count }}
          </span>
          <span class="text-foreground text-sm whitespace-nowrap">
            <ng-container i18n="Label for the number of selected tasks">
              {count, plural, =1 {task selected} other {tasks selected}}
            </ng-container>
          </span>
        </div>

        @if (actions().length) {
          <span class="bg-border mx-1 h-6 w-px" aria-hidden="true"></span>

          @for (action of actions(); track action.label) {
            <button
              app-toolbar-button
              [color]="action.destructive ? 'warn' : 'neutral'"
              (click)="action.action()">
              <svg [lucideIcon]="action.icon" class="h-4 w-4"></svg>
              <span>{{ action.label }}</span>
            </button>
          }
        }

        <span class="bg-border mx-1 h-6 w-px" aria-hidden="true"></span>

        <button app-toolbar-button (click)="onClearClicked()">
          <svg lucideX class="h-4 w-4"></svg>
          <span i18n="Button that clears the task selection">Clear</span>
          <kbd
            class="border-border text-foreground/50 rounded border px-1.5 py-0.5 text-[10px] font-medium">
            {{ escapeKeyLabel }}
          </kbd>
        </button>
      </div>
    }
  `,
})
export class BoardGroupsSelectionComponent {
  private dialog = inject(DialogService);
  private selection = inject(BoardSelectionService);
  private boardCommands = inject(BoardGroupCommandsService);
  private keyboard = inject(KeyboardService);
  private cdkDialog = inject(Dialog);

  readonly escapeKeyLabel = $localize`:Keyboard key that clears the task selection, shown as a hint:Esc`;

  selected = this.selection.taskIds;
  count = this.selection.count;
  permissions = inject(SessionService).permissions;

  actions = computed<SelectionAction[]>(() => {
    const actions: SelectionAction[] = [];
    const permissions = this.permissions();

    if (permissions.has(PERMISSIONS.tasks.move)) {
      actions.push({
        label: $localize`:Action that moves the selected tasks to another board group:Move to group`,
        action: this.onMoveTasksClicked.bind(this),
        icon: LucideCombine,
      });
    }
    if (permissions.has(PERMISSIONS.tasks.reassign)) {
      actions.push({
        label: $localize`:Action that reassigns the selected tasks to another person:Reassign`,
        action: this.onReassignTasksClicked.bind(this),
        icon: LucideUsers,
      });
    }
    if (permissions.has(PERMISSIONS.tasks.delete)) {
      actions.push({
        label: $localize`:Action that deletes the selected tasks:Delete tasks`,
        action: this.onDeleteClicked.bind(this),
        icon: LucideTrash2,
        destructive: true,
      });
    }

    return actions;
  });

  constructor() {
    effect(() => {
      const event = this.keyboard.keyDown();

      if (event?.key !== 'Escape' || !this.escapeClearsSelection(event)) {
        return;
      }

      untracked(() => {
        if (this.count()) {
          this.selection.clear();
        }
      });
    });
  }

  // Escape belongs to whatever is on top: an open dialog closes, a field being
  // edited reverts, and only a bare board clears the selection.
  private escapeClearsSelection(event: KeyboardEvent) {
    if (this.cdkDialog.openDialogs.length) {
      return false;
    }

    const target = event.target as HTMLElement | null;

    if (!target) {
      return true;
    }

    const tag = target.tagName;

    return tag !== 'INPUT' && tag !== 'TEXTAREA' && !target.isContentEditable;
  }

  onClearClicked() {
    this.selection.clear();
  }

  onDeleteClicked() {
    this.boardCommands.deleteSelectedTasks();
  }

  onMoveTasksClicked() {
    this.dialog.open(MoveTasksDialogComponent, {
      width: '600px',
      panelClass: 'app-modal-class',
    });
  }

  onReassignTasksClicked() {
    this.dialog.open(ReassignTasksDialogComponent, {
      width: '400px',
      panelClass: 'app-modal-class',
    });
  }
}

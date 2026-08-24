import { Component, computed, input, output } from '@angular/core';
import { hasPermission } from '@core/auth/has-permission';
import { PERMISSIONS } from '@core/auth/permissions';
import { TaskPin, TaskPinScope } from '@core/models/task-pin';
import { pinScopeIcons } from '@core/util/pin-scope';
import {
  LucideCheck,
  LucideDynamicIcon,
  LucideIconInput,
  LucideLock,
  LucidePinOff,
} from '@lucide/angular';

export interface PinScopeTarget {
  boardId?: number | null;
  boardName?: string | null;
  projectId?: number | null;
  projectName?: string | null;
}

interface PinScopeRow {
  scope: TaskPinScope;
  icon: LucideIconInput;
  label: string;
  qualifier: string | null;
  pinned: boolean;
  allowed: boolean;
}

@Component({
  selector: 'app-pin-scope-menu',
  imports: [LucideCheck, LucideDynamicIcon, LucideLock, LucidePinOff],
  host: { class: 'block' },
  template: `
    <p
      class="text-foreground/40 px-3 pt-2 pb-1.5 text-[11px] font-semibold tracking-[0.6px] uppercase"
      i18n="Heading above the list of scopes a task can be pinned to">
      Pin to
    </p>

    @for (row of rows(); track row.scope) {
      <button
        type="button"
        role="menuitemcheckbox"
        class="hover:bg-foreground/5 flex w-full items-center gap-3 rounded-sm px-3 py-2 text-left text-sm transition-colors disabled:pointer-events-none disabled:opacity-50"
        [attr.aria-checked]="row.pinned"
        [disabled]="!row.allowed"
        (click)="toggled.emit(row.scope)">
        @if (row.pinned) {
          <span
            class="bg-primary text-primary-foreground flex h-4 w-4 flex-none items-center justify-center rounded-[4px]">
            <svg lucideCheck class="h-3 w-3"></svg>
          </span>
        } @else {
          <span class="border-border h-4 w-4 flex-none rounded-[4px] border">
          </span>
        }

        <svg [lucideIcon]="row.icon" class="text-muted h-4 w-4 flex-none"></svg>

        <span class="flex-1 truncate">
          {{ row.label }}
          @if (row.qualifier) {
            <span class="text-muted">{{ row.qualifier }}</span>
          }
        </span>

        @if (!row.allowed) {
          <svg
            lucideLock
            class="text-muted h-[13px] w-[13px] flex-none"
            [attr.aria-label]="lockedLabel"
            [title]="lockedLabel"></svg>
        }
      </button>
    }

    @if (anyPinned()) {
      <span class="bg-border mx-2 my-1 block h-px" aria-hidden="true"></span>

      <button
        type="button"
        role="menuitem"
        class="text-warn hover:bg-foreground/5 flex w-full items-center gap-3 rounded-sm px-3 py-2 text-left text-sm transition-colors"
        (click)="unpinAll.emit()">
        <svg lucidePinOff class="h-4 w-4 flex-none"></svg>
        <span
          class="flex-1"
          i18n="Menu action that removes every pin the caller may remove">
          Unpin everywhere
        </span>
      </button>
    }
  `,
})
export class PinScopeMenuComponent {
  readonly pins = input.required<TaskPin[]>();
  readonly target = input.required<PinScopeTarget>();

  readonly toggled = output<TaskPinScope>();
  readonly unpinAll = output();

  protected readonly lockedLabel = $localize`:Tooltip on a pin scope the caller is not allowed to set:You do not have permission to pin here`;

  private readonly canPinBoard = hasPermission(PERMISSIONS.boards.update);
  private readonly canPinProject = hasPermission(PERMISSIONS.projects.update);
  private readonly canPinWorkspace = hasPermission(
    PERMISSIONS.tasks.pinWorkspace
  );

  protected readonly anyPinned = computed(() => {
    const removable = this.pins().some((pin) => pin.canUnpin);

    return removable;
  });

  protected readonly rows = computed<PinScopeRow[]>(() => {
    const target = this.target();
    const rows: PinScopeRow[] = [
      {
        scope: TaskPinScope.user,
        icon: pinScopeIcons[TaskPinScope.user],
        label: $localize`:Pin scope that keeps a task in view for you alone:Just for me`,
        qualifier: null,
        pinned: this.isPinned(TaskPinScope.user),
        allowed: true,
      },
    ];

    if (target.boardId) {
      rows.push({
        scope: TaskPinScope.board,
        icon: pinScopeIcons[TaskPinScope.board],
        label: target.boardName ?? '',
        qualifier: $localize`:Qualifier after a board name in the pin scope menu: board`,
        pinned: this.isPinned(TaskPinScope.board),
        allowed: this.canPinBoard(),
      });
    }

    if (target.projectId) {
      rows.push({
        scope: TaskPinScope.project,
        icon: pinScopeIcons[TaskPinScope.project],
        label: target.projectName ?? '',
        qualifier: $localize`:Qualifier after a project name in the pin scope menu: project`,
        pinned: this.isPinned(TaskPinScope.project),
        allowed: this.canPinProject(),
      });
    }

    rows.push({
      scope: TaskPinScope.workspace,
      icon: pinScopeIcons[TaskPinScope.workspace],
      label: $localize`:Pin scope that keeps a task in view for everyone in the workspace:Workspace`,
      qualifier: null,
      pinned: this.isPinned(TaskPinScope.workspace),
      allowed: this.canPinWorkspace(),
    });

    return rows;
  });

  private isPinned(scope: TaskPinScope): boolean {
    return this.pins().some((pin) => pin.scope === scope);
  }
}

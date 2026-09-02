import { DialogRef } from '@angular/cdk/dialog';
import {
  Component,
  LOCALE_ID,
  afterNextRender,
  computed,
  inject,
  signal,
} from '@angular/core';
import { BoardGroupCommandsService } from '@core/services/board-group-commands.service';
import { BoardSelectionService } from '@core/services/board-selection.service';
import { BoardViewService } from '@core/services/board-view.service';
import { LucideX } from '@lucide/angular';
import { AvatarComponent } from '@static/components/avatar/avatar.component';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import {
  FilterOption,
  FilterOptionListComponent,
} from '@static/components/filter-option-list/filter-option-list.component';
import { DialogTitleComponent } from '@static/components/dialog-title/dialog-title.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { DialogCloseDirective } from '@static/directives/dialog-close.directive';

interface MemberOption extends FilterOption<string> {
  pictureUrl?: string | null;
  isServiceAccount?: boolean;
}

// Stands for the Unassign row. Member ids are GUIDs, so an empty one cannot collide with a person.
const unassignValue = '';

@Component({
  selector: 'app-reassign-tasks-dialog',
  imports: [
    AvatarComponent,
    DialogActionsDirective,
    DialogCloseDirective,
    DialogTitleComponent,
    FilterOptionListComponent,
    FlatButtonComponent,
    LucideX,
    StrokedButtonComponent,
  ],
  host: { class: 'block' },
  template: `
    <ng-template #leading let-option>
      @if (option.value === unassign) {
        <span
          class="border-foreground/30 text-muted flex h-6 w-6 shrink-0 items-center justify-center rounded-full border border-dashed">
          <svg lucideX class="h-3 w-3" aria-hidden="true"></svg>
        </span>
      } @else {
        <app-avatar
          class="shrink-0"
          size="sm"
          [tooltip]="false"
          [name]="option.label"
          [imageUrl]="option.pictureUrl"
          [isServiceAccount]="option.isServiceAccount ?? false" />
      }
    </ng-template>

    <app-dialog-title
      noMargin
      class="mb-1"
      i18n="Title of the dialog for reassigning tasks to another person">
      Re-assign tasks
    </app-dialog-title>

    <p class="text-muted mb-1 text-sm">
      <ng-container
        i18n="Explains how many tasks a reassignment replaces the assignees on">
        {taskCount(), plural,
          =1 {Replaces the assignees on 1 selected task.}
          other {Replaces the assignees on {{ taskCount() }} selected tasks.}
        }
      </ng-container>
    </p>

    <p class="text-muted mb-4 text-xs font-medium">{{ todayHint() }}</p>

    <div
      class="border-border bg-form-field-background overflow-hidden rounded-sm border-2">
      <app-filter-option-list
        class="w-full!"
        [listMaxHeightClass]="listMaxHeightClass"
        [open]="opened()"
        [highlightSelected]="true"
        [dismissKeyHint]="false"
        [options]="options()"
        [selected]="selectedValues()"
        [optionLeading]="leading"
        [searchPlaceholder]="labels.search"
        [listAriaLabel]="labels.members"
        [emptyMessage]="labels.noMembers"
        (toggled)="toggle($event)" />
    </div>

    <div app-dialog-actions align="end">
      <button app-stroked-button app-dialog-close type="button">
        <span i18n="Dismisses a dialog without acting">Cancel</span>
      </button>
      <button
        app-flat-button
        type="button"
        [disabled]="!picked().length"
        (click)="reassign()">
        @if (unassigning()) {
          <ng-container
            i18n="Button that clears the assignees on the selected tasks">
            {taskCount(), plural,
              =1 {Unassign 1 task}
              other {Unassign {{ taskCount() }} tasks}
            }
          </ng-container>
        } @else {
          <ng-container
            i18n="Button that reassigns the selected tasks to the people picked">
            {taskCount(), plural,
              =1 {Re-assign 1 task}
              other {Re-assign {{ taskCount() }} tasks}
            }
          </ng-container>
        }
      </button>
    </div>
  `,
})
export class ReassignTasksDialogComponent {
  static readonly width = '480px';

  private readonly boardCommands = inject(BoardGroupCommandsService);
  private readonly selection = inject(BoardSelectionService);
  private readonly boardView = inject(BoardViewService);
  private readonly locale = inject(LOCALE_ID);

  readonly dialogRef =
    inject<DialogRef<ReassignTasksDialogComponent>>(DialogRef);

  protected readonly unassign = unassignValue;
  protected readonly listMaxHeightClass = 'max-h-54';

  protected readonly labels = {
    search: $localize`:Placeholder in the box that searches workspace members:Search members`,
    members: $localize`:Accessible name of the list of people a task can be assigned to:Members`,
    noMembers: $localize`:Shown when a board has no members to assign tasks to:No members`,
  };

  protected readonly picked = signal<string[]>([]);
  protected readonly opened = signal(false);

  private readonly tasks = this.selection.selectedTasks;

  protected readonly taskCount = computed(() => this.tasks().length);

  private readonly assignedCounts = computed(() => {
    const counts = new Map<string, number>();

    for (const task of this.tasks()) {
      for (const assignee of task.assignees) {
        counts.set(assignee.id, (counts.get(assignee.id) ?? 0) + 1);
      }
    }

    return counts;
  });

  protected readonly options = computed<MemberOption[]>(() => {
    const counts = this.assignedCounts();
    const members = this.boardView.users().map((user) => {
      return {
        value: user.id,
        label: user.displayName,
        hint: this.memberHint(counts.get(user.id) ?? 0, user.isServiceAccount),
        pictureUrl: user.pictureUrl,
        isServiceAccount: user.isServiceAccount,
      };
    });

    return [this.unassignOption(), ...members];
  });

  protected readonly selectedValues = computed(() => new Set(this.picked()));

  protected readonly unassigning = computed(() => {
    return this.picked().includes(unassignValue);
  });

  protected readonly todayHint = computed(() => {
    const counts = this.assignedCounts();
    const names = new Map(
      this.boardView.users().map((user) => [user.id, user.displayName])
    );
    const fragments = [...counts]
      .sort(([, left], [, right]) => right - left)
      .map(([id, count]) => this.assignedFragment(names.get(id), count));
    const unassigned = this.tasks().filter(
      (task) => task.assignees.length === 0
    ).length;

    if (unassigned > 0) {
      fragments.push(this.unassignedFragment(unassigned));
    }

    return this.today(
      new Intl.ListFormat(this.locale, {
        style: 'narrow',
        type: 'unit',
      }).format(fragments)
    );
  });

  constructor() {
    afterNextRender(() => this.opened.set(true));
  }

  // Unassign and a set of people are two ways of saying the same thing, so picking either
  // clears the other rather than leaving a contradiction on screen.
  protected toggle(value: string) {
    if (value === unassignValue) {
      this.picked.update((picked) => {
        return picked.includes(unassignValue) ? [] : [unassignValue];
      });

      return;
    }

    this.picked.update((picked) => {
      const people = picked.filter((id) => id !== unassignValue);

      return people.includes(value)
        ? people.filter((id) => id !== value)
        : [...people, value];
    });
  }

  protected reassign() {
    const picked = this.picked();

    if (!picked.length) return;

    this.boardCommands.reassignSelectedTasks(this.unassigning() ? [] : picked);
    this.dialogRef.close();
  }

  private unassignOption(): MemberOption {
    const count = this.taskCount();

    return {
      value: unassignValue,
      label: $localize`:Option that clears the assignees on the selected tasks:Unassign`,
      hint: $localize`:Says how many tasks the Unassign option clears. COUNT is how many are selected:clears all ${count}:COUNT:`,
      sticky: true,
    };
  }

  private memberHint(count: number, isServiceAccount?: boolean): string {
    if (count > 0) {
      return $localize`:Says how many of the selected tasks a person already holds. COUNT is how many they hold and TOTAL is how many are selected:on ${count}:COUNT: of ${this.taskCount()}:TOTAL:`;
    }

    if (isServiceAccount) {
      return $localize`:Marks a member that is a service account rather than a person:service account`;
    }

    return '';
  }

  private assignedFragment(name: string | undefined, count: number): string {
    const displayName =
      name ??
      $localize`:Stands in for a person the current user cannot see:Someone else`;

    return $localize`:One entry in a breakdown of who holds the selected tasks today, e.g. "Ada on 3". NAME is the person and COUNT is how many tasks they hold:${displayName}:NAME: on ${count}:COUNT:`;
  }

  private unassignedFragment(count: number): string {
    return $localize`:One entry in a breakdown of who holds the selected tasks today, counting those held by nobody. COUNT is how many:${count}:COUNT: unassigned`;
  }

  private today(detail: string): string {
    return $localize`:Prefixes a summary of who holds the selected tasks before they are reassigned. DETAIL is the breakdown:Today: ${detail}:DETAIL:`;
  }
}

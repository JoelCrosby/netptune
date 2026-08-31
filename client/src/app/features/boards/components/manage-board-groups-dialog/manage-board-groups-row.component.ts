import { CdkDragHandle } from '@angular/cdk/drag-drop';
import { Component, computed, input, output, viewChild } from '@angular/core';
import {
  LucideEllipsis,
  LucideEye,
  LucideEyeOff,
  LucideGripVertical,
} from '@lucide/angular';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
import { InlineEditInputComponent } from '@static/components/inline-edit-input/inline-edit-input.component';

export interface ManageBoardGroupRow {
  id: number;
  name: string;
  taskCount: number;
  hidden: boolean;
}

@Component({
  selector: 'app-manage-board-groups-row',
  host: { class: 'block' },
  imports: [
    CdkDragHandle,
    IconButtonComponent,
    InlineEditInputComponent,
    LucideEllipsis,
    LucideEye,
    LucideEyeOff,
    LucideGripVertical,
  ],
  template: `
    <div [class]="rowClass()">
      <span
        cdkDragHandle
        aria-hidden="true"
        [class]="gripClass()"
        [cdkDragHandleDisabled]="reorderDisabled()">
        <svg lucideGripVertical class="h-4 w-4"></svg>
      </span>

      <span [class]="nameClass()">
        <app-inline-edit-input
          class="min-w-0 text-sm font-medium"
          activeBorder="true"
          [value]="row().name"
          [size]="row().name.length"
          [disabled]="!canEdit()"
          (submitted)="onNameSubmitted($event)"></app-inline-edit-input>

        @if (stateLabel(); as label) {
          <span
            class="text-foreground/40 shrink-0 text-[11px] whitespace-nowrap">
            {{ label }}
          </span>
        }
      </span>

      <span [class]="countClass()" [title]="taskCountLabel()">
        {{ row().taskCount }}
      </span>

      <button
        app-icon-button
        type="button"
        [class]="visibilityButtonClass()"
        [ariaLabel]="visibilityLabel()"
        [attr.aria-pressed]="row().hidden"
        (click)="hiddenChanged.emit(!row().hidden)">
        @if (row().hidden) {
          <svg lucideEyeOff class="h-4 w-4"></svg>
        } @else {
          <svg lucideEye class="h-4 w-4"></svg>
        }
      </button>

      <button
        app-icon-button
        type="button"
        [class]="iconButtonClass()"
        [ariaLabel]="menuLabel()"
        (click)="onMenuClicked($event)">
        <svg lucideEllipsis class="text-foreground/40 h-4 w-4"></svg>
      </button>
    </div>
  `,
})
export class ManageBoardGroupsRowComponent {
  readonly row = input.required<ManageBoardGroupRow>();
  readonly dense = input(false);
  readonly canEdit = input(false);
  readonly reorderDisabled = input(false);

  readonly renamed = output<string>();
  readonly hiddenChanged = output<boolean>();
  readonly menuRequested = output<HTMLElement>();

  private readonly editor = viewChild.required(InlineEditInputComponent);

  protected readonly rowClass = computed(() => {
    const height = this.dense() ? 'min-h-10' : 'min-h-12';
    const surface = this.row().hidden ? 'bg-foreground/2' : '';

    return `hover:bg-hover flex shrink-0 items-center gap-2 rounded-md px-2 transition-colors ${height} ${surface}`;
  });

  protected readonly gripClass = computed(() => {
    const affordance = this.reorderDisabled()
      ? 'cursor-default opacity-40'
      : 'cursor-grab';

    return `text-foreground/28 inline-flex w-4 shrink-0 justify-center ${affordance}`;
  });

  protected readonly nameClass = computed(() => {
    const tone = this.row().hidden ? 'text-foreground/40' : 'text-foreground';

    return `flex min-w-0 flex-1 items-center gap-2 ${tone}`;
  });

  protected readonly countClass = computed(() => {
    const { hidden, taskCount } = this.row();

    const shape = taskCount
      ? 'bg-foreground/8'
      : 'border-border border border-dashed';
    const tone = hidden || !taskCount ? 'text-foreground/40' : 'text-muted';
    const padding = this.dense() ? 'py-px' : 'py-0.5';

    return `inline-flex min-w-5.5 shrink-0 items-center justify-center rounded-full px-2 text-[11px] font-medium tabular-nums ${shape} ${tone} ${padding}`;
  });

  protected readonly iconButtonClass = computed(() => {
    return this.dense() ? 'h-7 w-7 rounded-md' : 'h-8 w-8 rounded-md';
  });

  protected readonly visibilityButtonClass = computed(() => {
    const emphasis = this.row().hidden
      ? 'bg-foreground/8 text-foreground'
      : 'text-muted';

    return `${this.iconButtonClass()} ${emphasis}`;
  });

  protected readonly stateLabel = computed(() => {
    const { hidden, taskCount } = this.row();

    if (hidden && !taskCount) {
      return $localize`:Marks a board group that is both hidden and empty:Hidden · no tasks`;
    }

    if (hidden) {
      return $localize`:Marks a board group hidden from the current user's board:Hidden`;
    }

    if (!taskCount) {
      return $localize`:Marks a board group that contains no tasks:No tasks`;
    }

    return null;
  });

  protected readonly taskCountLabel = computed(() => {
    const count = this.row().taskCount;

    return count === 1
      ? $localize`:How many tasks a board group holds:1 task`
      : $localize`:How many tasks a board group holds. COUNT is the number of tasks:${count}:COUNT: tasks`;
  });

  protected readonly visibilityLabel = computed(() => {
    const name = this.row().name;

    return this.row().hidden
      ? $localize`:Accessible label for the button that shows a board group. NAME is the group name:Show ${name}:NAME:`
      : $localize`:Accessible label for the button that hides a board group. NAME is the group name:Hide ${name}:NAME:`;
  });

  protected readonly menuLabel = computed(() => {
    const name = this.row().name;

    return $localize`:Accessible label for a board group's overflow menu. NAME is the group name:More actions for ${name}:NAME:`;
  });

  focusRename() {
    const element = this.editor().editableRef()?.nativeElement as
      HTMLElement | undefined;

    element?.focus();
  }

  protected onNameSubmitted(value: string) {
    const name = value.trim();
    const unchanged = name === this.row().name;

    if (!name || unchanged) {
      this.editor().value.set(this.row().name);

      return;
    }

    this.renamed.emit(name);
  }

  protected onMenuClicked(event: MouseEvent) {
    this.menuRequested.emit(event.currentTarget as HTMLElement);
  }
}

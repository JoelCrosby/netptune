import { Component, computed, inject, linkedSignal } from '@angular/core';
import { PinnedTask, TaskPin } from '@core/models/task-pin';
import { boardPinsResource } from '@core/resources/task-pin.resource';
import { BoardSelectionService } from '@core/services/board-selection.service';
import { BoardViewService } from '@core/services/board-view.service';
import { DialogService } from '@core/services/dialog.service';
import { PinCommandsService } from '@core/services/pin-commands.service';
import { pinScopeIcons, pinScopeTooltip } from '@core/util/pin-scope';
import { TaskDetailDialogComponent } from '@entry/dialogs/task-detail-dialog/task-detail-dialog.component';
import {
  LucideChevronDown,
  LucideChevronRight,
  LucideIconInput,
  LucidePin,
} from '@lucide/angular';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { ToolbarButtonComponent } from '@static/components/button/toolbar-button.component';
import { TaskChipComponent } from '@static/components/task-chip.component';

const visibleChips = 3;

interface BannerPin {
  pinned: PinnedTask;
  icon: LucideIconInput;
  scopeLabel: string;
  removable: TaskPin | null;
}

@Component({
  selector: 'app-board-pinned-banner',
  imports: [
    LucideChevronDown,
    LucideChevronRight,
    LucidePin,
    TaskChipComponent,
    StrokedButtonComponent,
    ToolbarButtonComponent,
  ],
  styles: [
    `
      @keyframes pinned-banner-in {
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

      .pinned-banner {
        animation: pinned-banner-in 160ms ease-out;
      }

      @media (prefers-reduced-motion: reduce) {
        .pinned-banner {
          animation: none;
        }
      }
    `,
  ],
  template: `
    @if (count(); as count) {
      @if (selectionActive()) {
        <div
          class="border-border bg-dialog-background absolute right-6 bottom-6 z-30 flex items-center gap-1.5 rounded-xl border px-2.5 py-1.5 opacity-75"
          [title]="collapsedLabel()">
          <svg lucidePin class="text-primary h-3.5 w-3.5 fill-current"></svg>
          <span class="text-muted text-xs tabular-nums">{{ count }}</span>
        </div>
      } @else if (expanded()) {
        <div
          class="pinned-banner border-border bg-dialog-background absolute bottom-6 left-1/2 z-40 flex max-w-[calc(100%-2rem)] -translate-x-1/2 items-center gap-1 rounded-xl border p-1.5 shadow-lg"
          role="region"
          i18n-aria-label="Accessible label for the board's pinned task bar"
          aria-label="Pinned tasks">
          <div class="flex items-center gap-2 px-2">
            <svg lucidePin class="text-primary h-4 w-4 fill-current"></svg>
            <span
              class="text-foreground text-sm"
              i18n="Label on the board's pinned task bar">
              Pinned
            </span>
            <span
              class="bg-primary text-primary-foreground flex h-6 min-w-6 items-center justify-center rounded-full px-1.5 text-xs font-semibold">
              {{ count }}
            </span>
          </div>

          <span class="bg-border mx-1 h-6 w-px" aria-hidden="true"></span>

          <div class="flex items-center gap-1.5">
            @for (chip of chips(); track chip.pinned.task.id) {
              <app-task-chip
                [systemId]="chip.pinned.task.systemId"
                [name]="chip.pinned.task.name"
                [icon]="chip.icon"
                [iconLabel]="chip.scopeLabel"
                [removable]="!!chip.removable"
                [removeLabel]="unpinLabel"
                (opened)="onChipOpened(chip)"
                (removed)="onUnpinClicked(chip.removable)" />
            }

            @if (overflowCount(); as overflow) {
              <button
                app-stroked-button
                color="neutral"
                class="text-muted hover:text-foreground h-8.5 min-w-0 rounded-lg border-dashed px-3 text-[13px]"
                [title]="overflowLabel()"
                (click)="onCollapseClicked()">
                +{{ overflow }}
              </button>
            }
          </div>

          <span class="bg-border mx-1 h-6 w-px" aria-hidden="true"></span>

          <button
            app-toolbar-button
            class="h-8 w-8 justify-center px-0"
            [title]="collapseLabel"
            [attr.aria-label]="collapseLabel"
            (click)="onCollapseClicked()">
            <svg lucideChevronDown class="h-3.5 w-3.5"></svg>
          </button>
        </div>
      } @else {
        <button
          app-stroked-button
          color="neutral"
          class="pinned-banner bg-dialog-background absolute bottom-6 left-1/2 z-40 h-auto min-w-0 -translate-x-1/2 rounded-xl px-3 py-1.75 text-[13px] shadow-lg"
          (click)="onExpandClicked()">
          <svg lucidePin class="text-primary h-3.75 w-3.75 fill-current"></svg>
          <span class="text-foreground">{{ collapsedLabel() }}</span>
          <svg
            lucideChevronRight
            class="text-foreground/50 h-3.25 w-3.25"></svg>
        </button>
      }
    }
  `,
})
export class BoardPinnedBannerComponent {
  private readonly boardView = inject(BoardViewService);
  private readonly selection = inject(BoardSelectionService);
  private readonly pinCommands = inject(PinCommandsService);
  private readonly dialog = inject(DialogService);

  private readonly boardId = computed(() => this.boardView.board()?.id);
  private readonly pinsRef = boardPinsResource(this.boardId);

  protected readonly collapseLabel = $localize`:Tooltip on the control that collapses the pinned task bar:Collapse`;
  protected readonly unpinLabel = $localize`:Tooltip on the control that removes a pin:Unpin`;

  protected readonly pinnedTasks = computed(() => this.pinsRef.value() ?? []);
  protected readonly count = computed(() => this.pinnedTasks().length);
  protected readonly selectionActive = computed(
    () => this.selection.count() > 0
  );

  protected readonly expanded = linkedSignal<number, boolean>({
    source: this.count,
    computation: (count) => count > 0 && count <= visibleChips,
  });

  protected readonly chips = computed<BannerPin[]>(() => {
    return this.pinnedTasks()
      .slice(0, visibleChips)
      .map((pinned) => this.toBannerPin(pinned));
  });

  protected readonly overflowCount = computed(() =>
    Math.max(0, this.count() - visibleChips)
  );

  protected readonly collapsedLabel = computed(() => {
    const count = this.count();

    return $localize`:Label on the collapsed pinned task bar. COUNT is the number of pinned tasks:${count}:COUNT: pinned`;
  });

  protected readonly overflowLabel = computed(() => {
    const count = this.overflowCount();

    return $localize`:Tooltip on the pill standing in for the pinned tasks the bar could not fit:${count}:COUNT: more pinned`;
  });

  protected onExpandClicked() {
    this.expanded.set(true);
  }

  protected onCollapseClicked() {
    this.expanded.set(false);
  }

  protected onChipOpened(chip: BannerPin) {
    this.dialog.open(TaskDetailDialogComponent, {
      width: TaskDetailDialogComponent.width,
      data: { systemId: chip.pinned.task.systemId },
      panelClass: 'app-modal-class',
      autoFocus: false,
    });
  }

  protected onUnpinClicked(pin: TaskPin | null) {
    if (!pin) return;

    this.pinCommands.unpin(pin);
  }

  private toBannerPin(pinned: PinnedTask): BannerPin {
    const primary = pinned.pins[0];

    return {
      pinned,
      icon: pinScopeIcons[primary.scope],
      scopeLabel: pinScopeTooltip(primary.scope, primary.scopeName),
      removable: pinned.pins.find((pin) => pin.canUnpin) ?? null,
    };
  }
}

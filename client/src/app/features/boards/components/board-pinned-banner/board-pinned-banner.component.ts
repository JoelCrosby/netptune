import { Component, computed, inject, signal } from '@angular/core';
import { PinnedTask, TaskPin } from '@core/models/task-pin';
import { boardPinsResource } from '@core/resources/task-pin.resource';
import { BoardSelectionService } from '@core/services/board-selection.service';
import { BoardViewService } from '@core/services/board-view.service';
import { DialogService } from '@core/services/dialog.service';
import { PinCommandsService } from '@core/services/pin-commands.service';
import { pinnedTaskFilter } from '@core/util/pinned-task-filter';
import { TaskDetailDialogComponent } from '@entry/dialogs/task-detail-dialog/task-detail-dialog.component';
import { LucideChevronUp, LucidePin } from '@lucide/angular';
import { PinnedTaskRowComponent } from '@shared/components/pinned-task-row/pinned-task-row.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { TabGroupComponent } from '@static/components/tab-group/tab-group.component';

@Component({
  selector: 'app-board-pinned-banner',
  imports: [
    LucideChevronUp,
    LucidePin,
    PinnedTaskRowComponent,
    StrokedButtonComponent,
    TabGroupComponent,
  ],
  host: { '(document:keydown.escape)': 'close()' },
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
          [title]="pillLabel()">
          <svg lucidePin class="text-primary h-3.5 w-3.5 fill-current"></svg>
          <span class="text-muted text-xs tabular-nums">{{ count }}</span>
        </div>
      } @else {
        @if (open()) {
          <button
            type="button"
            class="fixed inset-0 z-30 cursor-default"
            tabindex="-1"
            aria-hidden="true"
            (click)="close()"></button>

          <div
            id="board-pinned-list"
            class="pinned-banner border-border bg-dialog-background absolute bottom-19 left-1/2 z-40 flex max-h-[min(28rem,calc(100%-7rem))] w-190 max-w-[calc(100%-2rem)] -translate-x-1/2 flex-col overflow-hidden rounded-xl border shadow-xl"
            role="region"
            i18n-aria-label="Accessible label for the board's pinned task bar"
            aria-label="Pinned tasks">
            <div class="flex shrink-0 items-center gap-2 py-2.5 pr-2.5 pl-4">
              <svg lucidePin class="text-primary h-4 w-4 fill-current"></svg>
              <span
                class="text-sm font-semibold"
                i18n="Label on the board's pinned task bar">
                Pinned
              </span>
              <span class="text-muted text-sm tabular-nums">{{ count }}</span>

              <app-tab-group
                class="ml-auto"
                variant="island"
                [tabs]="filter.tabs()"
                [(value)]="filter.filter" />
            </div>

            <div class="custom-scroll min-h-0 overflow-y-auto">
              @for (pinned of filter.visible(); track pinned.task.id) {
                <app-pinned-task-row
                  class="border-border border-t"
                  layout="compact"
                  [showPageLink]="true"
                  [pinned]="pinned"
                  (opened)="onTaskOpened(pinned)"
                  (unpinned)="onUnpinClicked($event)" />
              } @empty {
                <p
                  class="border-border text-muted border-t px-4 py-6 text-center text-sm">
                  <span i18n="Shown when a pinned task filter matches nothing"
                    >Nothing pinned</span
                  >
                </p>
              }
            </div>
          </div>
        }

        <button
          app-stroked-button
          color="neutral"
          class="pinned-banner bg-dialog-background hover:bg-card-hover absolute bottom-6 left-1/2 z-40 h-10 min-w-0 -translate-x-1/2 rounded-xl px-3.5 text-[13px] shadow-lg"
          aria-controls="board-pinned-list"
          [class.border-primary]="open()"
          [attr.aria-expanded]="open()"
          (click)="toggle()">
          <svg lucidePin class="text-primary h-3.75 w-3.75 fill-current"></svg>
          <span class="text-foreground">{{ pillLabel() }}</span>
          <svg
            lucideChevronUp
            class="text-foreground/50 h-3.25 w-3.25 transition-transform"
            [class.rotate-180]="open()"></svg>
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

  protected readonly pinnedTasks = computed(() => this.pinsRef.value() ?? []);
  protected readonly count = computed(() => this.pinnedTasks().length);
  protected readonly selectionActive = computed(
    () => this.selection.count() > 0
  );

  protected readonly filter = pinnedTaskFilter(this.pinnedTasks);

  protected readonly open = signal(false);

  protected readonly pillLabel = computed(() => {
    const count = this.count();

    return $localize`:Label on the collapsed pinned task bar. COUNT is the number of pinned tasks:${count}:COUNT: pinned`;
  });

  protected toggle() {
    this.open.update((open) => !open);
  }

  protected close() {
    this.open.set(false);
  }

  protected onTaskOpened(pinned: PinnedTask) {
    this.close();

    this.dialog.open(TaskDetailDialogComponent, {
      width: TaskDetailDialogComponent.width,
      height: TaskDetailDialogComponent.height,
      data: { systemId: pinned.task.systemId },
      panelClass: TaskDetailDialogComponent.panelClass,
      autoFocus: false,
    });
  }

  protected onUnpinClicked(pin: TaskPin) {
    this.pinCommands.unpin(pin);
  }
}

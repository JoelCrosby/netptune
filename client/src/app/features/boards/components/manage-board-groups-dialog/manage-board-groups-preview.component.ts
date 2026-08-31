import { Component, input } from '@angular/core';
import { ManageBoardGroupRow } from './manage-board-groups-row.component';

const PREVIEW_CARD_LIMIT = 2;

@Component({
  selector: 'app-manage-board-groups-preview',
  host: { class: 'block' },
  template: `
    @if (dense()) {
      <span
        class="border-foreground/8 bg-foreground/2 flex h-12.5 items-end gap-px rounded-lg border px-2.5 py-2"
        aria-hidden="true">
        @for (row of rows(); track row.id) {
          <span
            class="flex h-full flex-1 flex-col justify-end gap-0.75"
            [class.opacity-45]="row.hidden">
            <span class="bg-foreground/14 h-1 shrink-0 rounded-[2px]"></span>
            @if (row.hidden) {
              <span
                class="border-border flex-1 rounded-[2px] border border-dashed"></span>
            } @else {
              <span
                class="bg-card border-foreground/8 flex-1 rounded-[2px] border"></span>
            }
          </span>
        }
      </span>
    } @else {
      <span
        class="border-foreground/8 bg-foreground/2 flex min-h-14.5 gap-1.5 rounded-lg border p-2.5"
        aria-hidden="true">
        @for (row of rows(); track row.id) {
          <span
            class="flex flex-1 flex-col gap-1"
            [class.opacity-45]="row.hidden">
            <span class="bg-foreground/14 h-1.5 shrink-0 rounded-[2px]"></span>
            @if (row.hidden || !row.taskCount) {
              <span
                class="border-border flex-1 rounded-[3px] border border-dashed"></span>
            } @else {
              @for (card of cards(row.taskCount); track card) {
                <span
                  class="bg-card border-foreground/8 h-5.5 shrink-0 rounded-[3px] border"></span>
              }
            }
          </span>
        }
      </span>
    }
  `,
})
export class ManageBoardGroupsPreviewComponent {
  readonly rows = input.required<readonly ManageBoardGroupRow[]>();
  readonly dense = input(false);

  protected cards(taskCount: number): number[] {
    const count = Math.min(taskCount, PREVIEW_CARD_LIMIT);

    return Array.from({ length: count }, (_, index) => index);
  }
}

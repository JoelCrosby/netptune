import { Component, computed, input, output } from '@angular/core';
import {
  LucideCircleCheck,
  LucideCircleDashed,
  LucideLoaderCircle,
} from '@lucide/angular';
import { ProgressBarComponent } from '@static/components/progress-bar/progress-bar.component';
import { AiChangeLetter, letterColour } from './ai-assistant-diff';

/** One kind of change, counted as the run works through it. */
export interface AiApplyRowView {
  key: string;
  letter: AiChangeLetter | null;
  lead: string;
  emphasis: string;
  trail: string;
  label: string;
  done: number;
  total: number;
}

@Component({
  selector: 'app-ai-assistant-apply-progress',
  host: { class: 'block' },
  imports: [
    LucideCircleCheck,
    LucideCircleDashed,
    LucideLoaderCircle,
    ProgressBarComponent,
  ],
  template: `
    <div
      class="border-border bg-card-header flex items-center gap-2 border-b px-3 py-2.5">
      <svg
        lucideLoaderCircle
        class="text-primary h-3.5 w-3.5 shrink-0 animate-spin"></svg>
      <h3 class="text-[13px] font-medium whitespace-nowrap">
        {{ heading() }}
      </h3>
    </div>

    <app-progress-bar
      class="h-0.5"
      [rounded]="false"
      [value]="percent()"
      [mode]="total() === 0 ? 'indeterminate' : 'determinate'" />

    <div class="flex flex-col">
      @for (row of rows(); track row.key) {
        <div
          class="border-border/55 flex items-center gap-2.25 border-b px-3 py-2.25 last:border-b-0"
          [class.opacity-60]="row.done === 0">
          <span class="flex h-4 w-4 shrink-0 items-center justify-center">
            @if (row.done === row.total) {
              <svg
                lucideCircleCheck
                class="text-change-added h-4 w-4"
                [attr.aria-label]="doneLabel"></svg>
            } @else if (row.done > 0) {
              <svg
                lucideLoaderCircle
                class="text-primary h-4 w-4 animate-spin"
                [attr.aria-label]="runningLabel"></svg>
            } @else {
              <svg
                lucideCircleDashed
                class="text-muted h-4 w-4"
                [attr.aria-label]="waitingLabel"></svg>
            }
          </span>

          @if (row.letter; as letter) {
            <span
              class="font-avatar w-3.5 shrink-0 text-[12px] font-bold"
              [class]="letterColour(letter)"
              aria-hidden="true">
              {{ letter }}
            </span>
          } @else {
            <span class="w-3.5 shrink-0"></span>
          }

          <span
            class="text-muted min-w-0 flex-1 truncate text-[13px]"
            [title]="row.label"
            >{{ row.lead
            }}<span class="text-foreground">{{ row.emphasis }}</span
            >{{ row.trail }}</span
          >

          <span class="text-muted shrink-0 text-[11.5px] tabular-nums">
            {{ row.done }}/{{ row.total }}
          </span>
        </div>
      }
    </div>

    <div
      class="border-border bg-card-header flex items-center gap-2 border-t px-3 py-2.5">
      <button
        type="button"
        class="border-border text-foreground hover:bg-card-hover flex h-8 items-center rounded-md border px-3.5 text-[13px] font-medium transition-colors disabled:opacity-50"
        [disabled]="isStopping()"
        (click)="stopped.emit()">
        @if (isStopping()) {
          <span i18n="Shown on the stop button once a stop was asked for"
            >Stopping…</span
          >
        } @else {
          <span i18n="Button that stops changes part way through being applied"
            >Stop</span
          >
        }
      </button>
    </div>
  `,
})
export class AiAssistantApplyProgressComponent {
  readonly rows = input.required<AiApplyRowView[]>();
  readonly completed = input.required<number>();
  readonly total = input.required<number>();
  readonly percent = input.required<number>();
  readonly isStopping = input(false);

  readonly stopped = output();

  protected readonly doneLabel = $localize`:Marks a group of changes that has finished applying:Applied`;
  protected readonly runningLabel = $localize`:Marks a group of changes being applied:Applying`;
  protected readonly waitingLabel = $localize`:Marks a group of changes waiting to be applied:Waiting`;

  protected readonly letterColour = letterColour;

  protected readonly heading = computed(() => {
    if (this.isStopping()) {
      return $localize`:Shown while a run is being stopped:Stopping after the current change…`;
    }

    const completed = this.completed();
    const total = this.total();

    return $localize`:Counts the changes a run has applied so far:${completed}:COMPLETED: of ${total}:TOTAL: done…`;
  });
}

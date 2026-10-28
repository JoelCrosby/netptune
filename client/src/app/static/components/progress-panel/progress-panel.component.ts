import { Component, computed, input } from '@angular/core';
import {
  LucideArrowDownRight,
  LucideArrowRight,
  LucideArrowUpRight,
} from '@lucide/angular';
import { PanelBodyComponent } from '../panel-body.component';
import { PanelComponent } from '../panel.component';
import {
  StatStripComponent,
  StatStripItem,
} from '../stat-strip/stat-strip.component';
import { SignalTone, signalToneSoft } from '../tones';

export type ProgressSegmentColor =
  'primary' | 'primarySoft' | 'faint' | 'info' | 'empty';

export interface ProgressSegment {
  label: string;
  count: number;
  color: ProgressSegmentColor;
}

export interface ProgressMarker {
  percent: number;
  label: string;
}

export interface TrendBadge {
  label: string;
  tone: SignalTone;
  direction: 'up' | 'flat' | 'down';
}

const markerLabelMinPercent = 12;
const markerLabelMaxPercent = 88;

const segmentBarClasses: Record<ProgressSegmentColor, string> = {
  primary: 'bg-primary',
  primarySoft: 'bg-primary/60',
  faint: 'bg-foreground/25',
  info: 'bg-blue-500',
  empty: 'bg-transparent',
};

const segmentSwatchClasses: Record<ProgressSegmentColor, string> = {
  primary: 'bg-primary',
  primarySoft: 'bg-primary/60',
  faint: 'bg-foreground/25',
  info: 'bg-blue-500',
  empty: 'bg-foreground/10 border border-foreground/25',
};

@Component({
  selector: 'app-progress-panel',
  imports: [
    LucideArrowDownRight,
    LucideArrowRight,
    LucideArrowUpRight,
    PanelBodyComponent,
    PanelComponent,
    StatStripComponent,
  ],
  host: { class: 'block' },
  template: `
    <section app-panel surface="card">
      <app-panel-body>
        <div
          class="flex flex-wrap items-baseline justify-between gap-x-4 gap-y-2">
          <p class="flex flex-wrap items-baseline gap-x-2 gap-y-1">
            <span class="text-3xl font-semibold tracking-tight tabular-nums">
              {{ value() }}%
            </span>
            {{ valueLabel() }}

            @if (badge(); as badge) {
              <span
                class="ml-1 inline-flex h-6 items-center gap-1.5 self-center rounded-full px-2.5 text-xs font-semibold"
                [class]="badgeClass()">
                @switch (badge.direction) {
                  @case ('up') {
                    <svg lucideArrowUpRight class="h-3.5 w-3.5"></svg>
                  }
                  @case ('down') {
                    <svg lucideArrowDownRight class="h-3.5 w-3.5"></svg>
                  }
                  @default {
                    <svg lucideArrowRight class="h-3.5 w-3.5"></svg>
                  }
                }
                {{ badge.label }}
              </span>
            }
          </p>

          @if (caption()) {
            <p class="text-muted text-sm tabular-nums">{{ caption() }}</p>
          }
        </div>

        <div class="relative mt-4">
          <div class="bg-foreground/10 flex h-2 overflow-hidden rounded-full">
            @for (segment of segments(); track segment.label) {
              <div
                [class]="barClasses[segment.color]"
                [style.width.%]="segmentPercent(segment)"
                [attr.title]="segment.label"></div>
            }
          </div>

          @if (marker(); as marker) {
            <div
              class="bg-foreground absolute -top-1 -bottom-1 w-0.5 rounded-sm"
              [style.left.%]="marker.percent"></div>
            <div
              class="text-muted absolute top-4 -translate-x-1/2 text-xs font-medium whitespace-nowrap"
              [style.left.%]="markerLabelPercent()">
              {{ marker.label }}
            </div>
          }
        </div>

        <div
          class="text-muted flex flex-wrap gap-x-4 gap-y-1.5 text-xs"
          [class]="marker() ? 'mt-9' : 'mt-4'">
          @for (segment of segments(); track segment.label) {
            <span class="flex items-center gap-1.5">
              <span
                class="h-2 w-2 rounded-xs"
                [class]="swatchClasses[segment.color]"></span>
              {{ segment.label }} {{ segment.count }}
            </span>
          }
        </div>
      </app-panel-body>

      @if (stats().length) {
        <app-stat-strip [items]="stats()" />
      }
    </section>
  `,
})
export class ProgressPanelComponent {
  readonly value = input.required<number>();
  readonly valueLabel = input('');
  readonly caption = input('');
  readonly total = input.required<number>();
  readonly segments = input.required<readonly ProgressSegment[]>();
  readonly marker = input<ProgressMarker | null>(null);
  readonly badge = input<TrendBadge | null>(null);
  readonly stats = input<readonly StatStripItem[]>([]);

  protected readonly barClasses = segmentBarClasses;
  protected readonly swatchClasses = segmentSwatchClasses;

  protected readonly badgeClass = computed(() => {
    const badge = this.badge();

    return badge ? signalToneSoft[badge.tone] : '';
  });

  protected readonly markerLabelPercent = computed(() => {
    const percent = this.marker()?.percent ?? 0;

    return Math.min(
      markerLabelMaxPercent,
      Math.max(markerLabelMinPercent, percent)
    );
  });

  protected segmentPercent(segment: ProgressSegment): number {
    const total = this.total();

    return total ? (segment.count / total) * 100 : 0;
  }
}

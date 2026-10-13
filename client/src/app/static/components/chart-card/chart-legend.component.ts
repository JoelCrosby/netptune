import { Component, input } from '@angular/core';

export type ChartLegendColor = 'primary' | 'primarySoft' | 'muted' | 'faint';
export type ChartLegendLine = 'solid' | 'dashed';

export interface ChartLegendItem {
  label: string;
  color: ChartLegendColor;
  line?: ChartLegendLine;
}

const solidClasses: Record<ChartLegendColor, string> = {
  primary: 'bg-primary h-0.5',
  primarySoft: 'bg-primary/60 h-0.5',
  muted: 'bg-muted h-px',
  faint: 'bg-foreground/25 h-0.5',
};

const dashedClasses: Record<ChartLegendColor, string> = {
  primary: 'border-primary h-0 border-t-2 border-dashed',
  primarySoft: 'border-primary/60 h-0 border-t-2 border-dashed',
  muted: 'border-muted h-0 border-t border-dashed',
  faint: 'border-foreground/25 h-0 border-t-2 border-dashed',
};

@Component({
  selector: 'app-chart-legend',
  host: { class: 'text-muted flex flex-wrap gap-x-3.5 gap-y-1 text-[11px]' },
  template: `
    @for (item of items(); track item.label) {
      <span class="flex items-center gap-1.5">
        <span
          class="w-3.5"
          [class]="
            item.line === 'dashed'
              ? dashedClasses[item.color]
              : solidClasses[item.color]
          "></span>
        {{ item.label }}
      </span>
    }
  `,
})
export class ChartLegendComponent {
  readonly items = input.required<readonly ChartLegendItem[]>();

  protected readonly solidClasses = solidClasses;
  protected readonly dashedClasses = dashedClasses;
}

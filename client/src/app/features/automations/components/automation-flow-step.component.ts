import { Component, computed, input } from '@angular/core';
import { LucideDynamicIcon, type LucideIconInput } from '@lucide/angular';
import { cn } from '@static/components/button/button.variants';

export type AutomationFlowNodeAppearance = 'solid' | 'outline' | 'dashed';

const nodeAppearanceClasses: Record<AutomationFlowNodeAppearance, string> = {
  solid: 'bg-primary text-primary-foreground shadow-sm',
  outline: 'bg-card border-border text-foreground/45 border',
  dashed: 'border-border text-foreground/45 border border-dashed',
};

@Component({
  selector: 'app-automation-flow-step',
  imports: [LucideDynamicIcon],
  host: { class: 'grid grid-cols-[2.25rem_minmax(0,1fr)] gap-x-3' },
  template: `
    <div class="relative flex justify-center" aria-hidden="true">
      @if (connected()) {
        <div class="bg-border absolute top-7.5 bottom-0 left-1/2 w-px"></div>
      }

      <div [class]="nodeClass()">
        @if (icon(); as nodeIcon) {
          <svg [lucideIcon]="nodeIcon" class="h-3.5 w-3.5"></svg>
        } @else {
          {{ step() }}
        }
      </div>
    </div>

    <div [class]="contentClass()">
      <ng-content />
    </div>
  `,
})
export class AutomationFlowStepComponent {
  readonly icon = input<LucideIconInput | null>(null);
  readonly step = input<number | null>(null);
  readonly appearance = input<AutomationFlowNodeAppearance>('solid');
  readonly connected = input(true);

  protected readonly nodeClass = computed(() => {
    return cn(
      'relative flex h-7 w-7 items-center justify-center rounded-full text-xs font-bold',
      nodeAppearanceClasses[this.appearance()]
    );
  });

  protected readonly contentClass = computed(() => {
    return this.connected() ? 'min-w-0 pb-3.5' : 'min-w-0';
  });
}

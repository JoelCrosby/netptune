import { NgTemplateOutlet } from '@angular/common';
import { Component, computed, input, output } from '@angular/core';
import { LucideDynamicIcon, type LucideIconInput } from '@lucide/angular';
import { cn } from '@static/components/button/button.variants';

export type FlowStepSize = 'default' | 'compact';

export type FlowStepChange = 'added' | 'removed' | 'changed';

const changeClasses: Record<FlowStepChange, string> = {
  added: 'border-green-500/45 bg-green-500/6',
  removed: 'border-warn/40 bg-warn/5 opacity-75',
  changed: 'border-blue-500/45 bg-blue-500/6',
};

@Component({
  selector: 'app-flow-step',
  imports: [LucideDynamicIcon, NgTemplateOutlet],
  template: `
    <ng-template #content>
      <span class="flex items-center justify-between gap-2">
        <span
          class="text-primary flex items-center gap-1.5 text-[0.6875rem] font-bold tracking-[0.12em]">
          @if (icon(); as stepIcon) {
            <svg [lucideIcon]="stepIcon" class="h-3.25 w-3.25"></svg>
          } @else if (step(); as stepNumber) {
            <span
              class="bg-primary text-primary-foreground inline-flex h-4 w-4 items-center justify-center rounded-full text-[0.625rem] tracking-normal">
              {{ stepNumber }}
            </span>
          }
          <span>{{ keyword() }}</span>
        </span>

        <ng-content select="[flowStepAside]" />
      </span>

      <span [class]="titleClass()">{{ title() }}</span>

      <span class="text-foreground/60 text-[13px] leading-[1.45] text-pretty">
        <ng-content />
      </span>
    </ng-template>

    @if (interactive()) {
      <button
        type="button"
        [class]="stepClass()"
        [attr.aria-current]="selected() ? 'step' : null"
        (click)="selectStep.emit()">
        <ng-container [ngTemplateOutlet]="content" />
      </button>
    } @else {
      <div [class]="stepClass()">
        <ng-container [ngTemplateOutlet]="content" />
      </div>
    }
  `,
})
export class FlowStepComponent {
  readonly keyword = input.required<string>();
  readonly title = input.required<string>();
  readonly icon = input<LucideIconInput | null>(null);
  readonly step = input<number | null>(null);
  readonly selected = input(false);
  readonly interactive = input(true);
  readonly size = input<FlowStepSize>('default');
  readonly change = input<FlowStepChange | null>(null);

  readonly selectStep = output();

  protected readonly titleClass = computed(() => {
    return cn(
      'text-foreground font-semibold',
      this.size() === 'compact' ? 'text-sm' : 'text-[15px]',
      this.change() === 'removed' && 'line-through'
    );
  });

  protected readonly stepClass = computed(() => {
    const compact = this.size() === 'compact';
    const interactive = this.interactive();
    const change = this.change();

    return cn(
      'flex w-full flex-col rounded-[10px] border text-left',
      compact ? 'gap-0.75 px-3.5 py-3' : 'gap-1 px-4 py-3.5',
      interactive &&
        'focus-visible:ring-primary cursor-pointer transition-colors focus-visible:ring-2 focus-visible:outline-none',
      this.selected()
        ? 'border-primary/55 bg-primary/8'
        : 'border-border bg-card',
      interactive && !this.selected() && 'hover:border-primary/35',
      change && changeClasses[change]
    );
  });
}

import { NgTemplateOutlet } from '@angular/common';
import { Component, computed, input, output } from '@angular/core';
import { LucideDynamicIcon, type LucideIconInput } from '@lucide/angular';
import { cn } from '@static/components/button/button.variants';

export type AutomationFlowNodeSize = 'default' | 'compact';

// One step in a rule's flow. The node carries a keyword and either an icon or the action's
// position, a title, and whatever summary the caller projects underneath. The editor makes it a
// button that selects the step; the detail rail shows it as a static card.
@Component({
  selector: 'app-automation-flow-node',
  imports: [LucideDynamicIcon, NgTemplateOutlet],
  template: `
    <ng-template #content>
      <span class="flex items-center justify-between gap-2">
        <span
          class="text-primary flex items-center gap-1.5 text-[0.6875rem] font-bold tracking-[0.12em]">
          @if (icon(); as nodeIcon) {
            <svg [lucideIcon]="nodeIcon" class="h-3.25 w-3.25"></svg>
          } @else if (step(); as stepNumber) {
            <span
              class="bg-primary text-primary-foreground inline-flex h-4 w-4 items-center justify-center rounded-full text-[0.625rem] tracking-normal">
              {{ stepNumber }}
            </span>
          }
          <span>{{ keyword() }}</span>
        </span>

        <ng-content select="[flowNodeAside]" />
      </span>

      <span [class]="titleClass()">{{ title() }}</span>

      <span class="text-foreground/60 text-[13px] leading-[1.45] text-pretty">
        <ng-content />
      </span>
    </ng-template>

    @if (interactive()) {
      <button
        type="button"
        [class]="nodeClass()"
        [attr.aria-current]="selected() ? 'step' : null"
        (click)="selectNode.emit()">
        <ng-container [ngTemplateOutlet]="content" />
      </button>
    } @else {
      <div [class]="nodeClass()">
        <ng-container [ngTemplateOutlet]="content" />
      </div>
    }
  `,
})
export class AutomationFlowNodeComponent {
  readonly keyword = input.required<string>();
  readonly title = input.required<string>();
  readonly icon = input<LucideIconInput | null>(null);
  readonly step = input<number | null>(null);
  readonly selected = input(false);
  readonly interactive = input(true);
  readonly size = input<AutomationFlowNodeSize>('default');

  readonly selectNode = output();

  protected readonly titleClass = computed(() => {
    return cn(
      'text-foreground font-semibold',
      this.size() === 'compact' ? 'text-sm' : 'text-[15px]'
    );
  });

  protected readonly nodeClass = computed(() => {
    const compact = this.size() === 'compact';
    const interactive = this.interactive();

    return cn(
      'flex w-full flex-col rounded-[10px] border text-left',
      compact ? 'gap-0.75 px-3.5 py-3' : 'gap-1 px-4 py-3.5',
      interactive &&
        'focus-visible:ring-primary cursor-pointer transition-colors focus-visible:ring-2 focus-visible:outline-none',
      this.selected()
        ? 'border-primary/55 bg-primary/8'
        : 'border-border bg-card',
      interactive && !this.selected() && 'hover:border-primary/35'
    );
  });
}

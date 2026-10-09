import { Component, computed, input, output } from '@angular/core';
import { LucideDynamicIcon, type LucideIconInput } from '@lucide/angular';
import { cn } from '@static/components/button/button.variants';

// One step in the editor's flow list. The node carries a keyword and either an icon or the
// action's position, a title, and whatever summary the caller projects underneath.
@Component({
  selector: 'app-automation-flow-node',
  imports: [LucideDynamicIcon],
  template: `
    <button
      type="button"
      [class]="buttonClass()"
      [attr.aria-current]="selected() ? 'step' : null"
      (click)="selectNode.emit()">
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

      <span class="text-foreground text-[15px] font-semibold">
        {{ title() }}
      </span>

      <span class="text-foreground/60 text-[13px] leading-[1.45] text-pretty">
        <ng-content />
      </span>
    </button>
  `,
})
export class AutomationFlowNodeComponent {
  readonly keyword = input.required<string>();
  readonly title = input.required<string>();
  readonly icon = input<LucideIconInput | null>(null);
  readonly step = input<number | null>(null);
  readonly selected = input(false);

  readonly selectNode = output();

  protected readonly buttonClass = computed(() => {
    return cn(
      'focus-visible:ring-primary flex w-full cursor-pointer flex-col gap-1 rounded-[10px] border px-4 py-3.5 text-left transition-colors focus-visible:ring-2 focus-visible:outline-none',
      this.selected()
        ? 'border-primary/55 bg-primary/8'
        : 'border-border bg-card hover:border-primary/35'
    );
  });
}

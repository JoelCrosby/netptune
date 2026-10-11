import { computed, Component, input, output } from '@angular/core';
import { cn } from '../button/button.variants';

export interface OutlineItem {
  id: number;
  label: string;
  level: number;
}

const indentPerLevelPx = 16;
const baseIndentPx = 8;

@Component({
  selector: 'app-outline-list',
  host: { '[class]': 'hostClass()' },
  template: `
    <ul
      class="flex flex-col gap-px text-[13px]"
      [attr.aria-label]="ariaLabel()">
      @for (item of items(); track item.id) {
        <li>
          <button
            type="button"
            [class]="itemClass(item)"
            [style.padding-left.px]="indent(item)"
            [attr.aria-current]="item.id === activeId() ? 'location' : null"
            (click)="selected.emit(item.id)">
            <span
              class="h-[5px] w-[5px] shrink-0 rounded-full"
              [class.bg-primary]="item.id === activeId()"></span>
            <span class="truncate">{{ item.label }}</span>
          </button>
        </li>
      }
    </ul>
  `,
})
export class OutlineListComponent {
  readonly class = input('');

  protected readonly hostClass = computed(() => cn('block', this.class()));

  readonly items = input.required<readonly OutlineItem[]>();
  readonly activeId = input<number | null>(null);
  readonly ariaLabel = input<string | null>(null);

  readonly selected = output<number>();

  protected indent(item: OutlineItem): number {
    return (
      baseIndentPx +
      Math.max(0, item.level - this.topLevel()) * indentPerLevelPx
    );
  }

  protected itemClass(item: OutlineItem): string {
    const active = item.id === this.activeId();

    return cn(
      'hover:bg-foreground/5 flex h-[30px] w-full items-center gap-2 rounded-md pr-2 text-left',
      active ? 'text-primary font-semibold' : 'text-foreground/70'
    );
  }

  private topLevel(): number {
    const levels = this.items().map((item) => item.level);

    return levels.length ? Math.min(...levels) : 1;
  }
}

import { Component, computed, input, model } from '@angular/core';
import { LucideDynamicIcon, LucideIconInput } from '@lucide/angular';
import { cn } from '../button/button.variants';

export interface SectionNavItem {
  value: string;
  label: string;
  icon: LucideIconInput;
  // A null count hides the badge; a zero count hides it and dims the row, so a
  // list of mostly empty sections does not read as a column of noughts.
  count?: number | null;
}

export interface SectionNavGroup {
  key: string;
  label?: string | null;
  items: SectionNavItem[];
}

@Component({
  selector: 'app-section-nav',
  imports: [LucideDynamicIcon],
  host: { '[class]': 'hostClass()' },
  template: `
    <nav class="flex flex-col gap-px" [attr.aria-label]="ariaLabel()">
      @for (group of groups(); track group.key; let first = $first) {
        @if (!first) {
          <div
            class="bg-foreground/8 mx-2.5 mt-2.5 mb-2 h-px"
            aria-hidden="true"></div>
        }

        @if (group.label) {
          <div
            class="text-foreground/40 px-2.5 pb-1.5 text-[10px] font-semibold tracking-[0.8px] uppercase">
            {{ group.label }}
          </div>
        }

        @for (item of group.items; track item.value) {
          <button
            type="button"
            [class]="itemClass(item)"
            [attr.aria-current]="item.value === value() ? 'true' : null"
            (click)="value.set(item.value)">
            <svg [lucideIcon]="item.icon" class="h-4 w-4 shrink-0"></svg>
            <span class="flex-1 truncate text-left">{{ item.label }}</span>
            @if (item.count) {
              <span class="text-muted text-xs font-medium">
                {{ item.count }}
              </span>
            }
          </button>
        }
      }
    </nav>
  `,
})
export class SectionNavComponent {
  readonly groups = input.required<SectionNavGroup[]>();
  readonly value = model<string>('');
  readonly ariaLabel = input<string | null>(null);
  readonly class = input('');

  protected readonly hostClass = computed(() => cn('block', this.class()));

  protected itemClass(item: SectionNavItem): string {
    const isActive = item.value === this.value();
    const isEmpty = item.count === 0;

    return cn(
      'focus-visible:ring-primary flex h-8 w-full cursor-pointer items-center gap-2.5 rounded-lg px-2.5 text-[13px] transition-colors outline-none focus-visible:ring-2',
      isActive && 'bg-foreground/8 text-foreground font-medium',
      !isActive && isEmpty && 'text-muted hover:bg-hover hover:text-foreground',
      !isActive &&
        !isEmpty &&
        'text-foreground/80 hover:bg-hover hover:text-foreground'
    );
  }
}

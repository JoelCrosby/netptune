import {
  booleanAttribute,
  Component,
  computed,
  input,
  output,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { cn } from '../button/button.variants';
import { InlineButtonComponent } from '../button/inline-button.component';

export interface RowListItem {
  id: string | number;
  title: string;
  subtitle?: string | null;
  meta?: readonly string[];
  link: string | readonly unknown[];
  actionLabel?: string | null;
}

@Component({
  selector: 'app-row-list',
  imports: [RouterLink, InlineButtonComponent],
  host: { class: 'block' },
  template: `
    <ul [class]="listClass()" [attr.aria-label]="ariaLabel()">
      @for (item of items(); track item.id) {
        <li
          class="hover:bg-hover flex min-h-[52px] items-center gap-3 px-6 py-1.5 transition-colors">
          <a
            class="focus-visible:ring-primary flex min-w-0 flex-1 items-center gap-3 rounded focus-visible:ring-2 focus-visible:outline-none"
            [routerLink]="item.link">
            <span class="flex min-w-0 flex-1 flex-col gap-px">
              <span class="truncate text-sm font-medium">{{ item.title }}</span>
              @if (item.subtitle) {
                <span class="text-foreground/60 truncate text-xs">{{
                  item.subtitle
                }}</span>
              }
            </span>
            @for (value of item.meta ?? []; track $index) {
              <span
                class="text-foreground/60 w-28 shrink-0 truncate text-right text-xs max-md:hidden"
                >{{ value }}</span
              >
            }
          </a>
          @if (item.actionLabel) {
            <button
              app-inline-button
              type="button"
              (click)="actioned.emit(item.id)">
              {{ item.actionLabel }}
            </button>
          }
        </li>
      } @empty {
        <li class="text-foreground/60 px-6 py-5 text-sm">
          {{ emptyLabel() }}
        </li>
      }
    </ul>
  `,
})
export class RowListComponent {
  readonly items = input.required<readonly RowListItem[]>();
  readonly ariaLabel = input.required<string>();
  readonly emptyLabel = input('');
  readonly flush = input(false, { transform: booleanAttribute });

  readonly actioned = output<string | number>();

  protected readonly listClass = computed(() => {
    return cn(
      'divide-border divide-y overflow-hidden',
      !this.flush() && 'border-border bg-card rounded-lg border'
    );
  });
}

import { computed, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { cn } from '../button/button.variants';

export interface BreadcrumbItem {
  id: string | number;
  label: string;
  link?: string[] | null;
}

@Component({
  selector: 'app-breadcrumb',
  imports: [RouterLink],
  host: { '[class]': 'hostClass()' },
  template: `
    <nav class="min-w-0" [attr.aria-label]="ariaLabel()">
      <ol
        class="text-foreground/50 flex min-w-0 items-center gap-1.5 overflow-hidden px-0.5 py-0.5 text-[13px] whitespace-nowrap">
        @for (
          item of items();
          track item.id;
          let first = $first, last = $last
        ) {
          <li
            class="flex min-w-0 items-center gap-1.5"
            [class.shrink-0]="first && !last"
            [class.max-w-40]="!last">
            @if (last) {
              <span
                class="text-foreground truncate font-medium"
                aria-current="page">
                {{ item.label }}
              </span>
            } @else {
              @if (item.link; as link) {
                <a
                  class="hover:text-foreground truncate hover:underline"
                  [routerLink]="link">
                  {{ item.label }}
                </a>
              } @else {
                <span class="truncate">{{ item.label }}</span>
              }
              <span class="text-foreground/25" aria-hidden="true">/</span>
            }
          </li>
        }
      </ol>
    </nav>
  `,
})
export class BreadcrumbComponent {
  readonly class = input('');

  protected readonly hostClass = computed(() =>
    cn('flex min-w-0', this.class())
  );

  readonly items = input.required<readonly BreadcrumbItem[]>();
  readonly ariaLabel = input<string | null>(null);
}

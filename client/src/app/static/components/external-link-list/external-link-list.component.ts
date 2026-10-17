import { Component, computed, input, signal } from '@angular/core';
import {
  LucideDynamicIcon,
  LucideExternalLink,
  type LucideIconInput,
} from '@lucide/angular';
import { TooltipDirective } from '../../directives/tooltip.directive';
import { BadgeComponent, type BadgeColor } from '../badge/badge.component';
import { cn } from '../button/button.variants';
import { InlineButtonComponent } from '../button/inline-button.component';
import { ProviderMarkComponent } from '../provider-mark.component';

export interface ExternalLinkBadge {
  label: string;
  color: BadgeColor;
  title?: string;
}

export interface ExternalLinkListItem {
  id: string | number;
  href: string;
  icon: LucideIconInput;
  label: string;
  meta: string;
  provider?: string;
  muted?: boolean;
  badges: readonly ExternalLinkBadge[];
}

export interface ExternalLinkGroup {
  id: string;
  label: string;
  items: readonly ExternalLinkListItem[];
}

@Component({
  selector: 'app-external-link-list',
  imports: [
    BadgeComponent,
    InlineButtonComponent,
    LucideDynamicIcon,
    LucideExternalLink,
    ProviderMarkComponent,
    TooltipDirective,
  ],
  host: { '[class]': 'hostClass()' },
  template: `
    @for (group of groups(); track group.id) {
      <section class="flex flex-col gap-1" [attr.aria-label]="group.label">
        @if (groups().length > 1) {
          <h3
            class="text-muted px-2.5 text-[11px] font-semibold tracking-[0.06em] uppercase">
            {{ group.label }}
          </h3>
        }

        <ul class="flex flex-col gap-0.5 text-[13px]">
          @for (item of visibleItems(group); track item.id) {
            <li>
              <a
                class="group hover:bg-foreground/5 focus-visible:ring-primary flex items-start gap-2.5 rounded-lg px-2.5 py-2 focus-visible:ring-2 focus-visible:outline-none"
                target="_blank"
                rel="noopener noreferrer"
                [href]="item.href">
                <svg
                  aria-hidden="true"
                  class="mt-0.5 h-4 w-4 shrink-0"
                  [class.text-muted]="item.muted"
                  [lucideIcon]="item.icon"></svg>

                <span class="flex min-w-0 flex-1 flex-col gap-0.5">
                  <span
                    class="truncate font-medium"
                    [class.text-muted]="item.muted"
                    [class.line-through]="item.muted">
                    {{ item.label }}
                  </span>
                  <span
                    class="text-foreground/50 flex min-w-0 items-center gap-1.5 text-xs">
                    @if (item.provider) {
                      <app-provider-mark
                        [provider]="item.provider"
                        [size]="12" />
                    }
                    <span class="truncate">{{ item.meta }}</span>
                  </span>
                </span>

                <span class="flex shrink-0 items-center gap-1.5 self-center">
                  @for (badge of item.badges; track badge.label) {
                    <app-badge [color]="badge.color" [appTooltip]="badge.title">
                      {{ badge.label }}
                    </app-badge>
                  }
                  <span class="sr-only">{{ newTabLabel() }}</span>
                  <svg
                    lucideExternalLink
                    aria-hidden="true"
                    class="text-muted h-3.5 w-3.5 opacity-0 transition-opacity group-hover:opacity-100 group-focus-visible:opacity-100"></svg>
                </span>
              </a>
            </li>
          }
        </ul>

        @if (hiddenCount(group); as hidden) {
          <button
            app-inline-button
            color="muted"
            class="self-start px-2.5 py-1 text-xs"
            (click)="expand(group.id)">
            {{ showMoreLabel() }} ({{ hidden }})
          </button>
        }
      </section>
    }
  `,
})
export class ExternalLinkListComponent {
  readonly groups = input.required<readonly ExternalLinkGroup[]>();
  readonly limit = input(5);
  readonly showMoreLabel = input.required<string>();
  readonly newTabLabel = input.required<string>();
  readonly class = input('');

  private readonly expanded = signal<ReadonlySet<string>>(new Set());

  protected readonly hostClass = computed(() => {
    return cn('flex flex-col gap-3', this.class());
  });

  protected visibleItems(group: ExternalLinkGroup) {
    const isExpanded = this.expanded().has(group.id);

    return isExpanded ? group.items : group.items.slice(0, this.limit());
  }

  protected hiddenCount(group: ExternalLinkGroup) {
    const isExpanded = this.expanded().has(group.id);

    return isExpanded ? 0 : Math.max(0, group.items.length - this.limit());
  }

  protected expand(groupId: string) {
    this.expanded.update((current) => new Set([...current, groupId]));
  }
}

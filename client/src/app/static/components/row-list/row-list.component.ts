import {
  booleanAttribute,
  Component,
  computed,
  input,
  output,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucideDynamicIcon, type LucideIconInput } from '@lucide/angular';
import { AvatarComponent } from '../avatar/avatar.component';
import { TooltipDirective } from '../../directives/tooltip.directive';
import { cn } from '../button/button.variants';
import { IconButtonComponent } from '../button/icon-button.component';
import { InlineButtonComponent } from '../button/inline-button.component';

export interface RowListItem {
  id: string | number;
  title: string;
  subtitle?: string | null;
  icon?: LucideIconInput;
  avatarName?: string | null;
  meta?: readonly string[];
  link: string | readonly unknown[];
  actionLabel?: string | null;
  actionIcon?: LucideIconInput;
}

@Component({
  selector: 'app-row-list',
  imports: [
    RouterLink,
    AvatarComponent,
    IconButtonComponent,
    InlineButtonComponent,
    LucideDynamicIcon,
    TooltipDirective,
  ],
  host: { class: 'block' },
  template: `
    <ul [class]="listClass()" [attr.aria-label]="ariaLabel()">
      @for (item of items(); track item.id) {
        <li
          class="hover:bg-hover flex min-h-11 items-center gap-1 pr-2 transition-colors">
          <a
            class="focus-visible:ring-primary flex min-w-0 flex-1 items-center gap-3 rounded px-4 py-2 focus-visible:ring-2 focus-visible:outline-none"
            [routerLink]="item.link">
            @if (item.icon) {
              <svg
                class="text-foreground/40 size-3.75 shrink-0"
                [lucideIcon]="item.icon"
                aria-hidden="true"></svg>
            }
            <span class="flex min-w-0 flex-1 flex-col gap-px">
              <span class="truncate text-sm font-medium">{{ item.title }}</span>
              @if (item.subtitle) {
                <span class="text-foreground/60 truncate text-xs">{{
                  item.subtitle
                }}</span>
              }
            </span>
            @if (item.avatarName) {
              <app-avatar
                class="shrink-0"
                aria-hidden="true"
                [name]="item.avatarName" />
              <span class="sr-only">{{ item.avatarName }}</span>
            }
            @for (value of item.meta ?? []; track $index) {
              <span
                class="text-foreground/60 w-24 shrink-0 truncate text-right text-xs max-md:hidden"
                >{{ value }}</span
              >
            }
          </a>
          @if (item.actionLabel) {
            @if (item.actionIcon) {
              <button
                app-icon-button
                size="small"
                color="muted"
                type="button"
                [ariaLabel]="item.actionLabel"
                [appTooltip]="item.actionLabel"
                (click)="actioned.emit(item.id)">
                <svg class="h-4 w-4" [lucideIcon]="item.actionIcon"></svg>
              </button>
            } @else {
              <button
                app-inline-button
                type="button"
                (click)="actioned.emit(item.id)">
                {{ item.actionLabel }}
              </button>
            }
          }
        </li>
      } @empty {
        <li class="text-foreground/60 px-4 py-5 text-sm">
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
      'divide-border/60 divide-y overflow-hidden',
      !this.flush() && 'border-border bg-card rounded-lg border'
    );
  });
}

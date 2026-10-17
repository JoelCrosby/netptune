import { Component, input, output } from '@angular/core';
import { LucideX } from '@lucide/angular';
import { BadgeComponent, type BadgeColor } from '../badge/badge.component';
import { IconButtonComponent } from '../button/icon-button.component';
import { ProviderMarkComponent } from '../provider-mark.component';

export interface AccountListItem {
  id: number;
  provider: string;
  label: string;
  badge: { label: string; color: BadgeColor } | null;
  removable: boolean;
}

@Component({
  selector: 'app-account-list',
  imports: [
    BadgeComponent,
    IconButtonComponent,
    LucideX,
    ProviderMarkComponent,
  ],
  host: { class: 'block' },
  template: `
    <ul class="flex flex-col gap-1.5" [attr.aria-label]="ariaLabel()">
      @for (item of items(); track item.id) {
        <li
          class="border-border flex min-h-11 items-center gap-3 rounded-lg border px-3 text-sm">
          <app-provider-mark [provider]="item.provider" />
          <span class="min-w-0 flex-1 truncate font-medium">{{
            item.label
          }}</span>
          @if (item.badge; as badge) {
            <app-badge [color]="badge.color">{{ badge.label }}</app-badge>
          }
          @if (item.removable) {
            <button
              app-icon-button
              type="button"
              [attr.aria-label]="removeLabel() + ' ' + item.label"
              (click)="removed.emit(item.id)">
              <svg lucideX class="h-4 w-4"></svg>
            </button>
          }
        </li>
      } @empty {
        <li class="text-muted text-sm">{{ emptyLabel() }}</li>
      }
    </ul>
  `,
})
export class AccountListComponent {
  readonly items = input.required<readonly AccountListItem[]>();
  readonly ariaLabel = input.required<string>();
  readonly removeLabel = input.required<string>();
  readonly emptyLabel = input('');

  readonly removed = output<number>();
}

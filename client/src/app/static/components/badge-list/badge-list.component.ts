import { Component, input } from '@angular/core';
import { BadgeColor, BadgeComponent } from '../badge/badge.component';

export interface BadgeListItem {
  badge: string;
  color: BadgeColor;
  text: string;
}

@Component({
  selector: 'app-badge-list',
  imports: [BadgeComponent],
  host: { class: 'flex flex-col gap-1.5' },
  template: `
    @if (heading()) {
      <span class="text-muted text-xs font-medium">{{ heading() }}</span>
    }
    @if (lead()) {
      <p class="text-muted text-sm">{{ lead() }}</p>
    }
    @for (item of items(); track $index) {
      <div class="flex min-w-0 items-center gap-2 text-sm">
        <app-badge [color]="item.color">{{ item.badge }}</app-badge>
        <span class="truncate">{{ item.text }}</span>
      </div>
    }
  `,
})
export class BadgeListComponent {
  readonly items = input.required<BadgeListItem[]>();
  readonly heading = input('');
  readonly lead = input<string | null>(null);
}

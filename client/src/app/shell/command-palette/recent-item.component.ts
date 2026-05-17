import {
  ChangeDetectionStrategy,
  Component,
  input,
  output,
} from '@angular/core';
import { LucideClock } from '@lucide/angular';
import { RecentItem } from './recent-items.service';

@Component({
  selector: 'app-recent-item',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LucideClock],
  template: `
    <button
      type="button"
      class="aria-selected:bg-accent aria-selected:text-accent-foreground relative flex w-full cursor-default items-center gap-2 rounded-sm px-2 py-2 outline-none select-none"
      [attr.aria-selected]="selected() || null"
      (click)="activate.emit(item())"
      (mouseenter)="hover.emit()">
      <svg lucideClock class="h-4 w-4 shrink-0 opacity-50"></svg>
      <span class="flex-1 truncate text-left">{{ item().title }}</span>
    </button>
  `,
})
export class RecentItemComponent {
  item = input.required<RecentItem>();
  selected = input(false);
  activate = output<RecentItem>();
  hover = output();
}

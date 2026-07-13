import { Component, input, output } from '@angular/core';
import { LucideClock } from '@lucide/angular';
import { RecentItem } from './recent-items.service';
import { CommandPaletteItemComponent } from './command-palette-item.component';

@Component({
  selector: 'app-recent-item',
  imports: [LucideClock, CommandPaletteItemComponent],
  template: `
    <button
      app-command-palette-item
      [selected]="selected()"
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

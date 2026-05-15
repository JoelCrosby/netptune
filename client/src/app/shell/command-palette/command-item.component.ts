import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { LucideZap } from '@lucide/angular';
import { Command } from '@core/services/command-registry.service';

@Component({
  selector: 'app-command-item',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LucideZap],
  template: `
    <button
      type="button"
      class="relative flex w-full cursor-default select-none items-center gap-2 rounded-sm px-2 py-1.5 text-sm outline-none aria-selected:bg-accent aria-selected:text-accent-foreground"
      [attr.aria-selected]="selected() || null"
      (click)="activate.emit(command())"
      (mouseenter)="hover.emit()">
      <svg lucideZap class="h-4 w-4 shrink-0 opacity-50"></svg>
      <span class="flex-1 truncate text-left">{{ command().label }}</span>
      @if (command().shortcut) {
        <span class="ml-auto text-xs tracking-widest text-muted-foreground">{{ command().shortcut }}</span>
      }
    </button>
  `,
})
export class CommandItemComponent {
  command = input.required<Command>();
  selected = input(false);
  activate = output<Command>();
  hover = output();
}

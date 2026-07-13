import { Component, input, output } from '@angular/core';
import { LucideZap } from '@lucide/angular';
import { Command } from '@core/services/command-registry.service';
import { CommandPaletteItemComponent } from './command-palette-item.component';

@Component({
  selector: 'app-command-item',
  imports: [LucideZap, CommandPaletteItemComponent],
  template: `
    <button
      app-command-palette-item
      [selected]="selected()"
      (click)="activate.emit(command())"
      (mouseenter)="hover.emit()">
      <svg lucideZap class="h-4 w-4 shrink-0 opacity-50"></svg>
      <span class="flex-1 truncate text-left">{{ command().label }}</span>
      @if (command().shortcut) {
        <span class="text-muted-foreground ml-auto tracking-widest">{{
          command().shortcut
        }}</span>
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

import { Component, computed, input, output } from '@angular/core';
import { LucideZap } from '@lucide/angular';
import { Command } from '@core/services/command-registry.service';
import { KeyboardKeyComponent } from '@static/components/keyboard-key/keyboard-key.component';
import { CommandPaletteItemComponent } from './command-palette-item.component';

@Component({
  selector: 'app-command-item',
  imports: [LucideZap, CommandPaletteItemComponent, KeyboardKeyComponent],
  template: `
    <button
      app-command-palette-item
      [optionId]="optionId()"
      [selected]="selected()"
      (click)="activate.emit(command())"
      (mouseenter)="hover.emit()">
      <svg lucideZap class="h-4 w-4 shrink-0 opacity-50"></svg>
      <span class="flex-1 truncate text-left">{{ command().label }}</span>
      @if (shortcutKeys().length) {
        <span class="ml-auto flex shrink-0 items-center gap-1">
          @for (key of shortcutKeys(); track $index) {
            <app-keyboard-key>{{ key }}</app-keyboard-key>
          }
        </span>
      }
    </button>
  `,
})
export class CommandItemComponent {
  command = input.required<Command>();
  selected = input(false);
  optionId = input<string>();
  activate = output<Command>();
  hover = output();

  readonly shortcutKeys = computed(() => {
    return this.command().shortcut?.map((key) => key.toUpperCase()) ?? [];
  });
}

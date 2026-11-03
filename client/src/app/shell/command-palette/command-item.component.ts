import { Component, computed, input, output } from '@angular/core';
import { LucideMapPin, LucideZap } from '@lucide/angular';
import { Command } from '@core/services/command-registry.service';
import { KeyboardKeyComponent } from '@static/components/keyboard-key/keyboard-key.component';
import { HintTextDirective } from '@static/directives/hint-text.directive';
import { CommandPaletteItemComponent } from './command-palette-item.component';

@Component({
  selector: 'app-command-item',
  imports: [
    LucideMapPin,
    LucideZap,
    CommandPaletteItemComponent,
    HintTextDirective,
    KeyboardKeyComponent,
  ],
  template: `
    <button
      app-command-palette-item
      [optionId]="optionId()"
      [selected]="selected()"
      (click)="activate.emit(command())"
      (mousemove)="hover.emit()">
      @if (command().place) {
        <svg lucideMapPin class="h-4 w-4 shrink-0 opacity-50"></svg>
      } @else {
        <svg lucideZap class="h-4 w-4 shrink-0 opacity-50"></svg>
      }
      <span class="flex-1 truncate text-left">{{ command().label }}</span>
      @if (command().place; as place) {
        <span appHintText class="shrink-0">{{ place.section }}</span>
      }
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

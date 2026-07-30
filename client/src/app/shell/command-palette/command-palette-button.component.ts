import { Component, inject } from '@angular/core';
import { LucideSearch } from '@lucide/angular';
import { CommandPaletteService } from './command-palette.service';

@Component({
  selector: 'app-command-palette-button',
  imports: [LucideSearch],
  template: `
    <button
      type="button"
      class="text-muted bg-secondary-background hover:bg-secondary-background-hover hover:text-foreground flex h-8 min-w-64 cursor-pointer items-center gap-2 rounded-md px-3 text-xs transition-colors"
      (click)="commandPalette.open()"
      i18n-aria-label="
        Accessible label for the button that opens the command palette
      "
      aria-label="Open command palette">
      <svg lucideSearch class="h-3.5 w-3.5"></svg>
      <span
        class="hidden sm:inline"
        i18n="Label on the button that opens the command palette"
        >Search</span
      >
      <kbd
        class="bg-muted/10 ml-auto hidden rounded px-1 py-0.5 font-mono text-xs sm:inline"
        i18n="
          Keyboard shortcut hint for the command palette. Translate the modifier
          key to its local name (for example Strg in German); leave the K as-is
        "
        >Ctrl K</kbd
      >
    </button>
  `,
})
export class CommandPaletteButtonComponent {
  commandPalette = inject(CommandPaletteService);
}

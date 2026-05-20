import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { LucideSearch } from '@lucide/angular';
import { CommandPaletteService } from './command-palette.service';

@Component({
  selector: 'app-command-palette-button',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [LucideSearch],
  template: `
    <button
      type="button"
      class="border-border text-muted-foreground hover:bg-accent hover:text-foreground flex h-8 cursor-pointer items-center gap-2 rounded-md border px-3 text-xs transition-colors"
      (click)="commandPalette.open()"
      aria-label="Open command palette">
      <svg lucideSearch class="h-3.5 w-3.5"></svg>
      <span class="hidden sm:inline">Search</span>
      <kbd
        class="bg-muted/10 hidden rounded px-1 py-0.5 font-mono text-xs sm:inline"
        >Ctrl K</kbd
      >
    </button>
  `,
})
export class CommandPaletteButtonComponent {
  commandPalette = inject(CommandPaletteService);
}

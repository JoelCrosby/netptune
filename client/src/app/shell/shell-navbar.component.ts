import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { PageHeaderBackLinkComponent } from '@app/static/components/page-header/page-header-back-link.component';
import { Store } from '@ngrx/store';
import { ShellService } from './shell.service';
import { NotificationBellComponent } from '@app/entry/components/notification-bell/notification-bell.component';
import { CurrentSprintDropdownComponent } from './current-sprint-dropdown.component';
import { ProfileMenuComponent } from './profile-menu.component';
import { CommandPaletteService } from './command-palette/command-palette.service';
import { LucideSearch } from '@lucide/angular';

@Component({
  selector: 'app-shell-navbar',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    PageHeaderBackLinkComponent,
    NotificationBellComponent,
    CurrentSprintDropdownComponent,
    ProfileMenuComponent,
    LucideSearch,
  ],
  template: `
    <div
      class="bg-background border-border sticky z-10 flex h-full items-center justify-between border-b px-4">
      <div class="h-6">
        <app-page-header-back-link />
      </div>

      <div class="ml-auto flex items-center justify-end gap-2 py-2">
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
        <app-current-sprint-dropdown />
        <app-notification-bell />
        <app-profile-menu />
      </div>
    </div>
  `,
})
export class ShellNavbarComponent {
  readonly store = inject(Store);

  shell = inject(ShellService);
  commandPalette = inject(CommandPaletteService);
}

import { Component, computed, inject } from '@angular/core';
import { ThemeService } from '@core/services/theme.service';
import { BrandShellComponent } from '@static/components/brand-shell/brand-shell.component';
import { PlanetBackdropComponent } from '@static/components/planet-backdrop/planet-backdrop.component';
import { ThemeToggleButtonComponent } from '@static/components/theme-toggle-button.component';

@Component({
  selector: 'app-auth-shell',
  imports: [
    BrandShellComponent,
    PlanetBackdropComponent,
    ThemeToggleButtonComponent,
  ],
  template: `
    <app-brand-shell>
      <div class="flex items-center gap-3.5" shellActions>
        <app-theme-toggle-button [dark]="isDark()" (toggled)="toggleTheme()" />
        <ng-content select="[shellAction]" />
      </div>

      <app-planet-backdrop placement="backdrop" shellBackdrop />

      <ng-content />
    </app-brand-shell>
  `,
})
export class AuthShellComponent {
  private readonly themes = inject(ThemeService);

  protected readonly isDark = computed(() => this.themes.theme() === 'dark');

  toggleTheme() {
    this.themes.set(this.isDark() ? 'light' : 'dark');
  }
}

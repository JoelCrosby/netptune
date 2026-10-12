import { Component, computed, input, output } from '@angular/core';
import { LucideMoon, LucideSun } from '@lucide/angular';
import { StrokedButtonComponent } from './button/stroked-button.component';

@Component({
  selector: 'app-theme-toggle-button',
  imports: [LucideMoon, LucideSun, StrokedButtonComponent],
  template: `
    <button
      app-stroked-button
      color="neutral"
      size="small"
      class="w-8.5 min-w-0 px-0"
      type="button"
      [attr.aria-label]="label()"
      [attr.title]="label()"
      (click)="toggled.emit()">
      @if (dark()) {
        <svg lucideSun size="16" aria-hidden="true"></svg>
      } @else {
        <svg lucideMoon size="16" aria-hidden="true"></svg>
      }
    </button>
  `,
})
export class ThemeToggleButtonComponent {
  readonly dark = input(false);

  readonly toggled = output();

  protected readonly label = computed(() => {
    if (this.dark()) {
      return $localize`:Auth page button that switches from the dark theme to the light one:Switch to light theme`;
    }

    return $localize`:Auth page button that switches from the light theme to the dark one:Switch to dark theme`;
  });
}

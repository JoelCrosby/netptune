import { Component, computed, inject, signal } from '@angular/core';
import { LocaleService } from '@core/services/locale.service';
import { type Locale } from '@core/util/locale';
import {
  LucideCheck,
  LucideChevronDown,
  LucideLanguages,
} from '@lucide/angular';
import { MenuItemComponent } from '@static/components/dropdown-menu/menu-item.component';

@Component({
  selector: 'app-locale-switcher',
  imports: [MenuItemComponent, LucideCheck, LucideChevronDown, LucideLanguages],
  // An inline disclosure rather than a nested dropdown: the dropdown overlay owns
  // a backdrop, so a second one inside it would close the parent on first click.
  // Collapsed by default also means a stray click cannot trigger a locale change,
  // which reloads the document and discards unsaved work.
  template: `
    <button
      app-menu-item
      type="button"
      [attr.aria-expanded]="expanded()"
      [attr.aria-label]="toggleLabel()"
      (click)="expanded.set(!expanded())">
      <svg lucideLanguages class="h-4 w-4 shrink-0"></svg>
      <span i18n="Profile menu item that reveals the language options">
        Language
      </span>
      <span class="text-muted ml-auto text-xs">{{ current.name }}</span>
      <svg
        lucideChevronDown
        class="h-4 w-4 shrink-0 opacity-70 transition-transform"
        [class.rotate-180]="expanded()"></svg>
    </button>

    @if (expanded()) {
      @for (locale of locales; track locale.code) {
        <button
          app-menu-item
          type="button"
          class="pl-6"
          [attr.aria-current]="locale.code === current.code ? 'true' : null"
          (click)="select(locale)">
          <span class="w-4 shrink-0">
            @if (locale.code === current.code) {
              <svg lucideCheck class="h-4 w-4"></svg>
            }
          </span>
          {{ locale.name }}
        </button>
      }
    }
  `,
})
export class LocaleSwitcherComponent {
  private readonly localeService = inject(LocaleService);

  readonly expanded = signal(false);
  readonly locales = this.localeService.locales;
  readonly current = this.localeService.current;

  /** aria-label is a binding, so it cannot be marked with i18n- in the template. */
  readonly toggleLabel = computed(() => {
    return this.expanded()
      ? $localize`:Accessible label for the control that hides the language options:Hide language options`
      : $localize`:Accessible label for the control that shows the language options. LANGUAGE is the current language, shown in its own language:Change language, currently ${this.current.name}:LANGUAGE:`;
  });

  select(locale: Locale): void {
    this.localeService.switchTo(locale);
  }
}

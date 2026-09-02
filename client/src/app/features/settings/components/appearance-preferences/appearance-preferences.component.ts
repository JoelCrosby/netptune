import { Component, computed, inject, input, signal } from '@angular/core';
import {
  APPEARANCE_PAGE_WIDTH,
  APPEARANCE_TASK_DETAIL_LAYOUT,
  APPEARANCE_THEME,
  PreferenceOption,
  PreferenceScope,
  ResolvedPreferenceValue,
} from '@core/models/user-preferences';
import { UserPreferencesService } from '@core/services/user-preferences.service';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { cn } from '@static/components/button/button.variants';
import { FormSelectOptionComponent } from '@static/components/form-select/form-select-option.component';
import { FormSelectComponent } from '@static/components/form-select/form-select.component';
import { PreferenceListComponent } from '../preference-list/preference-list.component';
import {
  preferenceScopeLabel,
  PreferenceScopeSelection,
  selectedScopeFor,
  valueForScope,
} from '../preference-scope';
import { PageWidthPreviewComponent } from './page-width-preview.component';
import { TaskDetailLayoutPreviewComponent } from './task-detail-layout-preview.component';
import { ThemePreviewComponent } from './theme-preview.component';

const arrowKeys = ['ArrowRight', 'ArrowDown', 'ArrowLeft', 'ArrowUp'];

const descriptions: Record<string, string> = {
  [APPEARANCE_THEME]: $localize`:Description of the theme preference:Applies everywhere you sign in.`,
  [APPEARANCE_TASK_DETAIL_LAYOUT]: $localize`:Description of the task detail layout preference:How a task opens in the dialog and on its own page.`,
  [APPEARANCE_PAGE_WIDTH]: $localize`:Description of the page width preference:How far list, dashboard and report pages stretch across the window.`,
};

const resetLabel = $localize`:Button that returns a preference to its default value:Reset to default`;

const clearLabels: Record<string, string> = {
  [APPEARANCE_THEME]: $localize`:Button that drops the theme choice so the app follows the operating system:Use system theme`,
};

const selectedBadge = $localize`:Badge on the chosen option tile:Selected`;

const optionCaptions: Record<string, string> = {
  'summary-rail': $localize`:Caption describing the summary rail task detail layout:Fields as a scannable summary on the right, sections collapsed to one line.`,
  cockpit: $localize`:Caption describing the cockpit task detail layout:Editable chip row under the title, comments docked beside the description.`,
  document: $localize`:Caption describing the document task detail layout:One centred reading column; every field lives behind “All fields”.`,
  centered: $localize`:Caption describing the centered page width:Pages sit in a centred column that stops at a comfortable reading width.`,
  full: $localize`:Caption describing the full page width:Pages stretch edge to edge, fitting more on wide screens.`,
};

const tileWidths: Record<string, string> = {
  [APPEARANCE_THEME]: 'w-[232px]',
  [APPEARANCE_TASK_DETAIL_LAYOUT]: 'w-[268px]',
  [APPEARANCE_PAGE_WIDTH]: 'w-[268px]',
};

function hasTilePreview(preference: ResolvedPreferenceValue): boolean {
  const key = preference.definition.key;

  return key in tileWidths && preference.definition.options.length > 0;
}

@Component({
  selector: 'app-appearance-preferences',
  imports: [
    FormSelectComponent,
    FormSelectOptionComponent,
    PageWidthPreviewComponent,
    PreferenceListComponent,
    StrokedButtonComponent,
    TaskDetailLayoutPreviewComponent,
    ThemePreviewComponent,
  ],
  host: { class: 'block @container' },
  template: `
    @for (
      preference of tiled();
      track preference.definition.key;
      let last = $last
    ) {
      <div
        class="px-6 pt-5 pb-6"
        [class]="
          last && !remaining().length ? '' : 'border-border/50 border-b'
        ">
        <div class="mb-3.5 flex items-end gap-4 @max-[900px]:flex-wrap">
          <div class="min-w-0 pb-1">
            <div class="text-sm font-semibold">
              {{ preference.definition.label }}
            </div>
            <div class="text-muted mt-0.5 text-[13px]">
              {{ description(preference) }}
            </div>
          </div>

          <div
            class="ml-auto flex items-end gap-3 @max-[900px]:w-full @max-[900px]:flex-wrap">
            @if (preference.definition.allowedScopes.length > 1) {
              <app-form-select
                class="block w-46"
                [noMargin]="true"
                i18n-label="Label of the preference scope field"
                label="Scope"
                i18n-placeholder="Placeholder in the preference scope picker"
                placeholder="Select scope"
                [value]="selectedScope(preference)"
                (changed)="selectScope(preference, $event)">
                @for (
                  scope of preference.definition.allowedScopes;
                  track scope
                ) {
                  <app-form-select-option [value]="scope">
                    {{ scopeLabel(scope) }}
                  </app-form-select-option>
                }
              </app-form-select>
            }

            @if (canClear(preference)) {
              <button
                app-stroked-button
                type="button"
                class="shrink-0"
                (click)="clearValue(preference)">
                {{ clearLabel(preference) }}
              </button>
            }
          </div>
        </div>

        <div
          role="radiogroup"
          class="flex flex-wrap gap-3.5 transition-opacity duration-150"
          [attr.aria-label]="preference.definition.label"
          [class.pointer-events-none]="isPending(preference)"
          [class.opacity-60]="isPending(preference)"
          (keydown)="moveFocus($event)">
          @for (option of preference.definition.options; track option.value) {
            <button
              type="button"
              role="radio"
              [class]="tileClass(preference, option)"
              [attr.aria-checked]="isSelected(preference, option)"
              [attr.aria-label]="option.label"
              [attr.tabindex]="tabIndex(preference, option)"
              (click)="selectOption(preference, option)">
              @switch (preference.definition.key) {
                @case (themeKey) {
                  <app-theme-preview [theme]="option.value" />
                }
                @case (pageWidthKey) {
                  <app-page-width-preview [width]="option.value" />
                }
                @default {
                  <app-task-detail-layout-preview [layout]="option.value" />
                }
              }

              <span class="flex items-center gap-2">
                <span
                  class="flex h-4 w-4 shrink-0 items-center justify-center rounded-full border-2 transition-colors duration-150"
                  [class]="
                    isSelected(preference, option)
                      ? 'border-primary'
                      : 'border-foreground/40'
                  ">
                  @if (isSelected(preference, option)) {
                    <span class="bg-primary h-2 w-2 rounded-full"></span>
                  }
                </span>

                <span
                  class="text-sm"
                  [class]="
                    isSelected(preference, option)
                      ? 'font-semibold'
                      : 'font-medium'
                  ">
                  {{ option.label }}
                </span>

                @if (isSelected(preference, option)) {
                  <span
                    class="text-primary ml-auto text-[11px] font-semibold tracking-[.06em] uppercase">
                    {{ selectedBadge }}
                  </span>
                }
              </span>

              @if (caption(option); as optionCaption) {
                <span class="text-muted text-xs leading-[18px]">
                  {{ optionCaption }}
                </span>
              }
            </button>
          }
        </div>
      </div>
    }

    @if (remaining().length) {
      <app-preference-list [values]="remaining()" />
    }
  `,
})
export class AppearancePreferencesComponent {
  readonly values = input.required<ResolvedPreferenceValue[]>();

  private readonly userPreferences = inject(UserPreferencesService);
  private readonly selectedScopes = signal<PreferenceScopeSelection>({});
  private readonly pendingKey = signal<string | null>(null);
  private readonly optimisticValues = signal<Record<string, string>>({});

  protected readonly themeKey = APPEARANCE_THEME;
  protected readonly pageWidthKey = APPEARANCE_PAGE_WIDTH;
  protected readonly selectedBadge = selectedBadge;

  protected readonly tiled = computed(() =>
    this.values().filter(hasTilePreview)
  );

  protected readonly remaining = computed(() => {
    return this.values().filter((preference) => !hasTilePreview(preference));
  });

  protected scopeLabel(scope: PreferenceScope): string {
    return preferenceScopeLabel(scope);
  }

  protected clearLabel(preference: ResolvedPreferenceValue): string {
    return clearLabels[preference.definition.key] ?? resetLabel;
  }

  protected canClear(preference: ResolvedPreferenceValue): boolean {
    return preference.source !== 'default';
  }

  protected description(preference: ResolvedPreferenceValue): string {
    return descriptions[preference.definition.key] ?? '';
  }

  protected caption(option: PreferenceOption): string | null {
    return optionCaptions[option.value] ?? null;
  }

  protected selectedScope(
    preference: ResolvedPreferenceValue
  ): PreferenceScope {
    return selectedScopeFor(preference, this.selectedScopes());
  }

  protected isPending(preference: ResolvedPreferenceValue): boolean {
    return this.pendingKey() === preference.definition.key;
  }

  protected isSelected(
    preference: ResolvedPreferenceValue,
    option: PreferenceOption
  ): boolean {
    return this.currentValue(preference) === option.value;
  }

  /** Roving tabindex, falling back to the first tile when nothing matches. */
  protected tabIndex(
    preference: ResolvedPreferenceValue,
    option: PreferenceOption
  ): number {
    const selected = this.isSelected(preference, option);

    if (selected) return 0;

    const options = preference.definition.options;
    const hasSelection = options.some((item) => {
      return this.isSelected(preference, item);
    });

    return !hasSelection && options[0] === option ? 0 : -1;
  }

  protected tileClass(
    preference: ResolvedPreferenceValue,
    option: PreferenceOption
  ): string {
    const selected = this.isSelected(preference, option);

    return cn(
      'flex cursor-pointer flex-col gap-2.5 rounded-[10px] border-2 p-2.5 text-left transition-colors duration-150',
      'focus-visible:ring-primary focus-visible:ring-2 focus-visible:ring-offset-2 focus-visible:outline-none',
      '@max-[600px]:min-w-0 @max-[600px]:flex-1 @max-[600px]:basis-[240px]',
      tileWidths[preference.definition.key],
      selected
        ? 'border-primary bg-primary/6'
        : 'border-border hover:border-primary/50 bg-transparent'
    );
  }

  protected selectScope(
    preference: ResolvedPreferenceValue,
    scope: PreferenceScope
  ) {
    this.selectedScopes.update((selected) => ({
      ...selected,
      [preference.definition.key]: scope,
    }));
  }

  protected selectOption(
    preference: ResolvedPreferenceValue,
    option: PreferenceOption
  ) {
    const writeInFlight = this.pendingKey() !== null;
    const alreadySelected = this.isSelected(preference, option);

    if (writeInFlight || alreadySelected) return;

    const key = preference.definition.key;

    this.pendingKey.set(key);
    this.optimisticValues.update((values) => ({
      ...values,
      [key]: option.value,
    }));

    this.userPreferences
      .updateValue(key, this.selectedScope(preference), option.value)
      .subscribe({
        next: () => this.settle(key),
        error: () => this.settle(key),
      });
  }

  protected clearValue(preference: ResolvedPreferenceValue) {
    const writeInFlight = this.pendingKey() !== null;

    if (writeInFlight) return;

    const key = preference.definition.key;

    this.pendingKey.set(key);

    this.userPreferences
      .deleteValue(key, this.selectedScope(preference))
      .subscribe({
        next: () => this.settle(key),
        error: () => this.settle(key),
      });
  }

  protected moveFocus(event: KeyboardEvent) {
    const isArrowKey = arrowKeys.includes(event.key);

    if (!isArrowKey) return;

    const group = event.currentTarget as HTMLElement;
    const tiles = Array.from(
      group.querySelectorAll<HTMLButtonElement>('[role="radio"]')
    );
    const current = tiles.indexOf(document.activeElement as HTMLButtonElement);

    if (current === -1) return;

    const forward = event.key === 'ArrowRight' || event.key === 'ArrowDown';
    const next = (current + (forward ? 1 : -1) + tiles.length) % tiles.length;

    event.preventDefault();
    tiles[next].focus();
  }

  private currentValue(preference: ResolvedPreferenceValue): string {
    const optimistic = this.optimisticValues()[preference.definition.key];

    if (optimistic) return optimistic;

    const value = valueForScope(preference, this.selectedScope(preference));

    return typeof value === 'string' ? value : '';
  }

  private settle(key: string) {
    this.pendingKey.set(null);
    this.optimisticValues.update((values) => {
      const { [key]: _removed, ...rest } = values;

      return rest;
    });
  }
}

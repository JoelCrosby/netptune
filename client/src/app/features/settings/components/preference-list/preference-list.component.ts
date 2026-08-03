import { Component, inject, input, signal } from '@angular/core';
import { StrokedButtonComponent } from '@app/static/components/button/stroked-button.component';
import {
  COMMAND_PALETTE_RECENT_ITEMS_SCOPE,
  PreferenceScope,
  ResolvedPreferenceValue,
} from '@core/models/user-preferences';
import { UserPreferencesService } from '@core/services/user-preferences.service';
import { FormSelectOptionComponent } from '@static/components/form-select/form-select-option.component';
import { FormSelectComponent } from '@static/components/form-select/form-select.component';
import { RecentItemsService } from '../../../../shell/command-palette/recent-items.service';

@Component({
  selector: 'app-preference-list',
  imports: [
    FormSelectComponent,
    FormSelectOptionComponent,
    StrokedButtonComponent,
  ],
  host: { class: 'block' },
  template: `
    <ul class="divide-border/50 flex flex-col divide-y">
      @for (preference of values(); track preference.definition.key) {
        <li class="flex flex-wrap items-end gap-3 px-6 py-4">
          <app-form-select
            class="block max-w-86 min-w-56 flex-1"
            [noMargin]="true"
            [label]="preference.definition.label"
            i18n-placeholder="Placeholder in a preference value picker"
            placeholder="Select value"
            [value]="currentValue(preference)"
            (changed)="updateValue(preference, $event)">
            @for (option of preference.definition.options; track option.value) {
              <app-form-select-option [value]="option.value">
                {{ option.label }}
              </app-form-select-option>
            }
          </app-form-select>

          <app-form-select
            class="block w-46"
            [noMargin]="true"
            i18n-label="Label of the preference scope field"
            label="Scope"
            i18n-placeholder="Placeholder in the preference scope picker"
            placeholder="Select scope"
            [value]="selectedScope(preference)"
            (changed)="selectScope(preference, $event)">
            @for (scope of preference.definition.allowedScopes; track scope) {
              <app-form-select-option [value]="scope">
                {{ scopeLabel(scope) }}
              </app-form-select-option>
            }
          </app-form-select>

          <button
            app-stroked-button
            type="button"
            class="h-10 shrink-0 px-4"
            (click)="clearValue(preference)">
            <span i18n="Button that removes a preference override">Clear</span>
          </button>
        </li>
      }
    </ul>
  `,
})
export class PreferenceListComponent {
  /** Ternaries in a template expression cannot be marked, so build the copy here. */
  protected scopeLabel(scope: string): string {
    return scope === 'workspace'
      ? $localize`:Preference scope limited to the current workspace:Workspace`
      : $localize`:Preference scope applying everywhere:Global`;
  }
  readonly values = input.required<ResolvedPreferenceValue[]>();

  private readonly userPreferences = inject(UserPreferencesService);
  private readonly recentItems = inject(RecentItemsService);
  private readonly selectedScopes = signal<Record<string, PreferenceScope>>({});

  protected selectedScope(
    preference: ResolvedPreferenceValue
  ): PreferenceScope {
    const key = preference.definition.key;
    const selected = this.selectedScopes()[key];

    if (selected) return selected;
    if (preference.source === 'workspace') return 'workspace';
    if (preference.definition.allowedScopes.includes('global')) return 'global';

    return preference.definition.allowedScopes[0];
  }

  protected currentValue(preference: ResolvedPreferenceValue): string {
    const value = this.valueForSelectedScope(preference);

    return typeof value === 'string' ? value : '';
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

  protected updateValue(preference: ResolvedPreferenceValue, value: string) {
    this.userPreferences
      .updateValue(
        preference.definition.key,
        this.selectedScope(preference),
        value
      )
      .subscribe(() => this.invalidateDependentClientState(preference));
  }

  protected clearValue(preference: ResolvedPreferenceValue) {
    this.userPreferences
      .deleteValue(preference.definition.key, this.selectedScope(preference))
      .subscribe(() => this.invalidateDependentClientState(preference));
  }

  private invalidateDependentClientState(preference: ResolvedPreferenceValue) {
    if (preference.definition.key === COMMAND_PALETTE_RECENT_ITEMS_SCOPE) {
      this.recentItems.invalidate();
    }
  }

  private valueForSelectedScope(preference: ResolvedPreferenceValue): unknown {
    return this.selectedScope(preference) === 'workspace'
      ? (preference.workspaceValue ?? preference.effectiveValue)
      : (preference.globalValue ?? preference.effectiveValue);
  }
}

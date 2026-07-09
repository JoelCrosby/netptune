import { Component, computed, inject, signal } from '@angular/core';
import { FlatButtonComponent } from '@app/static/components/button/flat-button.component';
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
  selector: 'app-settings',
  template: `
    <h3 class="font-overpass mb-4 text-[1.4rem] font-normal">
      User Preferences
    </h3>

    @for (group of visibleGroups(); track group.key) {
      <section class="mt-6">
        <h4 class="font-overpass mb-3 text-[1.1rem] font-normal">
          {{ group.label }}
        </h4>

        <div class="grid gap-4">
          @for (
            preference of group.preferences;
            track preference.definition.key
          ) {
            <div class="flex items-end gap-3 rounded-md py-4">
              @if (preference.definition.controlType === 'select') {
                <app-form-select
                  class="block max-w-86"
                  [label]="preference.definition.label"
                  placeholder="Select value"
                  [value]="currentValue(preference)"
                  (changed)="onPreferenceValueSelect(preference, $event)">
                  @for (
                    option of preference.definition.options;
                    track option.value
                  ) {
                    <app-form-select-option [value]="option.value">
                      {{ option.label }}
                    </app-form-select-option>
                  }
                </app-form-select>
              }

              <app-form-select
                class="block w-46"
                label="Scope"
                placeholder="Select scope"
                [value]="selectedScope(preference)"
                (changed)="onPreferenceScopeSelect(preference, $event)">
                @for (
                  scope of preference.definition.allowedScopes;
                  track scope
                ) {
                  <app-form-select-option [value]="scope">
                    {{ scope === 'workspace' ? 'Workspace' : 'Global' }}
                  </app-form-select-option>
                }
              </app-form-select>

              <div>
                <button
                  app-flat-button
                  type="button"
                  color="contrast"
                  class="mb-6 h-10 px-4"
                  (click)="clearPreference(preference)">
                  Clear
                </button>
              </div>
            </div>
          }
        </div>
      </section>
    }
  `,
  imports: [
    FlatButtonComponent,
    FormSelectComponent,
    FormSelectOptionComponent,
  ],
})
export class SettingsComponent {
  protected preferences = inject(UserPreferencesService);
  private recentItems = inject(RecentItemsService);

  private selectedScopes = signal<Record<string, PreferenceScope>>({});

  // Internal preferences are managed by dedicated UI, not the settings screen.
  protected visibleGroups = computed(() =>
    this.preferences
      .groups()
      .map((group) => ({
        ...group,
        preferences: group.preferences.filter(
          (preference) => !preference.definition.internal
        ),
      }))
      .filter((group) => group.preferences.length > 0)
  );

  constructor() {
    this.preferences.load();
  }

  selectedScope(preference: ResolvedPreferenceValue): PreferenceScope {
    const key = preference.definition.key;
    const selected = this.selectedScopes()[key];

    if (selected) return selected;
    if (preference.source === 'workspace') return 'workspace';
    if (preference.definition.allowedScopes.includes('global')) return 'global';

    return preference.definition.allowedScopes[0];
  }

  currentValue(preference: ResolvedPreferenceValue): string {
    const value =
      this.selectedScope(preference) === 'workspace'
        ? (preference.workspaceValue ?? preference.effectiveValue)
        : (preference.globalValue ?? preference.effectiveValue);

    return typeof value === 'string' ? value : '';
  }

  onPreferenceScopeSelect(preference: ResolvedPreferenceValue, scope: string) {
    this.selectedScopes.update((selected) => ({
      ...selected,
      [preference.definition.key]: scope as PreferenceScope,
    }));
  }

  onPreferenceValueSelect(preference: ResolvedPreferenceValue, value: string) {
    this.preferences
      .updateValue(
        preference.definition.key,
        this.selectedScope(preference),
        value
      )
      .subscribe(() => this.invalidateDependentClientState(preference));
  }

  clearPreference(preference: ResolvedPreferenceValue) {
    this.preferences
      .deleteValue(preference.definition.key, this.selectedScope(preference))
      .subscribe(() => this.invalidateDependentClientState(preference));
  }

  private invalidateDependentClientState(preference: ResolvedPreferenceValue) {
    if (preference.definition.key === COMMAND_PALETTE_RECENT_ITEMS_SCOPE) {
      this.recentItems.invalidate();
    }
  }
}

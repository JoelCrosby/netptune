import { Component, computed, inject, input, signal } from '@angular/core';
import { StrokedButtonComponent } from '@app/static/components/button/stroked-button.component';
import {
  COMMAND_PALETTE_RECENT_ITEMS_SCOPE,
  PreferenceScope,
  ResolvedPreferenceValue,
} from '@core/models/user-preferences';
import { UserPreferencesService } from '@core/services/user-preferences.service';
import {
  SelectMenuComponent,
  SelectMenuOption,
} from '@static/components/select-menu/select-menu.component';
import { RecentItemsService } from '../../../../shell/command-palette/recent-items.service';
import {
  preferenceScopeLabel,
  PreferenceScopeSelection,
  selectedScopeFor,
  valueForScope,
} from '../preference-scope';

const scopeFieldLabel = $localize`:Label of the preference scope field:Scope`;

interface PreferenceRow {
  preference: ResolvedPreferenceValue;
  label: string;
  valueOptions: readonly SelectMenuOption<string>[];
  scopeOptions: readonly SelectMenuOption<PreferenceScope>[];
  value: string;
  scope: PreferenceScope;
}

@Component({
  selector: 'app-preference-list',
  imports: [SelectMenuComponent, StrokedButtonComponent],
  host: { class: 'block' },
  template: `
    <ul class="divide-border/50 flex flex-col divide-y">
      @for (row of rows(); track row.preference.definition.key) {
        <li class="flex flex-wrap items-end gap-3 px-6 py-4">
          <div class="flex min-w-0 flex-col gap-1">
            <span class="text-muted text-xs">{{ row.label }}</span>
            <app-select-menu
              [options]="row.valueOptions"
              [value]="row.value"
              [ariaLabel]="row.label"
              buttonClass="h-9 min-w-56 justify-between"
              (valueChange)="updateValue(row.preference, $event)" />
          </div>

          @if (row.scopeOptions.length > 1) {
            <div class="flex flex-col gap-1">
              <span class="text-muted text-xs">{{ scopeFieldLabel }}</span>
              <app-select-menu
                [options]="row.scopeOptions"
                [value]="row.scope"
                [ariaLabel]="scopeFieldLabel"
                buttonClass="h-9 w-46 justify-between"
                (valueChange)="selectScope(row.preference, $event)" />
            </div>
          }

          <button
            app-stroked-button
            type="button"
            class="h-9 shrink-0 px-4"
            (click)="clearValue(row.preference)">
            <span i18n="Button that removes a preference override">Clear</span>
          </button>
        </li>
      }
    </ul>
  `,
})
export class PreferenceListComponent {
  readonly values = input.required<ResolvedPreferenceValue[]>();

  protected readonly scopeFieldLabel = scopeFieldLabel;

  private readonly userPreferences = inject(UserPreferencesService);
  private readonly recentItems = inject(RecentItemsService);
  private readonly selectedScopes = signal<PreferenceScopeSelection>({});

  protected readonly rows = computed<PreferenceRow[]>(() => {
    const scopes = this.selectedScopes();

    return this.values().map((preference) => {
      const scope = selectedScopeFor(preference, scopes);
      const value = valueForScope(preference, scope);

      return {
        preference,
        label: preference.definition.label,
        valueOptions: preference.definition.options,
        scopeOptions: preference.definition.allowedScopes.map((allowed) => {
          return { value: allowed, label: preferenceScopeLabel(allowed) };
        }),
        value: typeof value === 'string' ? value : '',
        scope,
      };
    });
  });

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

  private selectedScope(preference: ResolvedPreferenceValue): PreferenceScope {
    return selectedScopeFor(preference, this.selectedScopes());
  }

  private invalidateDependentClientState(preference: ResolvedPreferenceValue) {
    if (preference.definition.key === COMMAND_PALETTE_RECENT_ITEMS_SCOPE) {
      this.recentItems.invalidate();
    }
  }
}

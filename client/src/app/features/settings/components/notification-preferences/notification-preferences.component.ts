import { Component, computed, inject, input, signal } from '@angular/core';
import {
  PreferenceScope,
  ResolvedPreferenceValue,
} from '@core/models/user-preferences';
import { UserPreferencesService } from '@core/services/user-preferences.service';
import { LucideBell } from '@lucide/angular';
import { PanelComponent } from '@static/components/panel.component';
import { PanelHeaderComponent } from '@static/components/panel-header.component';
import {
  SegmentedControlComponent,
  SegmentedOption,
} from '@static/components/segmented-control/segmented-control.component';
import { SettingRowComponent } from '@static/components/setting-row/setting-row.component';
import { SwitchComponent } from '@static/components/switch/switch.component';

interface NotificationRow {
  key: string;
  label: string;
  hint: string;
  switchLabel: string;
  enabled: boolean;
  isOverridden: boolean;
  preference: ResolvedPreferenceValue;
}

const SCOPE_OPTIONS: SegmentedOption<PreferenceScope>[] = [
  {
    value: 'global',
    label: $localize`:Preference scope applying to every workspace:Everywhere`,
  },
  {
    value: 'workspace',
    label: $localize`:Preference scope limited to the current workspace:This workspace`,
  },
];

@Component({
  selector: 'app-notification-preferences',
  imports: [
    PanelComponent,
    PanelHeaderComponent,
    SegmentedControlComponent,
    SettingRowComponent,
    SwitchComponent,
  ],
  template: `
    <app-panel>
      <app-panel-header
        [icon]="headingIcon"
        i18n-heading="Heading above the notification toggles"
        heading="Notify me about"
        i18n-description="Explains which scope the toggles below are editing"
        description="Pick where these choices apply.">
        <app-segmented-control
          panelHeaderActions
          [options]="scopeOptions"
          [(value)]="scope"
          i18n-ariaLabel="
            Accessible label for the control that picks the preference scope
          "
          ariaLabel="Preference scope" />
      </app-panel-header>

      @for (row of rows(); track row.key) {
        <app-setting-row [label]="row.label" [hint]="row.hint">
          @if (row.isOverridden) {
            <button
              type="button"
              class="text-muted hover:text-foreground text-xs"
              (click)="clearValue(row.preference)">
              <span i18n="Button that removes a preference override">
                Reset
              </span>
            </button>
          }

          <app-switch
            [checked]="row.enabled"
            [ariaLabel]="row.switchLabel"
            (changed)="updateValue(row.preference, $event)" />
        </app-setting-row>
      } @empty {
        <p
          class="text-muted px-4 py-6 text-sm"
          i18n="Empty state for the notification preference list">
          There is nothing to configure yet.
        </p>
      }
    </app-panel>
  `,
})
export class NotificationPreferencesComponent {
  readonly values = input.required<ResolvedPreferenceValue[]>();

  private readonly userPreferences = inject(UserPreferencesService);

  protected readonly headingIcon = LucideBell;
  protected readonly scopeOptions = SCOPE_OPTIONS;
  protected readonly scope = signal<PreferenceScope>('global');

  protected readonly rows = computed<NotificationRow[]>(() => {
    const scope = this.scope();

    return this.values().map((preference) => {
      return this.toRow(preference, scope);
    });
  });

  protected updateValue(preference: ResolvedPreferenceValue, value: boolean) {
    this.userPreferences
      .updateValue(preference.definition.key, this.scopeFor(preference), value)
      .subscribe();
  }

  protected clearValue(preference: ResolvedPreferenceValue) {
    this.userPreferences
      .deleteValue(preference.definition.key, this.scopeFor(preference))
      .subscribe();
  }

  private toRow(
    preference: ResolvedPreferenceValue,
    scope: PreferenceScope
  ): NotificationRow {
    const value = this.valueFor(preference, scope);
    const label = preference.definition.label;

    return {
      key: preference.definition.key,
      label,
      hint: this.hint(preference, scope),
      switchLabel: $localize`:Accessible label for a notification toggle. EVENT is the already-localised event name:Receive ${label}:EVENT: notifications`,
      enabled: value === true,
      isOverridden: this.storedValue(preference, scope) !== null,
      preference,
    };
  }

  /** A preference that cannot be set per workspace stays on the scope it allows. */
  private scopeFor(preference: ResolvedPreferenceValue): PreferenceScope {
    const scope = this.scope();
    const isAllowed = preference.definition.allowedScopes.includes(scope);

    return isAllowed ? scope : preference.definition.allowedScopes[0];
  }

  private storedValue(
    preference: ResolvedPreferenceValue,
    scope: PreferenceScope
  ): unknown {
    const stored =
      scope === 'workspace'
        ? preference.workspaceValue
        : preference.globalValue;

    return stored ?? null;
  }

  private valueFor(
    preference: ResolvedPreferenceValue,
    scope: PreferenceScope
  ): unknown {
    return this.storedValue(preference, scope) ?? preference.effectiveValue;
  }

  private hint(
    preference: ResolvedPreferenceValue,
    scope: PreferenceScope
  ): string {
    const isSet = this.storedValue(preference, scope) !== null;

    if (scope === 'workspace') {
      if (isSet) {
        return $localize`:Shown when a notification is set for this workspace only:Set for this workspace`;
      }

      return $localize`:Shown when a workspace notification follows the global choice:Following your setting for everywhere`;
    }

    if (isSet) {
      return $localize`:Shown when a notification is set for every workspace:Set for every workspace`;
    }

    return $localize`:Shown when a notification has never been changed:Using the default`;
  }
}

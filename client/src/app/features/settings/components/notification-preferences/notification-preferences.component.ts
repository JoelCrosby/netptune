import { Component, computed, inject, input, signal } from '@angular/core';
import {
  PreferenceScope,
  ResolvedPreferenceValue,
} from '@core/models/user-preferences';
import { UserPreferencesService } from '@core/services/user-preferences.service';
import { LucideBell } from '@lucide/angular';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import { IconTileComponent } from '@static/components/icon-tile.component';
import {
  SegmentedControlComponent,
  SegmentedOption,
} from '@static/components/segmented-control/segmented-control.component';
import { SettingRowComponent } from '@static/components/setting-row/setting-row.component';
import { SkeletonComponent } from '@static/components/skeleton/skeleton.component';
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
    EmptyStateComponent,
    IconTileComponent,
    LucideBell,
    SegmentedControlComponent,
    SettingRowComponent,
    SkeletonComponent,
    SwitchComponent,
  ],
  host: { class: 'block' },
  template: `
    <section
      class="border-border bg-card overflow-hidden rounded-lg border shadow-sm">
      <header
        class="border-border flex flex-wrap items-center justify-between gap-x-4 gap-y-3 border-b px-6 py-5">
        <div class="flex min-w-0 items-center gap-3">
          <app-icon-tile [icon]="headingIcon" />

          <div class="min-w-0">
            <h2
              class="font-overpass text-base font-semibold"
              i18n="Heading above the notification toggles">
              Notify me about
            </h2>
            <p
              class="text-muted mt-1 text-sm"
              i18n="Explains which scope the toggles below are editing">
              Choose which events notify you, and where that choice applies.
            </p>
          </div>
        </div>

        <app-segmented-control
          class="shrink-0"
          [options]="scopeOptions"
          [(value)]="scope"
          i18n-ariaLabel="
            Accessible label for the control that picks the preference scope
          "
          ariaLabel="Preference scope" />
      </header>

      @if (isInitialLoad()) {
        <div
          class="flex flex-col gap-5 px-6 py-5"
          role="status"
          i18n-aria-label="Accessible label while notification preferences load"
          aria-label="Loading notification preferences">
          @for (row of skeletonRows; track $index) {
            <div class="flex items-center justify-between gap-4">
              <div class="flex-1">
                <app-skeleton class="h-3 w-40" />
                <app-skeleton class="mt-2 h-3 w-56" />
              </div>
              <app-skeleton class="h-5 w-9 shrink-0 rounded-full" />
            </div>
          }
        </div>
      } @else {
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
          <app-empty-state
            compact
            i18n-title="Empty state for the notification preference list"
            title="There is nothing to configure yet."
            i18n-description="
              Explains why the notification preference list is empty
            "
            description="Notification options appear here once they are available.">
            <svg emptyStateIcon lucideBell class="h-8 w-8"></svg>
          </app-empty-state>
        }
      }
    </section>
  `,
})
export class NotificationPreferencesComponent {
  readonly values = input.required<ResolvedPreferenceValue[]>();

  private readonly userPreferences = inject(UserPreferencesService);

  protected readonly headingIcon = LucideBell;
  protected readonly scopeOptions = SCOPE_OPTIONS;
  protected readonly scope = signal<PreferenceScope>('global');
  protected readonly skeletonRows = Array.from({ length: 5 });

  protected readonly isInitialLoad = computed(() => {
    return !this.userPreferences.loaded() && this.values().length === 0;
  });

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

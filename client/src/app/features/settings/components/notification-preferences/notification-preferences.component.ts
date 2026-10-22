import { Component, computed, inject, input, signal } from '@angular/core';
import {
  PreferenceScope,
  PreferenceSection,
  ResolvedPreferenceValue,
} from '@core/models/user-preferences';
import { UserPreferencesService } from '@core/services/user-preferences.service';
import { LucideBell } from '@lucide/angular';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import {
  GroupedToggleListComponent,
  ToggleListChange,
  ToggleListGroup,
  ToggleListRow,
} from '@static/components/grouped-toggle-list/grouped-toggle-list.component';
import {
  SegmentedControlComponent,
  SegmentedOption,
} from '@static/components/segmented-control/segmented-control.component';
import { SkeletonComponent } from '@static/components/skeleton/skeleton.component';
import { PanelComponent } from '@static/components/panel.component';
import { PanelHeaderComponent } from '@static/components/panel-header.component';
import { forkJoin } from 'rxjs';

type StateFilter = 'all' | 'on' | 'off' | 'changed';

interface PreferenceSectionGroup extends PreferenceSection {
  preferences: ResolvedPreferenceValue[];
}

const UNSECTIONED: PreferenceSection = {
  key: '',
  label: $localize`:Notification settings group for events without a group:Other`,
};

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

const FILTER_OPTIONS: SegmentedOption<StateFilter>[] = [
  {
    value: 'all',
    label: $localize`:Notification filter showing every event:All`,
  },
  {
    value: 'on',
    label: $localize`:Notification filter showing events that are turned on:On`,
  },
  {
    value: 'off',
    label: $localize`:Notification filter showing events that are turned off:Off`,
  },
  {
    value: 'changed',
    label: $localize`:Notification filter showing events changed from the default:Changed`,
  },
];

@Component({
  selector: 'app-notification-preferences',
  imports: [
    EmptyStateComponent,
    GroupedToggleListComponent,
    LucideBell,
    PanelComponent,
    PanelHeaderComponent,
    SegmentedControlComponent,
    SkeletonComponent,
  ],
  host: { class: 'block' },
  template: `
    <!-- overflow-clip rather than the panel's overflow-hidden, so the toolbar can stick -->
    <section app-panel surface="card" class="overflow-clip">
      <app-panel-header
        density="comfortable"
        [icon]="headingIcon"
        i18n-heading="Heading above the notification toggles"
        heading="Notify me about"
        i18n-description="Explains which scope the toggles below are editing"
        description="Choose which events notify you, and where that choice applies.">
        <app-segmented-control
          panelHeaderActions
          class="shrink-0"
          [options]="scopeOptions"
          [(value)]="scope"
          i18n-ariaLabel="
            Accessible label for the control that picks the preference scope
          "
          ariaLabel="Preference scope" />
      </app-panel-header>

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
      } @else if (values().length === 0) {
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
      } @else {
        <app-grouped-toggle-list
          [groups]="groups()"
          [filterOptions]="filterOptions"
          [filtering]="isFiltering()"
          [(query)]="query"
          [(filter)]="filter"
          i18n-searchPlaceholder="
            Placeholder in the notification event search box
          "
          searchPlaceholder="Search events"
          i18n-filterAriaLabel="
            Accessible label for the control that filters notification events by
            state
          "
          filterAriaLabel="Show events"
          i18n-emptyTitle="
            Empty state when no notification events match the search
          "
          emptyTitle="No events match"
          (rowChanged)="updateValue($event)"
          (rowReset)="clearValue($event)"
          (groupAction)="flipGroup($event)"
          (cleared)="resetFilters()" />
      }
    </section>
  `,
})
export class NotificationPreferencesComponent {
  readonly values = input.required<ResolvedPreferenceValue[]>();

  private readonly userPreferences = inject(UserPreferencesService);

  protected readonly headingIcon = LucideBell;
  protected readonly scopeOptions = SCOPE_OPTIONS;
  protected readonly filterOptions = FILTER_OPTIONS;
  protected readonly skeletonRows = Array.from({ length: 5 });

  protected readonly scope = signal<PreferenceScope>('global');
  protected readonly query = signal('');
  protected readonly filter = signal<StateFilter>('all');

  protected readonly isInitialLoad = computed(() => {
    return !this.userPreferences.loaded() && this.values().length === 0;
  });

  protected readonly isFiltering = computed(() => {
    return this.query().trim().length > 0 || this.filter() !== 'all';
  });

  // Read by the page header, which shows the totals beside the page title.
  readonly summary = computed(() => {
    const scope = this.scope();
    const values = this.values();
    const total = values.length;
    const enabled = values.filter((preference) => {
      return this.valueFor(preference, scope) === true;
    }).length;

    const changed = values.filter((preference) => {
      return preference.globalValue !== null;
    }).length;

    if (scope === 'global' && changed > 0) {
      return $localize`:Summary of notification events. ENABLED and TOTAL are counts, CHANGED is how many differ from the default:${enabled}:ENABLED: of ${total}:TOTAL: events on · ${changed}:CHANGED: changed from default`;
    }

    return $localize`:Summary of notification events. ENABLED and TOTAL are counts:${enabled}:ENABLED: of ${total}:TOTAL: events on`;
  });

  private readonly groupedPreferences = computed(() => {
    const groups = new Map<string, PreferenceSectionGroup>();

    for (const preference of this.values()) {
      const section = preference.definition.section ?? UNSECTIONED;
      const group = groups.get(section.key) ?? { ...section, preferences: [] };

      group.preferences.push(preference);
      groups.set(section.key, group);
    }

    return groups;
  });

  private readonly preferencesByKey = computed(() => {
    return new Map(
      this.values().map((preference) => [preference.definition.key, preference])
    );
  });

  protected readonly groups = computed<ToggleListGroup[]>(() => {
    const scope = this.scope();
    const query = this.query().trim().toLowerCase();
    const filter = this.filter();
    const isFiltering = this.isFiltering();

    return [...this.groupedPreferences().values()]
      .map((group) => {
        const groupMatches = group.label.toLowerCase().includes(query);
        const rows = group.preferences
          .map((preference) => this.toRow(preference, scope))
          .filter((row) => {
            const matchesQuery =
              !query || groupMatches || row.label.toLowerCase().includes(query);

            return matchesQuery && matchesFilter(row, filter);
          });

        const enabledCount = this.enabledCount(group, scope);
        const total = group.preferences.length;
        const shown = rows.length;
        const isAllOn = enabledCount === total;

        return {
          key: group.key,
          heading: group.label,
          summary: isFiltering
            ? $localize`:How many events in a notification group match the filter:${shown}:SHOWN: of ${total}:TOTAL: shown`
            : $localize`:How many events in a notification group are turned on:${enabledCount}:ENABLED: of ${total}:TOTAL: on`,
          actionLabel: isAllOn
            ? $localize`:Button that turns off every event in a notification group:Mute all`
            : $localize`:Button that turns on every event in a notification group:Turn all on`,
          rows,
        };
      })
      .filter((group) => group.rows.length > 0);
  });

  protected resetFilters() {
    this.query.set('');
    this.filter.set('all');
  }

  protected flipGroup(key: string) {
    const group = this.groupedPreferences().get(key);

    if (!group) return;

    const scope = this.scope();
    const value = this.enabledCount(group, scope) !== group.preferences.length;
    const updates = group.preferences
      .filter((preference) => this.valueFor(preference, scope) !== value)
      .map((preference) => {
        return this.userPreferences.updateValue(
          preference.definition.key,
          this.scopeFor(preference),
          value
        );
      });

    if (!updates.length) return;

    forkJoin(updates).subscribe();
  }

  protected updateValue(change: ToggleListChange) {
    const preference = this.preferencesByKey().get(change.key);

    if (!preference) return;

    this.userPreferences
      .updateValue(change.key, this.scopeFor(preference), change.checked)
      .subscribe();
  }

  protected clearValue(key: string) {
    const preference = this.preferencesByKey().get(key);

    if (!preference) return;

    this.userPreferences
      .deleteValue(key, this.scopeFor(preference))
      .subscribe();
  }

  private enabledCount(
    group: PreferenceSectionGroup,
    scope: PreferenceScope
  ): number {
    return group.preferences.filter((preference) => {
      return this.valueFor(preference, scope) === true;
    }).length;
  }

  private toRow(
    preference: ResolvedPreferenceValue,
    scope: PreferenceScope
  ): ToggleListRow {
    const label = preference.definition.label;
    const isOverridden = this.storedValue(preference, scope) !== null;

    return {
      key: preference.definition.key,
      label,
      checked: this.valueFor(preference, scope) === true,
      ariaLabel: $localize`:Accessible label for a notification toggle. EVENT is the already-localised event name:Receive ${label}:EVENT: notifications`,
      hint: this.hint(preference, scope),
      isHintHighlighted: isOverridden,
      isResettable: isOverridden,
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

function matchesFilter(row: ToggleListRow, filter: StateFilter): boolean {
  switch (filter) {
    case 'all':
      return true;
    case 'on':
      return row.checked;
    case 'off':
      return !row.checked;
    case 'changed':
      return row.isResettable === true;
  }
}

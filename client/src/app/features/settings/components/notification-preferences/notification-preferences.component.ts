import { Component, computed, inject, input, signal } from '@angular/core';
import {
  PreferenceScope,
  PreferenceSection,
  ResolvedPreferenceValue,
} from '@core/models/user-preferences';
import { UserPreferencesService } from '@core/services/user-preferences.service';
import {
  LucideBell,
  LucideChevronRight,
  LucideChevronsUpDown,
  LucideSearch,
} from '@lucide/angular';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import { FilterInputComponent } from '@static/components/filter-input/filter-input.component';
import {
  SegmentedControlComponent,
  SegmentedOption,
} from '@static/components/segmented-control/segmented-control.component';
import { SkeletonComponent } from '@static/components/skeleton/skeleton.component';
import { SwitchComponent } from '@static/components/switch/switch.component';
import { PanelComponent } from '@static/components/panel.component';
import { PanelHeaderComponent } from '@static/components/panel-header.component';
import { InlineButtonComponent } from '@static/components/button/inline-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { forkJoin } from 'rxjs';

type StateFilter = 'all' | 'on' | 'off' | 'changed';

interface PreferenceSectionGroup extends PreferenceSection {
  preferences: ResolvedPreferenceValue[];
}

const UNSECTIONED: PreferenceSection = {
  key: '',
  label: $localize`:Notification settings group for events without a group:Other`,
};

interface NotificationRow {
  key: string;
  label: string;
  hint: string;
  switchLabel: string;
  enabled: boolean;
  isOverridden: boolean;
  preference: ResolvedPreferenceValue;
}

interface NotificationGroup {
  key: string;
  label: string;
  summary: string;
  isOpen: boolean;
  isAllOn: boolean;
  flipLabel: string;
  rows: NotificationRow[];
  preferences: ResolvedPreferenceValue[];
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
    FilterInputComponent,
    InlineButtonComponent,
    LucideBell,
    LucideChevronRight,
    LucideChevronsUpDown,
    LucideSearch,
    PanelComponent,
    PanelHeaderComponent,
    SegmentedControlComponent,
    SkeletonComponent,
    StrokedButtonComponent,
    SwitchComponent,
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
        <div
          class="border-border bg-card sticky top-0 z-10 flex flex-wrap items-center gap-2.5 border-b px-6 py-3">
          <app-filter-input
            class="min-w-48 flex-1"
            [(value)]="query"
            i18n-placeholder="Placeholder in the notification event search box"
            placeholder="Search events" />

          <app-segmented-control
            class="shrink-0"
            [options]="filterOptions"
            [(value)]="filter"
            i18n-ariaLabel="
              Accessible label for the control that filters notification events
              by state
            "
            ariaLabel="Show events" />

          <button
            app-stroked-button
            type="button"
            color="neutral"
            size="small"
            class="shrink-0"
            [disabled]="isFiltering()"
            (click)="toggleAllGroups()">
            <svg
              lucideChevronsUpDown
              class="h-3.5 w-3.5"
              aria-hidden="true"></svg>
            @if (isAnyGroupOpen()) {
              <span i18n="Button that collapses every notification group">
                Collapse all
              </span>
            } @else {
              <span i18n="Button that expands every notification group">
                Expand all
              </span>
            }
          </button>
        </div>

        @for (group of groups(); track group.key) {
          <div class="border-border border-b last:border-b-0">
            <div class="flex items-center gap-3 pr-6 pl-4">
              <button
                type="button"
                class="text-foreground focus-visible:ring-primary flex min-w-0 flex-1 cursor-pointer items-center gap-3 rounded-md px-2 py-3 text-left outline-none focus-visible:ring-2"
                [attr.aria-expanded]="group.isOpen"
                [disabled]="isFiltering()"
                (click)="toggleGroup(group.key)">
                <svg
                  lucideChevronRight
                  aria-hidden="true"
                  class="text-muted h-3.5 w-3.5 shrink-0 transition-transform duration-150"
                  [class.rotate-90]="group.isOpen"></svg>
                <span class="text-sm font-semibold">{{ group.label }}</span>
                <span class="text-muted text-xs">{{ group.summary }}</span>
              </button>

              <button
                type="button"
                app-inline-button
                color="muted"
                appearance="soft"
                class="border-border shrink-0 rounded-full border font-semibold"
                (click)="flipGroup(group)">
                {{ group.flipLabel }}
              </button>
            </div>

            @if (group.isOpen) {
              <div class="flex flex-col px-6 pb-2.5">
                @for (row of group.rows; track row.key) {
                  <div
                    class="hover:bg-hover flex items-center gap-4 rounded-lg py-2 pr-2 pl-8.5">
                    <div class="min-w-0 flex-1">
                      <p class="truncate text-sm font-medium">
                        {{ row.label }}
                      </p>
                      <p
                        class="text-xs"
                        [class.text-primary]="row.isOverridden"
                        [class.text-muted]="!row.isOverridden">
                        {{ row.hint }}
                      </p>
                    </div>

                    @if (row.isOverridden) {
                      <button
                        type="button"
                        app-inline-button
                        color="muted"
                        appearance="soft"
                        class="shrink-0"
                        (click)="clearValue(row.preference)">
                        <span i18n="Button that removes a preference override">
                          Reset
                        </span>
                      </button>
                    }

                    <app-switch
                      class="shrink-0"
                      [checked]="row.enabled"
                      [ariaLabel]="row.switchLabel"
                      (changed)="updateValue(row.preference, $event)" />
                  </div>
                }
              </div>
            }
          </div>
        } @empty {
          <app-empty-state
            compact
            i18n-title="
              Empty state when no notification events match the search
            "
            title="No events match"
            i18n-description="
              Suggests how to find notification events after a search found
              nothing
            "
            description="Try a shorter word, or clear the filter.">
            <svg emptyStateIcon lucideSearch class="h-8 w-8"></svg>
            <button
              emptyStateAction
              app-stroked-button
              type="button"
              color="neutral"
              size="small"
              (click)="resetFilters()"
              i18n="
                Button that clears the notification search and state filter
              ">
              Clear search and filter
            </button>
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
  protected readonly filterOptions = FILTER_OPTIONS;
  protected readonly skeletonRows = Array.from({ length: 5 });

  protected readonly scope = signal<PreferenceScope>('global');
  protected readonly query = signal('');
  protected readonly filter = signal<StateFilter>('all');
  // Groups start collapsed; this holds the ones the user has opened.
  private readonly expanded = signal<ReadonlySet<string>>(new Set());

  protected readonly isInitialLoad = computed(() => {
    return !this.userPreferences.loaded() && this.values().length === 0;
  });

  protected readonly isFiltering = computed(() => {
    return this.query().trim().length > 0 || this.filter() !== 'all';
  });

  private readonly allGroupKeys = computed(() => {
    return this.groupedPreferences().map((group) => group.key);
  });

  protected readonly isAnyGroupOpen = computed(() => {
    const expanded = this.expanded();

    return this.allGroupKeys().some((key) => expanded.has(key));
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

    return [...groups.values()];
  });

  protected readonly groups = computed<NotificationGroup[]>(() => {
    const scope = this.scope();
    const query = this.query().trim().toLowerCase();
    const filter = this.filter();
    const isFiltering = this.isFiltering();
    const expanded = this.expanded();

    return this.groupedPreferences()
      .map((group) => {
        const groupMatches = group.label.toLowerCase().includes(query);
        const rows = group.preferences
          .map((preference) => this.toRow(preference, scope))
          .filter((row) => {
            const matchesQuery =
              !query || groupMatches || row.label.toLowerCase().includes(query);

            return matchesQuery && matchesFilter(row, filter);
          });

        const enabledCount = group.preferences.filter((preference) => {
          return this.valueFor(preference, scope) === true;
        }).length;

        const total = group.preferences.length;
        const shown = rows.length;
        const isAllOn = enabledCount === total;

        return {
          key: group.key,
          label: group.label,
          summary: isFiltering
            ? $localize`:How many events in a notification group match the filter:${shown}:SHOWN: of ${total}:TOTAL: shown`
            : $localize`:How many events in a notification group are turned on:${enabledCount}:ENABLED: of ${total}:TOTAL: on`,
          isOpen: isFiltering || expanded.has(group.key),
          isAllOn,
          flipLabel: isAllOn
            ? $localize`:Button that turns off every event in a notification group:Mute all`
            : $localize`:Button that turns on every event in a notification group:Turn all on`,
          rows,
          preferences: group.preferences,
        };
      })
      .filter((group) => group.rows.length > 0);
  });

  protected toggleGroup(key: string) {
    this.expanded.update((current) => {
      const next = new Set(current);

      if (next.has(key)) {
        next.delete(key);
      } else {
        next.add(key);
      }

      return next;
    });
  }

  protected toggleAllGroups() {
    const next = this.isAnyGroupOpen() ? [] : this.allGroupKeys();

    this.expanded.set(new Set(next));
  }

  protected resetFilters() {
    this.query.set('');
    this.filter.set('all');
  }

  protected flipGroup(group: NotificationGroup) {
    const scope = this.scope();
    const value = !group.isAllOn;
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

function matchesFilter(row: NotificationRow, filter: StateFilter): boolean {
  switch (filter) {
    case 'all':
      return true;
    case 'on':
      return row.enabled;
    case 'off':
      return !row.enabled;
    case 'changed':
      return row.isOverridden;
  }
}

import { Component, inject, input, signal } from '@angular/core';
import { FlatButtonComponent } from '@app/static/components/button/flat-button.component';
import { CheckboxComponent } from '@app/static/components/checkbox/checkbox.component';
import {
  PreferenceScope,
  ResolvedPreferenceValue,
} from '@core/models/user-preferences';
import { UserPreferencesService } from '@core/services/user-preferences.service';
import { LucideCheck } from '@lucide/angular';
import { DropdownButtonComponent } from '@static/components/dropdown-menu/dropdown-button.component';
import { MenuItemComponent } from '@static/components/dropdown-menu/menu-item.component';

@Component({
  selector: 'app-notification-preferences',
  imports: [
    CheckboxComponent,
    DropdownButtonComponent,
    FlatButtonComponent,
    LucideCheck,
    MenuItemComponent,
  ],
  template: `
    <div class="border-border max-h-96 overflow-auto rounded-md border">
      <table class="w-full min-w-132 border-collapse text-left">
        <thead
          class="bg-background text-muted sticky top-0 z-10 text-sm shadow-sm">
          <tr>
            <th class="px-3 py-2 font-medium" scope="col">Event</th>
            <th class="w-24 px-3 py-2 font-medium" scope="col">Enabled</th>
            <th class="w-40 px-3 py-2 font-medium" scope="col">Scope</th>
            <th class="w-24 px-3 py-2 font-medium" scope="col">Override</th>
          </tr>
        </thead>
        <tbody class="divide-border divide-y">
          @for (preference of values(); track preference.definition.key) {
            <tr class="hover:bg-foreground/3 text-sm">
              <th class="px-3 py-1.5 font-normal" scope="row">
                {{ preference.definition.label }}
              </th>
              <td class="px-3 py-1.5">
                <app-checkbox
                  [checked]="currentValue(preference)"
                  (changed)="updateValue(preference, $event)">
                  <span class="sr-only">
                    Receive {{ preference.definition.label }} notifications
                  </span>
                </app-checkbox>
              </td>
              <td class="px-3 py-1.5">
                <app-dropdown-button
                  #scopeMenu
                  [label]="scopeLabel(preference)"
                  [ariaLabel]="
                    'Notification scope for ' + preference.definition.label
                  "
                  buttonClass="h-7 min-w-32 justify-between px-2 text-xs">
                  @for (
                    scope of preference.definition.allowedScopes;
                    track scope
                  ) {
                    <button
                      app-menu-item
                      type="button"
                      role="menuitemradio"
                      class="min-w-36"
                      [attr.aria-checked]="selectedScope(preference) === scope"
                      (click)="
                        selectScope(preference, scope); scopeMenu.close()
                      ">
                      <span class="flex h-4 w-4 items-center justify-center">
                        @if (selectedScope(preference) === scope) {
                          <svg lucideCheck class="h-4 w-4"></svg>
                        }
                      </span>
                      <span>
                        {{ scope === 'workspace' ? 'Workspace' : 'Global' }}
                      </span>
                    </button>
                  }
                </app-dropdown-button>
              </td>
              <td class="px-3 py-1.5">
                <button
                  app-flat-button
                  type="button"
                  color="contrast"
                  class="h-7 px-3 text-xs"
                  (click)="clearValue(preference)">
                  Clear
                </button>
              </td>
            </tr>
          }
        </tbody>
      </table>
    </div>
  `,
})
export class NotificationPreferencesComponent {
  readonly values = input.required<ResolvedPreferenceValue[]>();

  readonly userPreferences = inject(UserPreferencesService);
  readonly selectedScopes = signal<Record<string, PreferenceScope>>({});

  selectedScope(preference: ResolvedPreferenceValue): PreferenceScope {
    const key = preference.definition.key;
    const selected = this.selectedScopes()[key];

    if (selected) return selected;
    if (preference.source === 'workspace') return 'workspace';
    if (preference.definition.allowedScopes.includes('global')) return 'global';

    return preference.definition.allowedScopes[0];
  }

  currentValue(preference: ResolvedPreferenceValue): boolean {
    const value = this.valueForSelectedScope(preference);

    return typeof value === 'boolean' ? value : false;
  }

  scopeLabel(preference: ResolvedPreferenceValue): string {
    return this.selectedScope(preference) === 'workspace'
      ? 'Workspace'
      : 'Global';
  }

  selectScope(preference: ResolvedPreferenceValue, scope: PreferenceScope) {
    this.selectedScopes.update((selected) => ({
      ...selected,
      [preference.definition.key]: scope,
    }));
  }

  updateValue(preference: ResolvedPreferenceValue, value: boolean) {
    this.userPreferences
      .updateValue(
        preference.definition.key,
        this.selectedScope(preference),
        value
      )
      .subscribe();
  }

  clearValue(preference: ResolvedPreferenceValue) {
    this.userPreferences
      .deleteValue(preference.definition.key, this.selectedScope(preference))
      .subscribe();
  }

  valueForSelectedScope(preference: ResolvedPreferenceValue): unknown {
    return this.selectedScope(preference) === 'workspace'
      ? (preference.workspaceValue ?? preference.effectiveValue)
      : (preference.globalValue ?? preference.effectiveValue);
  }
}

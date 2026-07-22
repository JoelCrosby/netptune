import { Component, computed, inject } from '@angular/core';
import { UserPreferencesService } from '@core/services/user-preferences.service';
import { SectionHeaderComponent } from '@static/components/section-header/section-header.component';
import { NotificationPreferencesComponent } from '../notification-preferences/notification-preferences.component';
import { PreferenceListComponent } from '../preference-list/preference-list.component';

@Component({
  selector: 'app-settings',
  template: `
    <app-section-header heading="User Preferences" />

    @for (group of visibleGroups(); track group.key) {
      <section class="mt-6">
        <h4 class="font-overpass mb-3 text-[1.1rem] font-normal">
          {{ group.label }}
        </h4>

        @if (group.key === 'notifications') {
          <app-notification-preferences [values]="group.preferences" />
        } @else {
          <app-preference-list [values]="group.preferences" />
        }
      </section>
    }
  `,
  imports: [
    NotificationPreferencesComponent,
    PreferenceListComponent,
    SectionHeaderComponent,
  ],
})
export class SettingsComponent {
  protected preferences = inject(UserPreferencesService);

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
}

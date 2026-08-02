import { Component, computed, inject } from '@angular/core';
import { UserPreferencesService } from '@core/services/user-preferences.service';
import { PreferenceListComponent } from '@settings/components/preference-list/preference-list.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';

const NOTIFICATION_GROUP = 'notifications';

@Component({
  selector: 'app-personal-general-settings-view',
  imports: [
    PageContainerComponent,
    PageHeaderComponent,
    PreferenceListComponent,
  ],
  template: `
    <app-page-container [centerPage]="true" [marginBottom]="true">
      <app-page-header
        i18n-title="Page title for general personal settings"
        title="General" />

      @for (group of groups(); track group.key) {
        <section class="mb-8">
          <h4 class="font-overpass mb-3 text-[1.1rem] font-normal">
            {{ group.label }}
          </h4>

          <app-preference-list [values]="group.preferences" />
        </section>
      }
    </app-page-container>
  `,
})
export class PersonalGeneralSettingsViewComponent {
  private readonly preferences = inject(UserPreferencesService);

  protected readonly groups = computed(() => {
    return this.preferences
      .groups()
      .filter((group) => group.key !== NOTIFICATION_GROUP)
      .map((group) => {
        return {
          ...group,
          preferences: group.preferences.filter(
            (preference) => !preference.definition.internal
          ),
        };
      })
      .filter((group) => group.preferences.length > 0);
  });

  constructor() {
    this.preferences.load();
  }
}

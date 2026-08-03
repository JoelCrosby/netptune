import { Component, computed, inject } from '@angular/core';
import { UserPreferencesService } from '@core/services/user-preferences.service';
import { NotificationPreferencesComponent } from '@settings/components/notification-preferences/notification-preferences.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';

const NOTIFICATION_GROUP = 'notifications';

@Component({
  selector: 'app-personal-notification-settings-view',
  imports: [
    NotificationPreferencesComponent,
    PageContainerComponent,
    PageHeaderComponent,
  ],
  template: `
    <app-page-container [centerPage]="true" [marginBottom]="true">
      <app-page-header
        i18n-title="Page title for personal notification settings"
        title="Notifications" />

      <app-notification-preferences [values]="preferences()" />
    </app-page-container>
  `,
})
export class PersonalNotificationSettingsViewComponent {
  private readonly userPreferences = inject(UserPreferencesService);

  protected readonly preferences = computed(() => {
    const group = this.userPreferences
      .groups()
      .find((candidate) => candidate.key === NOTIFICATION_GROUP);

    return (group?.preferences ?? []).filter(
      (preference) => !preference.definition.internal
    );
  });

  constructor() {
    this.userPreferences.load();
  }
}

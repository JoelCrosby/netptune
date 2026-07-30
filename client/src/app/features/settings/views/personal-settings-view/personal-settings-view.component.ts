import { Component } from '@angular/core';
import { SettingsComponent } from '@settings/components/settings/settings.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';

@Component({
  imports: [PageContainerComponent, PageHeaderComponent, SettingsComponent],
  template: `
    <app-page-container [centerPage]="true" [marginBottom]="true">
      <app-page-header
        i18n-title="Page title for personal settings"
        title="Personal" />

      <app-settings />
    </app-page-container>
  `,
})
export class PersonalSettingsViewComponent {}

import { ChangeDetectionStrategy, Component } from '@angular/core';
import { SettingsComponent } from '@settings/components/settings/settings.component';
import { TagsComponent } from '@settings/components/tags/tags.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';

@Component({
  templateUrl: './settings-view.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    PageContainerComponent,
    PageHeaderComponent,
    SettingsComponent,
    TagsComponent,
  ],
})
export class SettingsViewComponent {}

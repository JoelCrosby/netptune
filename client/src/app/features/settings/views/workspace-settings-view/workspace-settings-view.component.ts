import { Component } from '@angular/core';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { RouterOutlet } from '@angular/router';

@Component({
  imports: [PageContainerComponent, PageHeaderComponent, RouterOutlet],
  template: `<app-page-container [centerPage]="true" [marginBottom]="true">
    <app-page-header title="Workspace" />

    <router-outlet />
  </app-page-container> `,
})
export class WorkspaceSettingsViewComponent {}

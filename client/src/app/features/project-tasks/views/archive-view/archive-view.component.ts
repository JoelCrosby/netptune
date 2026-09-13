import { Component, computed, viewChild } from '@angular/core';
import { ArchiveListComponent } from '@project-tasks/components/archive-list/archive-list.component';
import { PageBodyComponent } from '@static/components/page-container/page-body.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';

@Component({
  selector: 'app-archive-view',
  imports: [
    ArchiveListComponent,
    PageBodyComponent,
    PageContainerComponent,
    PageHeaderComponent,
  ],
  template: `
    <app-page-container layout="list">
      <app-page-header
        toolbar
        i18n-title="Page title for the archived task list"
        title="Archive"
        [count]="count()" />

      <app-page-body>
        <app-archive-list />
      </app-page-body>
    </app-page-container>
  `,
})
export class ArchiveViewComponent {
  private readonly list = viewChild(ArchiveListComponent);
  readonly count = computed(() => this.list()?.count() ?? null);
}

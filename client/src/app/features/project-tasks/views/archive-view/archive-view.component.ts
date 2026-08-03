import { Component, signal } from '@angular/core';
import { ArchiveListComponent } from '@project-tasks/components/archive-list/archive-list.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';

@Component({
  selector: 'app-archive-view',
  imports: [PageContainerComponent, PageHeaderComponent, ArchiveListComponent],
  template: `
    <app-page-container>
      <app-page-header
        i18n-title="Page title for the archived task list"
        title="Archive"
        [count]="count()" />

      <app-archive-list (countChange)="count.set($event)" />
    </app-page-container>
  `,
})
export class ArchiveViewComponent {
  readonly count = signal<number | null>(null);
}

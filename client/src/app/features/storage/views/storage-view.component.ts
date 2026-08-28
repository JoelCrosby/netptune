import { Component } from '@angular/core';
import { PageBodyComponent } from '@static/components/page-container/page-body.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { StorageHeaderComponent } from '../components/storage-header.component';
import { StorageListComponent } from '../components/storage-list.component';

@Component({
  selector: 'app-storage-view',
  imports: [
    PageBodyComponent,
    PageContainerComponent,
    PageHeaderComponent,
    StorageHeaderComponent,
    StorageListComponent,
  ],
  template: `
    <app-page-container layout="list">
      <app-page-header
        toolbar
        i18n-title="Page title for workspace file storage"
        title="Storage" />

      <app-page-body scroll>
        <app-storage-header #header />
        <app-storage-list (fileDeleted)="header.reload()" />
      </app-page-body>
    </app-page-container>
  `,
})
export class StorageViewComponent {}

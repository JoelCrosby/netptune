import { Component } from '@angular/core';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { StorageHeaderComponent } from '../components/storage-header.component';
import { StorageListComponent } from '../components/storage-list.component';

@Component({
  selector: 'app-storage-view',
  imports: [
    PageContainerComponent,
    StorageHeaderComponent,
    StorageListComponent,
  ],
  template: `
    <app-page-container>
      <app-storage-header #header />
      <app-storage-list (fileDeleted)="header.reload()" />
    </app-page-container>
  `,
})
export class StorageViewComponent {}

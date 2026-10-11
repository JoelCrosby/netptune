import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PanelBodyComponent } from '@static/components/panel-body.component';
import { PageTreeComponent } from '../../components/page-tree.component';
import { PageTreeStore } from '../../services/page-tree.store';

@Component({
  selector: 'app-pages-layout-view',
  imports: [
    PageContainerComponent,
    PanelBodyComponent,
    PageTreeComponent,
    RouterOutlet,
  ],
  providers: [PageTreeStore],
  template: `
    <app-page-container layout="list" [horizontalPadding]="false">
      <div class="flex min-h-0 flex-1">
        <div
          app-panel-body
          padding="none"
          divider="right"
          class="flex w-[272px] shrink-0 flex-col max-md:hidden">
          <app-page-tree class="min-h-0 flex-1" />
        </div>

        <div class="flex min-w-0 flex-1 flex-col">
          <router-outlet />
        </div>
      </div>
    </app-page-container>
  `,
})
export class PagesLayoutViewComponent {}

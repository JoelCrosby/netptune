import { Component, computed, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PanelBodyComponent } from '@static/components/panel-body.component';
import { ResizeHandleComponent } from '@static/components/resize-handle.component';
import { PageTreeComponent } from '../../components/page-tree.component';
import {
  MAX_PAGE_SIDEBAR_WIDTH,
  MIN_PAGE_SIDEBAR_WIDTH,
  PageSidebarService,
} from '../../services/page-sidebar.service';
import { PageTreeStore } from '../../services/page-tree.store';

@Component({
  selector: 'app-pages-layout-view',
  imports: [
    PageContainerComponent,
    PanelBodyComponent,
    PageTreeComponent,
    ResizeHandleComponent,
    RouterOutlet,
  ],
  providers: [PageTreeStore],
  host: { '(window:resize)': 'onViewportResize()' },
  template: `
    <app-page-container layout="list" [horizontalPadding]="false">
      <div class="flex min-h-0 flex-1">
        <div
          app-panel-body
          padding="none"
          divider="right"
          class="relative flex shrink-0 flex-col max-md:hidden"
          [style.width.px]="width()">
          <app-resize-handle
            [edge]="'right'"
            i18n-label="
              Accessible name of the handle that resizes the pages sidebar
            "
            label="Resize pages sidebar"
            [width]="width()"
            [minWidth]="minWidth"
            [maxWidth]="maxWidth()"
            (widthChange)="sidebar.setWidth($event)"
            (resizingChange)="sidebar.setResizing($event)" />

          <app-page-tree class="min-h-0 flex-1" />
        </div>

        <div class="flex min-w-0 flex-1 flex-col">
          <router-outlet />
        </div>
      </div>
    </app-page-container>
  `,
})
export class PagesLayoutViewComponent {
  protected readonly sidebar = inject(PageSidebarService);
  protected readonly minWidth = MIN_PAGE_SIDEBAR_WIDTH;

  private readonly viewportWidth = signal(window.innerWidth);

  protected readonly maxWidth = computed(() => {
    const viewportShare = Math.round(this.viewportWidth() * maxViewportShare);

    return Math.max(
      MIN_PAGE_SIDEBAR_WIDTH,
      Math.min(MAX_PAGE_SIDEBAR_WIDTH, viewportShare)
    );
  });

  protected readonly width = computed(() => {
    return Math.min(this.sidebar.width(), this.maxWidth());
  });

  protected onViewportResize() {
    this.viewportWidth.set(window.innerWidth);
  }
}

const maxViewportShare = 0.35;

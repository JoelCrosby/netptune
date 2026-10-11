import { Component, inject } from '@angular/core';
import { Router } from '@angular/router';
import { hasPermission } from '@core/auth/has-permission';
import { PERMISSIONS } from '@core/auth/permissions';
import { PageKind } from '@core/models/pages';
import { PageCommandsService } from '@core/services/page-commands.service';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { PageTreeComponent } from '../../components/page-tree.component';
import { PageTreeStore } from '../../services/page-tree.store';

@Component({
  selector: 'app-pages-home-view',
  imports: [PageHeaderComponent, EmptyStateComponent, PageTreeComponent],
  host: { class: 'flex min-h-0 flex-1 flex-col overflow-y-auto' },
  template: `
    <div class="px-8 pt-4 max-md:px-3">
      <app-page-header
        i18n-title="Title of the pages home"
        title="Pages"
        [count]="store.nodes().length"
        [actionTitle]="canCreate() ? createLabel : null"
        (actionClick)="onCreate()" />
    </div>

    <div class="flex flex-col gap-6 px-8 py-6 max-md:px-3">
      <app-page-tree class="md:hidden" />

      <app-empty-state
        class="max-md:hidden"
        outlined
        i18n-title="Shown on the pages home before a page is opened"
        title="Pick a page from the tree"
        i18n-description="Hint on the pages home"
        description="Or create one to start writing. Pages can be nested inside folders and other pages." />
    </div>
  `,
})
export class PagesHomeViewComponent {
  protected readonly store = inject(PageTreeStore);
  private readonly commands = inject(PageCommandsService);
  private readonly router = inject(Router);

  protected readonly canCreate = hasPermission(PERMISSIONS.pages.create);
  protected readonly createLabel = $localize`:Button that creates a page:Create Page`;

  protected onCreate() {
    this.commands.create({ kind: PageKind.document }).subscribe((page) => {
      void this.router.navigate(this.store.link(page.slug));
    });
  }
}

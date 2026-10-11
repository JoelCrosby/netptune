import { inject, Injectable } from '@angular/core';
import { PageDetail } from '@core/models/pages';
import { PagesService } from '@core/services/pages.service';
import { WorkspaceRefreshService } from '@core/services/workspace-refresh.service';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import { PageDraft } from './page-draft';
import { PageTreeStore } from './page-tree.store';

@Injectable()
export class PageDraftFactory {
  private readonly pages = inject(PagesService);
  private readonly treeStore = inject(PageTreeStore);
  private readonly workspaceRefresh = inject(WorkspaceRefreshService);
  private readonly snackbar = inject(SnackbarService);

  create(page: PageDetail): PageDraft {
    return new PageDraft(page, this.pages, {
      renamed: ({ id, title, slug }, titleChanged) => {
        this.treeStore.renameLocally(id, title, slug);

        if (titleChanged) {
          this.workspaceRefresh.refresh(['pages']);
        }
      },
      metaSaved: () => this.workspaceRefresh.refresh(['pages']),
      failed: (message) => this.snackbar.error(message),
    });
  }
}

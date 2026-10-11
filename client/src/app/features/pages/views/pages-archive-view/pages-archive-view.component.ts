import { Component, inject } from '@angular/core';
import { ArchivedPage, PageKind } from '@core/models/pages';
import { PageCommandsService } from '@core/services/page-commands.service';
import { LucideFileText, LucideFolder } from '@lucide/angular';
import { InlineButtonComponent } from '@static/components/button/inline-button.component';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import { MediaRowComponent } from '@static/components/media-row.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { FromNowPipe } from '@static/pipes/from-now.pipe';
import { archivedPagesResource } from '../../resources/pages.resource';
import { pageArchiveRights } from '../../util/page-archive-rights';

@Component({
  selector: 'app-pages-archive-view',
  imports: [
    PageHeaderComponent,
    MediaRowComponent,
    InlineButtonComponent,
    EmptyStateComponent,
    FromNowPipe,
    LucideFolder,
    LucideFileText,
  ],
  host: { class: 'flex min-h-0 flex-1 flex-col overflow-y-auto' },
  template: `
    <div class="px-8 pt-4 max-md:px-3">
      <app-page-header
        i18n-title="Title of the archived pages list"
        title="Archive"
        [count]="archived.value().length" />
    </div>

    <div class="flex flex-col gap-2 px-8 py-6 max-md:px-3">
      @for (page of archived.value(); track page.id) {
        <app-media-row [heading]="page.title">
          <span mediaRowMedia>
            @if (page.kind === folderKind) {
              <svg lucideFolder class="h-4 w-4"></svg>
            } @else {
              <svg lucideFileText class="h-4 w-4"></svg>
            }
          </span>
          <span i18n="When and by whom an archived page was archived"
            >Archived {{ page.archivedAt | fromNow }} by
            {{ page.archivedByDisplayName }}</span
          >
          @if (canRestore(page.createdByUserId)) {
            <span mediaRowActions>
              <button app-inline-button type="button" (click)="onRestore(page)">
                <span i18n="Button that restores an archived page"
                  >Restore</span
                >
              </button>
            </span>
          }
        </app-media-row>
      } @empty {
        @if (!archived.isLoading()) {
          <app-empty-state
            outlined
            i18n-title="Shown when no page is archived"
            title="Nothing archived"
            i18n-description="Hint on the empty page archive"
            description="Archived pages appear here, ready to be restored." />
        }
      }
    </div>
  `,
})
export class PagesArchiveViewComponent {
  private readonly commands = inject(PageCommandsService);

  protected readonly archived = archivedPagesResource();
  protected readonly canRestore = pageArchiveRights();
  protected readonly folderKind = PageKind.folder;

  protected onRestore(page: ArchivedPage) {
    this.commands.restore(page.id);
  }
}

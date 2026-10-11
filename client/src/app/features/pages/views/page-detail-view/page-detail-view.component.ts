import {
  Component,
  computed,
  effect,
  inject,
  signal,
  viewChild,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { hasPermission } from '@core/auth/has-permission';
import { PERMISSIONS } from '@core/auth/permissions';
import {
  PageDetail,
  PageKind,
  PageStatus,
  PageVersionSummary,
} from '@core/models/pages';
import { prettyShortDate } from '@core/util/dates';
import { DialogService } from '@core/services/dialog.service';
import { PageCommandsService } from '@core/services/page-commands.service';
import { BreadcrumbItem } from '@static/components/breadcrumb/breadcrumb.component';
import { EditorMode } from '@static/components/editor/editor.component';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import { SnackbarService } from '@static/components/snackbar/snackbar.service';
import {
  PageVersionDialogComponent,
  PageVersionDialogData,
  PageVersionDialogResult,
} from '../../dialogs/page-version-dialog.component';
import { PageEditorComponent } from '../../components/page-editor.component';
import {
  PageRailComponent,
  versionLabel,
} from '../../components/page-rail.component';
import { PageToolbarComponent } from '../../components/page-toolbar.component';
import {
  pageDetailResource,
  pageLinksResource,
} from '../../resources/pages.resource';
import { pageArchiveRights } from '../../util/page-archive-rights';
import { PageDraft } from '../../services/page-draft';
import { PageDraftFactory } from '../../services/page-draft.factory';
import { PageTreeStore } from '../../services/page-tree.store';

@Component({
  selector: 'app-page-detail-view',
  imports: [
    PageToolbarComponent,
    PageEditorComponent,
    PageRailComponent,
    EmptyStateComponent,
  ],
  providers: [PageDraftFactory],
  host: { class: 'flex min-h-0 flex-1 flex-col' },
  template: `
    @if (detail.value(); as page) {
      @for (draft of drafts(); track draft.id) {
        <div class="flex min-h-0 flex-1">
          <div class="flex min-w-0 flex-1 flex-col">
            <app-page-toolbar
              [page]="page"
              [breadcrumbs]="breadcrumbs()"
              [saveState]="draft.state()"
              [canCreate]="canCreate()"
              [canArchive]="canArchive()"
              [canEdit]="canUpdate()"
              [status]="draft.latest().status"
              [(mode)]="mode"
              (favouriteToggled)="onFavouriteToggled(page, $event)"
              (createInside)="onCreateInside(page)"
              (linkCopied)="onCopyLink()"
              (archived)="onArchive(page)"
              (statusChanged)="onStatusChanged(draft, $event)"
              (historyRequested)="railTab.set('history')" />

            <div class="min-h-0 flex-1 overflow-y-auto">
              <app-page-editor
                [draft]="draft"
                [links]="links.value()"
                [mode]="mode()"
                [readOnly]="!canUpdate()" />
            </div>
          </div>

          @if (page.kind === documentKind) {
            <app-page-rail
              class="w-[300px] shrink-0 max-lg:hidden"
              [pageId]="draft.id"
              [(tab)]="railTab"
              [links]="links.value()"
              (taskOpened)="onTaskOpened($event)"
              (pageOpened)="onPageOpened($event)"
              [revision]="draft.latest().revision"
              [body]="draft.body()"
              (headingSelected)="onHeadingSelected($event)"
              (versionSelected)="onVersionSelected(draft, $event)" />
          }
        </div>
      }
    } @else if (detail.error()) {
      <app-empty-state
        class="m-8"
        outlined
        i18n-title="
          Shown when the requested page does not exist or was archived
        "
        title="Page not found"
        i18n-description="Hint shown when a page cannot be found"
        description="It may have been archived, or the link is out of date." />
    }
  `,
})
export class PageDetailViewComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly store = inject(PageTreeStore);
  private readonly commands = inject(PageCommandsService);
  private readonly draftFactory = inject(PageDraftFactory);
  private readonly snackbar = inject(SnackbarService);
  private readonly dialogs = inject(DialogService);
  private readonly editor = viewChild(PageEditorComponent);

  private readonly routeParams = toSignal(this.route.params, {
    initialValue: this.route.snapshot.params,
  });

  private readonly slug = computed(() => {
    const slug: unknown = this.routeParams()['slug'];

    return typeof slug === 'string' ? slug : undefined;
  });

  protected readonly detail = pageDetailResource(this.slug);
  protected readonly mode = signal<EditorMode>('rich');
  protected readonly railTab = signal<string | number | null>('outline');
  protected readonly documentKind = PageKind.document;

  protected readonly canCreate = hasPermission(PERMISSIONS.pages.create);
  protected readonly canUpdate = hasPermission(PERMISSIONS.pages.update);
  private readonly canArchivePage = pageArchiveRights();

  protected readonly canArchive = computed(() => {
    const page = this.detail.value();

    return page ? this.canArchivePage(page.createdByUserId) : false;
  });

  // One draft per page id, so the editor and its pending edits are rebuilt when the page changes.
  private readonly draft = computed(
    () => {
      const page = this.detail.value();

      return page ? this.draftFactory.create(page) : null;
    },
    { equal: (previous, next) => previous?.id === next?.id }
  );

  protected readonly drafts = computed(() => {
    const draft = this.draft();

    return draft ? [draft] : [];
  });

  private readonly linksKey = computed(() => {
    const draft = this.draft();

    return draft
      ? { pageId: draft.id, revision: draft.latest().revision }
      : undefined;
  });

  protected readonly links = pageLinksResource(this.linksKey);

  protected readonly breadcrumbs = computed<BreadcrumbItem[]>(() => {
    const draft = this.draft();

    if (!draft) return [];

    const ancestors = this.store.ancestorsOf(draft.id).map((ancestor) => ({
      id: ancestor.id,
      label: ancestor.title,
      link: this.store.link(ancestor.slug),
    }));

    const root: BreadcrumbItem = {
      id: 'pages',
      label: pagesRootLabel,
      link: this.store.homeLink(),
    };

    return [root, ...ancestors, { id: draft.id, label: titleOf(draft) }];
  });

  constructor() {
    effect(() => {
      const id = this.draft()?.id;

      if (id !== undefined) {
        this.commands.recordVisit(id);
      }
    });
  }

  protected onFavouriteToggled(page: PageDetail, isFavourite: boolean) {
    this.detail.update((current) => current && { ...current, isFavourite });
    this.commands.setFavourite(page.id, isFavourite);
  }

  protected onCreateInside(page: PageDetail) {
    this.commands
      .create({ kind: PageKind.document, parentId: page.id })
      .subscribe((created) => {
        this.store.expand(page.id);
        void this.router.navigate(this.store.link(created.slug));
      });
  }

  protected onArchive(page: PageDetail) {
    this.commands.archive(page).subscribe(() => {
      void this.router.navigate(this.store.homeLink());
    });
  }

  protected onStatusChanged(draft: PageDraft, status: PageStatus) {
    draft.updateMeta({ status });
  }

  protected async onVersionSelected(
    draft: PageDraft,
    version: PageVersionSummary
  ) {
    const isCurrent = version.revision === draft.latest().revision;
    const data: PageVersionDialogData = {
      pageId: draft.id,
      versionId: version.id,
      heading: versionHeading(version),
      canRestore: this.canUpdate() && !isCurrent,
    };

    const result = await this.dialogs.openForResult<PageVersionDialogResult>(
      PageVersionDialogComponent,
      {
        data,
        width: PageVersionDialogComponent.width,
      }
    );

    if (result !== 'restore') return;

    this.commands
      .restoreVersion(draft.id, version.id)
      .subscribe((page) => draft.replaceWith(page));
  }

  protected onTaskOpened(systemId: string) {
    void this.router.navigate([
      '/',
      this.store.workspaceSlug(),
      'tasks',
      systemId,
    ]);
  }

  protected onPageOpened(slug: string) {
    void this.router.navigate(this.store.link(slug));
  }

  protected onHeadingSelected(index: number) {
    this.editor()?.scrollToHeading(index);
  }

  protected onCopyLink() {
    const url = new URL(this.router.url, window.location.origin);

    void navigator.clipboard.writeText(url.toString()).then(
      () => this.snackbar.success(linkCopied),
      () => this.snackbar.error(linkNotCopied)
    );
  }
}

function titleOf(draft: PageDraft): string {
  return draft.title().trim() || untitled;
}

function versionHeading(version: PageVersionSummary): string {
  const label = versionLabel(version);
  const when = prettyShortDate(version.savedAt);

  return `${label} · ${when}`;
}

const pagesRootLabel = $localize`:First crumb in a page's location, linking to the pages home:Pages`;
const untitled = $localize`:Name shown for a page without a title:Untitled`;
const linkCopied = $localize`:Confirmation that a page link was copied:Link copied`;
const linkNotCopied = $localize`:Shown when a page link could not be copied:Link could not be copied`;

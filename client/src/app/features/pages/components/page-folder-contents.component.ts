import {
  Component,
  computed,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import { PageChild, PageKind, PageStatus } from '@core/models/pages';
import { fromNow } from '@core/util/dates';
import { LucideFileText, LucideFolder, LucidePlus } from '@lucide/angular';
import { BadgeComponent } from '@static/components/badge/badge.component';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { DocumentCardGridComponent } from '@static/components/document-list/document-card-grid.component';
import { DocumentListItem } from '@static/components/document-list/document-list-item';
import { DocumentTableComponent } from '@static/components/document-list/document-table.component';
import { SectionHeaderComponent } from '@static/components/section-header/section-header.component';
import {
  SegmentedControlComponent,
  SegmentedOption,
} from '@static/components/segmented-control/segmented-control.component';
import {
  SelectMenuComponent,
  SelectMenuOption,
} from '@static/components/select-menu/select-menu.component';
import { accentToneFor } from '@static/components/tones';
import { pageChildrenResource } from '../resources/pages.resource';
import {
  PageFolderView,
  PageFolderViewService,
} from '../services/page-folder-view.service';
import { PageTreeStore } from '../services/page-tree.store';

type PageChildSort = 'edited' | 'title' | 'tree';

const sortOptions: SelectMenuOption<PageChildSort>[] = [
  {
    value: 'edited',
    label: $localize`:Sort option that lists the most recently edited pages first:Last edited`,
  },
  {
    value: 'title',
    label: $localize`:Sort option that lists pages alphabetically by title:Title`,
  },
  {
    value: 'tree',
    label: $localize`:Sort option that lists pages in the order the page tree shows them:Tree order`,
  },
];

const viewOptions: SegmentedOption<PageFolderView>[] = [
  {
    value: 'list',
    label: $localize`:Option that shows a folder's pages as a table:List`,
  },
  {
    value: 'grid',
    label: $localize`:Option that shows a folder's pages as cards:Grid`,
  },
];

@Component({
  selector: 'app-page-folder-contents',
  imports: [
    BadgeComponent,
    DocumentCardGridComponent,
    DocumentTableComponent,
    FlatButtonComponent,
    LucidePlus,
    SectionHeaderComponent,
    SegmentedControlComponent,
    SelectMenuComponent,
  ],
  host: { class: 'block' },
  template: `
    @if (isLoaded()) {
      <app-section-header
        appearance="inline"
        wrap
        i18n-heading="Heading over the list of pages inside a folder"
        heading="Pages">
        <app-badge sectionHeaderBadge class="self-center">{{
          children().length
        }}</app-badge>

        <div sectionHeaderActions class="flex flex-wrap items-center gap-2">
          <app-segmented-control
            variant="subtle"
            i18n-ariaLabel="
              Accessible label for the choice between a table and cards of a
              folder's pages
            "
            ariaLabel="Folder layout"
            [options]="viewOptions"
            [value]="view()"
            (valueChange)="views.setView($event)" />

          <app-select-menu
            appearance="stroked"
            size="small"
            xPosition="before"
            i18n-prefix="Muted label ahead of the chosen sort in a folder"
            prefix="Sort:"
            i18n-ariaLabel="Accessible label for the sort of a folder's pages"
            ariaLabel="Sort pages"
            [options]="sortOptions"
            [value]="sort()"
            (valueChange)="sort.set($event)" />

          @if (canCreate()) {
            <button
              app-flat-button
              type="button"
              size="small"
              (click)="createRequested.emit()">
              <svg lucidePlus class="h-4 w-4 shrink-0"></svg>
              <span i18n="Menu item that creates a page">Create Page</span>
            </button>
          }
        </div>
      </app-section-header>

      @if (view() === 'grid') {
        <app-document-card-grid
          i18n-ariaLabel="
            Accessible label for the list of pages inside a folder
          "
          ariaLabel="Pages in this folder"
          [emptyHeading]="emptyHeading()"
          [emptyDescription]="emptyDescription"
          [items]="items()"
          [addLabel]="addLabel()"
          (added)="createRequested.emit()" />
      } @else {
        <app-document-table
          i18n-ariaLabel="
            Accessible label for the list of pages inside a folder
          "
          ariaLabel="Pages in this folder"
          i18n-titleHeader="Column heading for page titles in a folder's table"
          titleHeader="Title"
          i18n-statusHeader="Column heading for page status in a folder's table"
          statusHeader="Status"
          i18n-tagsHeader="Column heading for page tags in a folder's table"
          tagsHeader="Tags"
          i18n-editedHeader="
            Column heading for who last edited a page and when, in a folder's
            table
          "
          editedHeader="Last edited"
          [emptyHeading]="emptyHeading()"
          [emptyDescription]="emptyDescription"
          [items]="items()"
          [addLabel]="addLabel()"
          (added)="createRequested.emit()" />
      }
    }
  `,
})
export class PageFolderContentsComponent {
  private readonly store = inject(PageTreeStore);
  protected readonly views = inject(PageFolderViewService);

  readonly folderId = input.required<number>();
  readonly folderTitle = input.required<string>();
  readonly canCreate = input(false);

  readonly createRequested = output();

  protected readonly sortOptions = sortOptions;
  protected readonly viewOptions = viewOptions;
  protected readonly view = this.views.view;
  protected readonly sort = signal<PageChildSort>('edited');

  private readonly childrenResource = pageChildrenResource(this.folderId);

  protected readonly isLoaded = computed(() => {
    return this.childrenResource.value() !== undefined;
  });

  protected readonly children = computed(() => {
    return this.childrenResource.value() ?? [];
  });

  protected readonly items = computed<DocumentListItem[]>(() => {
    const sorted = sortChildren(this.children(), this.sort());

    return sorted.map((child) => this.toItem(child));
  });

  protected readonly addLabel = computed(() => {
    if (!this.canCreate()) return null;

    const folder = this.folderTitle();

    return this.children().length
      ? $localize`:Button that creates a page inside the folder being viewed:Add a page to ${folder}:FOLDER:`
      : $localize`:Button that creates the first page inside an empty folder:Add the first page to ${folder}:FOLDER:`;
  });

  protected readonly emptyHeading = computed(() => {
    const folder = this.folderTitle();

    return $localize`:Shown on a folder that holds no pages yet:Nothing in ${folder}:FOLDER: yet`;
  });

  protected readonly emptyDescription = $localize`:Hint shown on a folder that holds no pages yet:Pages you add here show up in this list and in the tree.`;

  private toItem(child: PageChild): DocumentListItem {
    const isFolder = child.kind === PageKind.folder;
    const isPublished = child.status === PageStatus.published;

    return {
      id: child.id,
      title: child.title,
      excerpt: isFolder ? pageCountLabel(child.childCount) : child.excerpt,
      icon: isFolder ? LucideFolder : LucideFileText,
      tone: isFolder ? 'neutral' : accentToneFor(child.id),
      statusLabel: isPublished ? publishedLabel : draftLabel,
      statusTone: isPublished ? 'success' : 'pending',
      tags: child.tags.map((tag) => tag.name),
      editorName: child.editorDisplayName,
      editorPictureUrl: child.editorPictureUrl,
      editedLabel: fromNow(child.updatedAt),
      readLabel: isFolder ? null : readLabel(child.readMinutes),
      link: this.store.link(child.slug),
    };
  }
}

function sortChildren(
  children: readonly PageChild[],
  sort: PageChildSort
): PageChild[] {
  switch (sort) {
    case 'edited':
      return [...children].sort((left, right) => {
        return Date.parse(right.updatedAt) - Date.parse(left.updatedAt);
      });
    case 'title':
      return [...children].sort((left, right) => {
        return left.title.localeCompare(right.title);
      });
    case 'tree':
      return [...children];
  }
}

const publishedLabel = $localize`:Status chip on a published page:Published`;
const draftLabel = $localize`:Status chip on a draft page:Draft`;

function readLabel(minutes: number): string {
  return $localize`:Reading time of a page in a folder's card:${minutes}:MINUTES: min read`;
}

function pageCountLabel(count: number): string {
  return count === 1
    ? $localize`:Number of pages inside a folder when there is exactly one:1 page`
    : $localize`:Number of pages inside a folder:${count}:COUNT: pages`;
}

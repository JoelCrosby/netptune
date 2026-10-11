import { Component, computed, input, model, output } from '@angular/core';
import { PageLinks, PageVersionSummary } from '@core/models/pages';
import { fromNow } from '@core/util/dates';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import {
  EntryListItem,
  EntryListComponent,
} from '@static/components/entry-list/entry-list.component';
import { ListGroupHeaderComponent } from '@static/components/list-group-header.component';
import { OutlineListComponent } from '@static/components/outline-list/outline-list.component';
import { PanelBodyComponent } from '@static/components/panel-body.component';
import {
  TabGroupComponent,
  TabItem,
} from '@static/components/tab-group/tab-group.component';
import { pageVersionsResource } from '../resources/pages.resource';
import { outlineFromMarkdown } from '../util/page-outline';

@Component({
  selector: 'app-page-rail',
  imports: [
    PanelBodyComponent,
    TabGroupComponent,
    OutlineListComponent,
    EntryListComponent,
    EmptyStateComponent,
    ListGroupHeaderComponent,
  ],
  host: { class: 'block' },
  template: `
    <aside
      app-panel-body
      padding="none"
      divider="left"
      class="flex h-full flex-col"
      i18n-aria-label="Accessible label for the page side rail"
      aria-label="Page details">
      <div
        app-panel-body
        padding="none"
        divider="bottom"
        class="flex h-12 shrink-0 items-center px-3">
        <app-tab-group variant="subtle" stretch [tabs]="tabs" [(value)]="tab" />
      </div>

      <div class="min-h-0 flex-1 overflow-y-auto p-3">
        @if (tab() === 'links') {
          @if (hasLinks()) {
            @if (taskEntries().length) {
              <app-list-group-header
                appearance="plain"
                i18n-label="Heading over the tasks a page mentions"
                label="Linked tasks" />
              <app-entry-list
                i18n-ariaLabel="Accessible label for the tasks a page mentions"
                ariaLabel="Linked tasks"
                [items]="taskEntries()"
                (selected)="onTaskSelected($event)" />
            }
            @if (pageEntries().length) {
              <app-list-group-header
                appearance="plain"
                i18n-label="Heading over the pages a page mentions"
                label="Linked pages" />
              <app-entry-list
                i18n-ariaLabel="Accessible label for the pages a page mentions"
                ariaLabel="Linked pages"
                [items]="pageEntries()"
                (selected)="onPageSelected($event)" />
            }
            @if (backlinkEntries().length) {
              <app-list-group-header
                appearance="plain"
                i18n-label="Heading over the pages that mention this page"
                label="Backlinks"
                [count]="backlinkEntries().length" />
              <app-entry-list
                i18n-ariaLabel="
                  Accessible label for the pages that mention this page
                "
                ariaLabel="Backlinks"
                [items]="backlinkEntries()"
                (selected)="onBacklinkSelected($event)" />
            }
          } @else {
            <app-empty-state
              compact
              i18n-title="
                Shown in the links tab before a page mentions or is mentioned by
                anything
              "
              title="No links yet"
              i18n-description="Hint in the empty links tab of a page"
              description="Type @ to mention a task or another page." />
          }
        } @else if (tab() === 'history') {
          @if (historyItems().length) {
            <app-entry-list
              highlightFirst
              i18n-ariaLabel="
                Accessible label for the list of saved versions of a page
              "
              ariaLabel="Version history"
              [items]="historyItems()"
              (selected)="onVersionSelected($event)" />
          } @else if (!versions.isLoading()) {
            <app-empty-state
              compact
              i18n-title="
                Shown in the history tab before a page has saved versions
              "
              title="No versions yet" />
          }
        } @else if (outline().length) {
          <app-outline-list
            i18n-ariaLabel="Accessible label for the list of a page's headings"
            ariaLabel="Outline"
            [items]="outline()"
            (selected)="headingSelected.emit($event)" />
        } @else {
          <app-empty-state
            compact
            i18n-title="Shown in the outline before a page has headings"
            title="No headings yet"
            i18n-description="Hint in the empty page outline"
            description="Headings you add appear here." />
        }
      </div>
    </aside>
  `,
})
export class PageRailComponent {
  readonly pageId = input.required<number>();
  readonly revision = input.required<number>();
  readonly body = input.required<string>();

  readonly links = input<PageLinks | undefined>(undefined);

  readonly headingSelected = output<number>();
  readonly taskOpened = output<string>();
  readonly pageOpened = output<string>();
  readonly versionSelected = output<PageVersionSummary>();

  readonly tab = model<string | number | null>('outline');

  protected readonly tabs: TabItem[] = [
    {
      value: 'outline',
      label: $localize`:Tab in the page side rail listing the page's headings:Outline`,
    },
    {
      value: 'links',
      label: $localize`:Tab in the page side rail listing linked tasks, pages and backlinks:Links`,
    },
    {
      value: 'history',
      label: $localize`:Tab in the page side rail listing saved versions:History`,
    },
  ];

  private readonly versionsKey = computed(() => {
    const showingHistory = this.tab() === 'history';

    return showingHistory
      ? { pageId: this.pageId(), revision: this.revision() }
      : undefined;
  });

  protected readonly versions = pageVersionsResource(this.versionsKey);

  protected readonly outline = computed(() => outlineFromMarkdown(this.body()));

  protected readonly historyItems = computed<EntryListItem[]>(() => {
    return this.versions.value().map((version) => ({
      id: version.id,
      label: versionLabel(version),
      meta: versionMeta(version),
    }));
  });

  protected readonly taskEntries = computed<EntryListItem[]>(() => {
    return (this.links()?.tasks ?? []).map((task) => ({
      id: task.id,
      label: task.name,
      meta: [task.systemId, task.statusName].filter(Boolean).join(' · '),
    }));
  });

  protected readonly pageEntries = computed<EntryListItem[]>(() => {
    return (this.links()?.pages ?? []).map((page) => ({
      id: page.id,
      label: page.title,
      meta: '',
    }));
  });

  protected readonly backlinkEntries = computed<EntryListItem[]>(() => {
    return (this.links()?.backlinks ?? []).map((backlink) => ({
      id: backlink.pageId,
      label: backlink.title,
      meta: backlink.snippet ?? '',
    }));
  });

  protected readonly hasLinks = computed(() => {
    const total =
      this.taskEntries().length +
      this.pageEntries().length +
      this.backlinkEntries().length;

    return total > 0;
  });

  protected onTaskSelected(id: number) {
    const task = this.links()?.tasks.find((candidate) => candidate.id === id);

    if (task) {
      this.taskOpened.emit(task.systemId);
    }
  }

  protected onPageSelected(id: number) {
    const page = this.links()?.pages.find((candidate) => candidate.id === id);

    if (page) {
      this.pageOpened.emit(page.slug);
    }
  }

  protected onBacklinkSelected(pageId: number) {
    const backlink = this.links()?.backlinks.find(
      (candidate) => candidate.pageId === pageId
    );

    if (backlink) {
      this.pageOpened.emit(backlink.slug);
    }
  }

  protected onVersionSelected(id: number) {
    const version = this.versions
      .value()
      .find((candidate) => candidate.id === id);

    if (version) {
      this.versionSelected.emit(version);
    }
  }
}

export function versionLabel(version: PageVersionSummary): string {
  if (version.restoredFromVersionId !== null) {
    const restoredFrom = fromNow(version.restoredFromCreatedAt);

    return $localize`:History entry for a version that restored an earlier one:Restored the version from ${restoredFrom}:WHEN:`;
  }

  if (version.revision <= 1) {
    return $localize`:History entry for the version a page was created with:Created`;
  }

  return $localize`:History entry for an ordinary edit of a page:Edited`;
}

function versionMeta(version: PageVersionSummary): string {
  const author = version.createdByDisplayName ?? '';
  const when = fromNow(version.savedAt);

  return $localize`:Who saved a page version and when:${author}:AUTHOR: · ${when}:WHEN:`;
}

import { Component, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { hasPermission } from '@core/auth/has-permission';
import { PERMISSIONS } from '@core/auth/permissions';
import { PageKind, PageTreeNode } from '@core/models/pages';
import { PageCommandsService } from '@core/services/page-commands.service';
import {
  LucideArchive,
  LucideFilePlus,
  LucideFileText,
  LucideFolder,
  LucideFolderPlus,
  LucideHouse,
  LucidePlus,
  LucideStar,
} from '@lucide/angular';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
import { DropdownMenuComponent } from '@static/components/dropdown-menu/dropdown-menu.component';
import { MenuItemComponent } from '@static/components/dropdown-menu/menu-item.component';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import { FilterInputComponent } from '@static/components/filter-input/filter-input.component';
import { ListGroupHeaderComponent } from '@static/components/list-group-header.component';
import { PanelBodyComponent } from '@static/components/panel-body.component';
import { TreeComponent } from '@static/components/tree/tree.component';
import { TreeMove, TreeNode } from '@static/components/tree/tree.types';
import {
  archiveNodeId,
  homeNodeId,
  PageTreeStore,
} from '../services/page-tree.store';

@Component({
  selector: 'app-page-tree',
  imports: [
    TreeComponent,
    ListGroupHeaderComponent,
    FilterInputComponent,
    IconButtonComponent,
    DropdownMenuComponent,
    MenuItemComponent,
    EmptyStateComponent,
    PanelBodyComponent,
    LucidePlus,
    LucideFilePlus,
    LucideFolderPlus,
  ],
  host: { class: 'flex min-h-0 flex-col' },
  template: `
    <div class="flex items-center gap-1.5 px-3 pt-3 pb-1">
      <app-filter-input
        appearance="compact"
        class="min-w-0 flex-1"
        i18n-placeholder="Placeholder in the box that filters the page tree"
        placeholder="Filter pages"
        i18n-ariaLabel="Accessible label for the box that filters the page tree"
        ariaLabel="Filter pages"
        [(value)]="filter" />

      @if (canCreate()) {
        <button
          app-icon-button
          type="button"
          aria-haspopup="menu"
          color="soft"
          size="small"
          i18n-ariaLabel="
            Accessible label for the button that creates a page or folder
          "
          ariaLabel="Create Page"
          (click)="createMenu.toggle($any($event.currentTarget))">
          <svg lucidePlus class="h-4 w-4"></svg>
        </button>

        <app-dropdown-menu #createMenu xPosition="before" panelClass="w-48 p-1">
          <button
            app-menu-item
            type="button"
            (click)="createMenu.close(); onCreate(null, documentKind)">
            <svg lucideFilePlus class="h-4 w-4 shrink-0"></svg>
            <span i18n="Menu item that creates a page">Create Page</span>
          </button>
          <button
            app-menu-item
            type="button"
            (click)="createMenu.close(); onCreate(null, folderKind)">
            <svg lucideFolderPlus class="h-4 w-4 shrink-0"></svg>
            <span i18n="Menu item that creates a folder of pages"
              >Create Folder</span
            >
          </button>
        </app-dropdown-menu>
      }
    </div>

    <nav
      class="min-h-0 flex-1 overflow-y-auto px-2 pb-3"
      i18n-aria-label="Accessible label for the page tree navigation"
      aria-label="Pages">
      @if (filter()) {
        <app-tree
          i18n-ariaLabel="Accessible label for the filtered page list"
          ariaLabel="Matching pages"
          [nodes]="matchingNodes()"
          [selectedId]="store.selectedId()" />

        @if (!matchingNodes().length) {
          <app-empty-state
            compact
            i18n-title="Shown when no page title matches the tree filter"
            title="No matching pages" />
        }
      } @else {
        <app-tree
          i18n-ariaLabel="Accessible label for the link to the pages home"
          ariaLabel="Pages home"
          [nodes]="homeNodes()"
          [selectedId]="store.homeSelected() ? homeNodeId : null" />

        @if (favouriteNodes().length) {
          <app-list-group-header
            appearance="plain"
            i18n-label="Heading over the user's favourite pages"
            label="Favourites" />
          <app-tree
            i18n-ariaLabel="Accessible label for the list of favourite pages"
            ariaLabel="Favourites"
            [nodes]="favouriteNodes()"
            [selectedId]="store.selectedId()" />
        }

        <app-list-group-header
          appearance="plain"
          i18n-label="Heading over the workspace page tree"
          label="Workspace" />

        @if (treeNodes().length) {
          <app-tree
            i18n-ariaLabel="Accessible label for the workspace page tree"
            ariaLabel="Workspace pages"
            [nodes]="treeNodes()"
            [selectedId]="store.selectedId()"
            [draggable]="canUpdate()"
            [addLabel]="canCreate() ? addChildLabel : null"
            [(expanded)]="store.expanded"
            (move)="onMove($event)"
            (add)="onCreate($event, documentKind)" />
        } @else if (!store.isLoading()) {
          <app-empty-state
            compact
            i18n-title="Shown in the page tree before any page exists"
            title="No pages yet"
            i18n-description="Hint under the empty page tree"
            description="Create a page to start writing." />
        }
      }
    </nav>

    <div
      app-panel-body
      padding="none"
      divider="top"
      class="flex h-14 shrink-0 flex-col justify-center px-2">
      <app-tree
        i18n-ariaLabel="Accessible label for the page tree footer links"
        ariaLabel="More pages"
        [nodes]="footerNodes()"
        [selectedId]="store.footerSelectedId()" />
    </div>
  `,
})
export class PageTreeComponent {
  protected readonly store = inject(PageTreeStore);
  private readonly commands = inject(PageCommandsService);
  private readonly router = inject(Router);

  protected readonly canCreate = hasPermission(PERMISSIONS.pages.create);
  protected readonly canUpdate = hasPermission(PERMISSIONS.pages.update);

  protected readonly documentKind = PageKind.document;
  protected readonly folderKind = PageKind.folder;
  protected readonly addChildLabel = $localize`:Accessible label for the button that creates a page inside another:Create Page inside`;

  protected readonly filter = signal('');

  protected readonly treeNodes = computed(() => {
    return this.store.nodes().map((node) => this.toTreeNode(node));
  });

  protected readonly favouriteNodes = computed(() => {
    return this.store
      .nodes()
      .filter((node) => node.isFavourite)
      .map((node) => ({
        ...this.toTreeNode(node),
        parentId: null,
        icon: LucideStar,
      }));
  });

  protected readonly matchingNodes = computed(() => {
    const term = this.filter().trim().toLocaleLowerCase();

    return this.store
      .nodes()
      .filter((node) => node.title.toLocaleLowerCase().includes(term))
      .map((node) => ({ ...this.toTreeNode(node), parentId: null }));
  });

  protected readonly homeNodeId = homeNodeId;

  protected readonly homeNodes = computed<TreeNode[]>(() => {
    return [
      {
        id: homeNodeId,
        parentId: null,
        label: $localize`:Link to the pages home:Home`,
        icon: LucideHouse,
        link: this.store.homeLink(),
      },
    ];
  });

  protected readonly footerNodes = computed<TreeNode[]>(() => {
    return [
      {
        id: archiveNodeId,
        parentId: null,
        label: $localize`:Link to the archived pages:Archive`,
        icon: LucideArchive,
        link: [...this.store.homeLink(), 'archive'],
      },
    ];
  });

  protected onMove(move: TreeMove) {
    this.store.move(move);
  }

  protected onCreate(parentId: number | null, kind: PageKind) {
    this.commands.create({ kind, parentId }).subscribe((page) => {
      if (parentId !== null) {
        this.store.expand(parentId);
      }

      void this.router.navigate(this.store.link(page.slug));
    });
  }

  private toTreeNode(node: PageTreeNode): TreeNode {
    const isFolder = node.kind === PageKind.folder;

    return {
      id: node.id,
      parentId: node.parentId,
      label: node.title,
      icon: isFolder ? LucideFolder : LucideFileText,
      link: this.store.link(node.slug),
    };
  }
}

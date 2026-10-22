import {
  Component,
  computed,
  inject,
  input,
  LOCALE_ID,
  model,
  output,
} from '@angular/core';
import { PageDetail, PageKind, PageStatus } from '@core/models/pages';
import {
  LucideArchive,
  LucideEllipsis,
  LucideFileCheck,
  LucideFileDown,
  LucideFilePen,
  LucideFilePlus,
  LucideFoldHorizontal,
  LucideHistory,
  LucidePanelRightClose,
  LucidePanelRightOpen,
  LucideStar,
  LucideUnfoldHorizontal,
} from '@lucide/angular';
import {
  AvatarStackComponent,
  AvatarStackItem,
} from '@static/components/avatar-stack/avatar-stack.component';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { DropdownMenuComponent } from '@static/components/dropdown-menu/dropdown-menu.component';
import { MenuItemComponent } from '@static/components/dropdown-menu/menu-item.component';
import { MenuSeparatorComponent } from '@static/components/dropdown-menu/menu-separator.component';
import { EditorMode } from '@static/components/editor/editor.component';
import { PanelBodyComponent } from '@static/components/panel-body.component';
import { PageContentWidth } from '../services/page-content-width.service';
import {
  SaveState,
  SaveStatusComponent,
} from '@static/components/save-status/save-status.component';
import {
  SegmentedControlComponent,
  SegmentedOption,
} from '@static/components/segmented-control/segmented-control.component';

@Component({
  selector: 'app-page-toolbar',
  imports: [
    PanelBodyComponent,
    AvatarStackComponent,
    SaveStatusComponent,
    SegmentedControlComponent,
    IconButtonComponent,
    DropdownMenuComponent,
    MenuItemComponent,
    MenuSeparatorComponent,
    LucideStar,
    LucideEllipsis,
    LucideFileCheck,
    LucideFileDown,
    LucideFilePen,
    LucideFilePlus,
    LucideHistory,
    LucideArchive,
    LucideFoldHorizontal,
    LucideUnfoldHorizontal,
    LucidePanelRightClose,
    LucidePanelRightOpen,
    StrokedButtonComponent,
  ],
  host: { class: 'block' },
  template: `
    <header
      app-panel-body
      padding="none"
      divider="bottom"
      class="flex h-12 items-center gap-2.5 pr-3 pl-5">
      <div class="flex-1"></div>

      @if (people().length) {
        <app-avatar-stack
          class="shrink-0"
          role="img"
          [attr.aria-label]="peopleLabel()"
          [limit]="4"
          [avatars]="people()" />
      }

      <app-save-status [state]="saveState()" />

      @if (isDocument()) {
        <app-segmented-control
          variant="subtle"
          i18n-ariaLabel="
            Accessible label for the switch between the rich and markdown editor
          "
          ariaLabel="Editor mode"
          [options]="modeOptions"
          [(value)]="mode" />

        <button
          app-icon-button
          type="button"
          color="muted"
          size="small"
          [attr.aria-pressed]="isFullWidth()"
          [ariaLabel]="contentWidthLabel"
          [title]="contentWidthLabel"
          (click)="contentWidth.set(isFullWidth() ? 'centered' : 'full')">
          @if (isFullWidth()) {
            <svg lucideFoldHorizontal class="h-4 w-4"></svg>
          } @else {
            <svg lucideUnfoldHorizontal class="h-4 w-4"></svg>
          }
        </button>
      }

      @if (canViewHistory()) {
        <button
          app-icon-button
          type="button"
          color="muted"
          size="small"
          i18n-ariaLabel="
            Accessible label for the button that shows a page's version history
          "
          ariaLabel="Version history"
          (click)="historyRequested.emit()">
          <svg lucideHistory class="h-4 w-4"></svg>
        </button>
      }

      <button
        app-stroked-button
        type="button"
        color="neutral"
        size="small"
        (click)="linkCopied.emit()">
        <span i18n="Button that copies a link to share the open page"
          >Share</span
        >
      </button>

      @if (hasMenuActions()) {
        <button
          app-icon-button
          type="button"
          color="muted"
          size="small"
          aria-haspopup="menu"
          i18n-ariaLabel="
            Accessible label for the button that opens the page action menu
          "
          ariaLabel="More page actions"
          (click)="moreMenu.toggle($any($event.currentTarget))">
          <svg lucideEllipsis class="h-4 w-4"></svg>
        </button>

        <app-dropdown-menu #moreMenu xPosition="before" panelClass="w-56 p-1">
          @if (canFavourite()) {
            <button
              app-menu-item
              type="button"
              (click)="
                moreMenu.close(); favouriteToggled.emit(!page().isFavourite)
              ">
              <svg lucideStar class="h-4 w-4 shrink-0"></svg>
              @if (page().isFavourite) {
                <span i18n="Menu item that removes a page from favourites"
                  >Remove from favourites</span
                >
              } @else {
                <span i18n="Menu item that adds a page to favourites"
                  >Add to favourites</span
                >
              }
            </button>
          }

          @if (canCreate()) {
            <button
              app-menu-item
              type="button"
              (click)="moreMenu.close(); createInside.emit()">
              <svg lucideFilePlus class="h-4 w-4 shrink-0"></svg>
              <span i18n="Menu item that creates a page inside the open page"
                >Create Page inside</span
              >
            </button>
          }

          @if (canEdit()) {
            <button
              app-menu-item
              type="button"
              (click)="moreMenu.close(); statusChanged.emit(nextStatus())">
              @if (isPublished()) {
                <svg lucideFilePen class="h-4 w-4 shrink-0"></svg>
                <span i18n="Menu item that moves a published page back to draft"
                  >Mark as draft</span
                >
              } @else {
                <svg lucideFileCheck class="h-4 w-4 shrink-0"></svg>
                <span i18n="Menu item that publishes a draft page"
                  >Publish</span
                >
              }
            </button>
          }

          @if (isDocument()) {
            <button
              app-menu-item
              type="button"
              (click)="moreMenu.close(); exportRequested.emit()">
              <svg lucideFileDown class="h-4 w-4 shrink-0"></svg>
              <span
                i18n="Menu item that downloads the open page as a markdown file"
                >Export as Markdown</span
              >
            </button>
          }

          @if (canArchive()) {
            <app-menu-separator />

            <button
              app-menu-item
              type="button"
              color="warn"
              (click)="moreMenu.close(); archived.emit()">
              <svg lucideArchive class="h-4 w-4 shrink-0"></svg>
              <span i18n="Menu item that archives the open page">Archive</span>
            </button>
          }
        </app-dropdown-menu>
      }

      @if (isDocument()) {
        <button
          app-icon-button
          type="button"
          color="muted"
          size="small"
          class="max-lg:hidden"
          [attr.aria-pressed]="!railCollapsed()"
          [ariaLabel]="railLabel"
          [title]="railLabel"
          (click)="railCollapsed.set(!railCollapsed())">
          @if (railCollapsed()) {
            <svg lucidePanelRightOpen class="h-4 w-4"></svg>
          } @else {
            <svg lucidePanelRightClose class="h-4 w-4"></svg>
          }
        </button>
      }
    </header>
  `,
})
export class PageToolbarComponent {
  readonly page = input.required<PageDetail>();
  readonly saveState = input.required<SaveState>();
  readonly canCreate = input(false);
  readonly canArchive = input(false);
  readonly canEdit = input(false);
  readonly canFavourite = input(false);
  readonly canViewHistory = input(false);
  readonly status = input.required<PageStatus>();
  readonly mode = model<EditorMode>('rich');
  readonly contentWidth = model<PageContentWidth>('centered');
  readonly railCollapsed = model(false);
  readonly people = input<readonly AvatarStackItem[]>([]);

  readonly favouriteToggled = output<boolean>();
  readonly createInside = output();
  readonly linkCopied = output();
  readonly archived = output();
  readonly statusChanged = output<PageStatus>();
  readonly historyRequested = output();
  readonly exportRequested = output();

  protected readonly contentWidthLabel = $localize`:Accessible label for the toggle that lets a page's writing run the full width of the editor:Full width`;

  private readonly listFormat = new Intl.ListFormat(inject(LOCALE_ID), {
    type: 'conjunction',
  });

  protected readonly peopleLabel = computed(() => {
    return this.listFormat.format(
      this.people().map((person) => person.label ?? person.displayName)
    );
  });

  protected readonly railLabel = $localize`:Accessible label for the toggle that shows or hides the panel beside a page:Side panel`;

  protected readonly isFullWidth = computed(() => {
    return this.contentWidth() === 'full';
  });

  protected readonly isPublished = computed(() => {
    return this.status() === PageStatus.published;
  });

  protected readonly nextStatus = computed(() => {
    return this.isPublished() ? PageStatus.draft : PageStatus.published;
  });

  protected readonly hasMenuActions = computed(() => {
    return (
      this.isDocument() ||
      this.canFavourite() ||
      this.canCreate() ||
      this.canEdit() ||
      this.canArchive()
    );
  });

  protected readonly isDocument = computed(() => {
    return this.page().kind === PageKind.document;
  });

  protected readonly modeOptions: SegmentedOption<EditorMode>[] = [
    {
      value: 'rich',
      label: $localize`:Option that shows a page as formatted text:Rich`,
    },
    {
      value: 'markdown',
      label: $localize`:Option that shows a page as raw markdown:Markdown`,
    },
  ];
}

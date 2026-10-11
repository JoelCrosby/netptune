import { Component, computed, input, output } from '@angular/core';
import { PageStatus, PageTag } from '@core/models/pages';
import { tagResource } from '@core/resources/tag.resource';
import { LucidePlus } from '@lucide/angular';
import { BadgeComponent } from '@static/components/badge/badge.component';
import { InlineButtonComponent } from '@static/components/button/inline-button.component';
import { DropdownMenuComponent } from '@static/components/dropdown-menu/dropdown-menu.component';
import {
  FilterOption,
  FilterOptionListComponent,
} from '@static/components/filter-option-list/filter-option-list.component';

@Component({
  selector: 'app-page-meta-chips',
  imports: [
    BadgeComponent,
    InlineButtonComponent,
    DropdownMenuComponent,
    FilterOptionListComponent,
    LucidePlus,
  ],
  host: { class: 'flex flex-wrap items-center gap-2' },
  template: `
    @if (status() === published) {
      <app-badge color="success" i18n="Status chip on a published page"
        >Published</app-badge
      >
    } @else {
      <app-badge color="pending" i18n="Status chip on a draft page"
        >Draft</app-badge
      >
    }

    @for (tag of tags(); track tag.id) {
      <app-badge shape="rounded">{{ tag.name }}</app-badge>
    }

    @if (canEdit()) {
      <button
        app-inline-button
        type="button"
        color="neutral"
        aria-haspopup="menu"
        (click)="tagMenu.toggle($any($event.currentTarget))">
        <svg lucidePlus class="h-3 w-3"></svg>
        <span i18n="Button that adds or removes tags on a page">Tag</span>
      </button>

      <app-dropdown-menu #tagMenu panelRole="none" panelClass="p-0">
        <app-filter-option-list
          [open]="tagMenu.showing()"
          [options]="tagOptions()"
          [selected]="selectedIds()"
          [listAriaLabel]="tagsLabel"
          [searchPlaceholder]="searchTagsLabel"
          (toggled)="onToggled($event)"
          (dismissed)="tagMenu.closeAndFocusTrigger()" />
      </app-dropdown-menu>
    }
  `,
})
export class PageMetaChipsComponent {
  readonly status = input.required<PageStatus>();
  readonly tags = input.required<readonly PageTag[]>();
  readonly canEdit = input(false);

  readonly tagsChanged = output<PageTag[]>();

  private readonly workspaceTags = tagResource();

  protected readonly published = PageStatus.published;
  protected readonly tagsLabel = $localize`:Accessible label for the list of tags a page can have:Tags`;
  protected readonly searchTagsLabel = $localize`:Placeholder in the box that narrows the tags offered for a page:Search tags`;

  protected readonly tagOptions = computed<FilterOption<number>[]>(() => {
    return this.workspaceTags.value().map((tag) => ({
      value: tag.id,
      label: tag.name,
    }));
  });

  protected readonly selectedIds = computed(() => {
    return new Set(this.tags().map((tag) => tag.id));
  });

  protected onToggled(tagId: number) {
    const current = this.tags();
    const isSelected = current.some((tag) => tag.id === tagId);

    if (isSelected) {
      this.tagsChanged.emit(current.filter((tag) => tag.id !== tagId));

      return;
    }

    const added = this.workspaceTags.value().find((tag) => tag.id === tagId);

    if (!added) return;

    this.tagsChanged.emit([...current, { id: added.id, name: added.name }]);
  }
}

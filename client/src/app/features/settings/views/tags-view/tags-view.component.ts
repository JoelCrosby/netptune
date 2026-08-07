import { Component, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { DialogService } from '@core/services/dialog.service';
import {
  CreateTagDialogComponent,
  CreateTagDialogResult,
} from '@entry/dialogs/create-tag-dialog/create-tag-dialog.component';
import {
  EditTagDialogComponent,
  EditTagDialogResult,
} from '@entry/dialogs/edit-tag-dialog/edit-tag-dialog.component';
import { ErrorStateComponent } from '@static/components/error-state/error-state.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { IconButtonComponent } from '@static/components/button/icon-button.component';
import {
  TableComponent,
  TableEmptyCellDirective,
  TableHeaderRowDirective,
  TableHeadDirective,
  TableRowDirective,
} from '@static/components/table/table.component';
import { TooltipDirective } from '@app/static/directives/tooltip.directive';
import { Tag } from '@core/models/tag';
import * as actions from '@core/store/tags/tags.actions';
import { LucideSettings2, LucideX } from '@lucide/angular';
import { Actions, ofType } from '@ngrx/effects';
import { Store } from '@ngrx/store';
import { first } from 'rxjs';
import { tagResource } from '@app/core/resources/tag.resource';

@Component({
  selector: 'app-tags-view',
  imports: [
    IconButtonComponent,
    RouterLink,
    TooltipDirective,
    LucideSettings2,
    LucideX,
    ErrorStateComponent,
    PageContainerComponent,
    PageHeaderComponent,
    TableComponent,
    TableEmptyCellDirective,
    TableHeaderRowDirective,
    TableHeadDirective,
    TableRowDirective,
  ],
  template: `
    <app-page-container [centerPage]="true" [marginBottom]="true">
      <app-page-header
        i18n-title="Page title for workspace tags"
        title="Tags"
        i18n-actionTitle="Button that opens the create-tag dialog"
        actionTitle="Create tag"
        (actionClick)="openCreateDialog()" />

      @if (tags.error()) {
        <app-error-state
          compact
          i18n-title="Shown when the tag list fails to load"
          title="Tags could not be loaded"
          i18n-description="Advice shown when a list fails to load"
          description="Check your connection and try again."
          (retry)="tags.reload()" />
      } @else {
        <app-table containerClass=" overflow-hidden" tableClass="table-fixed">
          <thead appTableHead>
            <tr appTableHeaderRow>
              <th class="px-4 py-3" i18n="Column heading for the name">Name</th>
              <th
                class="w-24 px-4 py-3"
                i18n="Column heading for the number of tasks using a row">
                Tasks
              </th>
              <th class="w-24 px-2 py-3"></th>
            </tr>
          </thead>
          <tbody>
            @for (tag of tags.value(); track tag.id) {
              <tr appTableRow class="group">
                <td class="px-4 py-2 align-middle">
                  <a
                    class="block w-full truncate text-left font-medium hover:underline"
                    [routerLink]="[tag.id]">
                    {{ tag.name }}
                  </a>
                </td>
                <td class="text-muted px-4 py-2 align-middle">
                  {{ tag.taskCount }}
                </td>
                <td class="px-2 py-2 align-middle">
                  <div class="flex gap-1">
                    <button
                      app-icon-button
                      i18n-appTooltip="Tooltip on the button that edits a tag"
                      appTooltip="Edit tag"
                      type="button"
                      i18n-aria-label="
                        Accessible label for the button that edits a tag
                      "
                      aria-label="Edit tag"
                      (click)="openEditDialog(tag)">
                      <svg lucideSettings2 class="h-4 w-4"></svg>
                    </button>
                    <button
                      app-icon-button
                      i18n-appTooltip="Tooltip on the button that deletes a tag"
                      appTooltip="Delete tag"
                      type="button"
                      i18n-aria-label="
                        Accessible label for the button that deletes a tag
                      "
                      aria-label="Delete tag"
                      (click)="onDeleteClicked(tag)">
                      <svg lucideX class="h-4 w-4"></svg>
                    </button>
                  </div>
                </td>
              </tr>
            } @empty {
              <tr>
                <td appTableEmptyCell colspan="3">
                  <span i18n="Empty state for the tag list">
                    No tags yet. Create one to group tasks across projects.
                  </span>
                </td>
              </tr>
            }
          </tbody>
        </app-table>
      }
    </app-page-container>
  `,
})
export class TagsViewComponent {
  private store = inject(Store);
  private actions$ = inject(Actions);
  private dialog = inject(DialogService);

  tags = tagResource();

  constructor() {
    this.store.dispatch(actions.loadTags.init());

    this.actions$
      .pipe(
        ofType(
          actions.addTag.success,
          actions.editTag.success,
          actions.deleteTags.success
        ),
        takeUntilDestroyed()
      )
      .subscribe(() => this.tags.reload());
  }

  openCreateDialog() {
    const dialogRef = this.dialog.open<CreateTagDialogResult>(
      CreateTagDialogComponent,
      {
        width: '420px',
      }
    );

    dialogRef.closed.pipe(first()).subscribe({
      next: (result) => {
        const name = result?.name.trim();
        if (!name) return;

        this.store.dispatch(actions.addTag.init({ name }));
      },
    });
  }

  openEditDialog(tag: Tag) {
    const dialogRef = this.dialog.open<EditTagDialogResult, Tag>(
      EditTagDialogComponent,
      {
        data: tag,
        width: '420px',
      }
    );

    dialogRef.closed.pipe(first()).subscribe({
      next: (result) => {
        const newValue = result?.name.trim();
        if (!newValue || newValue === tag.name) return;

        this.store.dispatch(
          actions.editTag.init({ currentValue: tag.name, newValue })
        );
      },
    });
  }

  onDeleteClicked(tag: Tag) {
    if (!tag) return;

    const tags = [tag.name];
    this.store.dispatch(actions.deleteTags.init({ tags }));
  }
}

import { Component, inject } from '@angular/core';
import { DialogService } from '@core/services/dialog.service';
import {
  CreateTagDialogComponent,
  CreateTagDialogResult,
} from '@entry/dialogs/create-tag-dialog/create-tag-dialog.component';
import {
  EditTagDialogComponent,
  EditTagDialogResult,
} from '@entry/dialogs/edit-tag-dialog/edit-tag-dialog.component';
import { StrokedButtonComponent } from '@app/static/components/button/stroked-button.component';
import { SectionHeaderComponent } from '@static/components/section-header/section-header.component';
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
import { LucidePencil, LucidePlus, LucideX } from '@lucide/angular';
import { Store } from '@ngrx/store';
import { first } from 'rxjs';
import { tagResource } from '@app/core/resources/tag.resource';

@Component({
  selector: 'app-tags-view',
  imports: [
    IconButtonComponent,
    TooltipDirective,
    LucidePencil,
    LucidePlus,
    LucideX,
    StrokedButtonComponent,
    SectionHeaderComponent,
    TableComponent,
    TableEmptyCellDirective,
    TableHeaderRowDirective,
    TableHeadDirective,
    TableRowDirective,
  ],
  template: `<app-section-header class="max-w-xl" heading="Tags">
      <button
        sectionHeaderActions
        app-stroked-button
        type="button"
        (click)="openCreateDialog()">
        <svg lucidePlus class="h-4 w-4"></svg>
        <span>Create tag</span>
      </button>
    </app-section-header>

    <app-table
      containerClass="max-w-xl overflow-hidden"
      tableClass="table-fixed">
      <thead appTableHead>
        <tr appTableHeaderRow>
          <th class="px-4 py-3">Name</th>
          <th class="w-24 px-2 py-3"></th>
        </tr>
      </thead>
      <tbody>
        @for (tag of tags.value(); track tag.id) {
          <tr appTableRow class="group bg-card">
            <td class="px-4 py-2 align-middle">
              <button
                type="button"
                class="block w-full cursor-pointer truncate text-left font-medium"
                (click)="openEditDialog(tag)">
                {{ tag.name }}
              </button>
            </td>
            <td class="px-2 py-2 align-middle">
              <div class="flex gap-1">
                <button
                  app-icon-button
                  appTooltip="Edit tag"
                  type="button"
                  aria-label="Edit tag"
                  (click)="openEditDialog(tag)">
                  <svg lucidePencil class="h-4 w-4"></svg>
                </button>
                <button
                  app-icon-button
                  appTooltip="Delete tag"
                  type="button"
                  aria-label="Delete tag"
                  (click)="onDeleteClicked(tag)">
                  <svg lucideX class="h-4 w-4"></svg>
                </button>
              </div>
            </td>
          </tr>
        } @empty {
          <tr>
            <td appTableEmptyCell colspan="2">No tags</td>
          </tr>
        }
      </tbody>
    </app-table> `,
})
export class TagsViewComponent {
  private store = inject(Store);
  private dialog = inject(DialogService);

  tags = tagResource();

  constructor() {
    this.store.dispatch(actions.loadTags.init());
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

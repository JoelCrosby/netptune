import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import {
  CdkDrag,
  CdkDragDrop,
  CdkDragHandle,
  CdkDropList,
  moveItemInArray,
} from '@angular/cdk/drag-drop';
import { Component, computed, inject, signal } from '@angular/core';
import { LucideGripVertical } from '@lucide/angular';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { CheckboxComponent } from '@static/components/checkbox/checkbox.component';
import { DialogTitleComponent } from '@static/components/dialog-title/dialog-title.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { DialogCloseDirective } from '@static/directives/dialog-close.directive';
import { DatatableColumnPreference } from './datatable.types';

export interface DatatableColumnsDialogItem {
  id: string;
  header: string;
  visible: boolean;
}

export interface DatatableColumnsDialogData {
  items: DatatableColumnsDialogItem[];
}

@Component({
  imports: [
    CdkDropList,
    CdkDrag,
    CdkDragHandle,
    CheckboxComponent,
    DialogTitleComponent,
    FlatButtonComponent,
    StrokedButtonComponent,
    DialogActionsDirective,
    DialogCloseDirective,
    LucideGripVertical,
  ],
  template: `
    <app-dialog-title>Customize Columns</app-dialog-title>

    <p class="text-foreground/60 mb-4 text-sm">
      Toggle which columns are shown and drag to reorder them.
    </p>

    <div
      class="flex flex-col gap-1"
      cdkDropList
      (cdkDropListDropped)="drop($event)">
      @for (item of items(); track item.id) {
        <div
          class="border-border bg-card flex items-center gap-3 rounded border px-2 py-2"
          cdkDrag>
          <button
            type="button"
            class="text-foreground/40 hover:text-foreground cursor-grab active:cursor-grabbing"
            aria-label="Drag to reorder"
            cdkDragHandle>
            <svg lucideGripVertical class="h-4 w-4"></svg>
          </button>

          <app-checkbox
            class="min-w-0 flex-1"
            [checked]="item.visible"
            [disabled]="item.visible && onlyOneVisible()"
            (changed)="toggle(item.id, $event)">
            <span class="truncate">{{ item.header }}</span>
          </app-checkbox>
        </div>
      }
    </div>

    <div app-dialog-actions align="end">
      <button app-stroked-button app-dialog-close>Cancel</button>
      <button app-flat-button color="primary" type="button" (click)="save()">
        Save
      </button>
    </div>
  `,
})
export class DatatableColumnsDialogComponent {
  private data = inject<DatatableColumnsDialogData>(DIALOG_DATA);
  private dialogRef = inject<DialogRef<DatatableColumnPreference[]>>(DialogRef);

  items = signal<DatatableColumnsDialogItem[]>(
    this.data.items.map((item) => ({ ...item }))
  );

  visibleCount = computed(
    () => this.items().filter((item) => item.visible).length
  );
  onlyOneVisible = computed(() => this.visibleCount() <= 1);

  drop(event: CdkDragDrop<DatatableColumnsDialogItem[]>) {
    const next = [...this.items()];
    moveItemInArray(next, event.previousIndex, event.currentIndex);
    this.items.set(next);
  }

  toggle(id: string, visible: boolean) {
    this.items.update((items) =>
      items.map((item) => (item.id === id ? { ...item, visible } : item))
    );
  }

  save() {
    const preferences = this.items().map(({ id, visible }) => ({
      id,
      visible,
    }));
    this.dialogRef.close(preferences);
  }
}

import { DialogRef } from '@angular/cdk/dialog';
import { Component, computed, inject } from '@angular/core';
import { Router } from '@angular/router';
import { taskPagesResource } from '@core/resources/task-pages.resource';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import {
  EntryListComponent,
  EntryListItem,
} from '@static/components/entry-list/entry-list.component';
import { TaskDetailService } from './task-detail.service';

@Component({
  selector: 'app-task-detail-pages',
  imports: [EntryListComponent, EmptyStateComponent],
  template: `
    @if (entries().length) {
      <app-entry-list
        i18n-ariaLabel="Accessible label for the pages that mention a task"
        ariaLabel="Pages mentioning this task"
        [items]="entries()"
        (selected)="onSelected($event)" />
    } @else if (!pages.isLoading()) {
      <app-empty-state
        compact
        i18n-title="Shown on a task that no page mentions"
        title="No pages mention this task"
        i18n-description="Hint on a task that no page mentions"
        description="Type @ in a page to mention it here." />
    }
  `,
})
export class TaskDetailPagesComponent {
  private readonly taskDetail = inject(TaskDetailService);
  private readonly router = inject(Router);
  private readonly dialogRef = inject(DialogRef, { optional: true });
  private readonly workspaceSlug = inject(CurrentWorkspaceService).slug;

  protected readonly pages = taskPagesResource(this.taskDetail.systemId);

  readonly count = computed(() => this.pages.value().length);

  protected readonly entries = computed<EntryListItem[]>(() => {
    return this.pages.value().map((page) => ({
      id: page.pageId,
      label: page.title,
      meta: page.snippet ?? '',
    }));
  });

  protected onSelected(pageId: number) {
    const page = this.pages
      .value()
      .find((candidate) => candidate.pageId === pageId);
    const workspace = this.workspaceSlug();

    if (!page || !workspace) return;

    // Shown inside the task dialog and on the standalone task page; the dialog must close or it would
    // sit over the page being opened.
    this.dialogRef?.close();

    void this.router.navigate(['/', workspace, 'pages', page.slug]);
  }
}

import { Component, inject } from '@angular/core';
import { ActivatedRoute, ParamMap } from '@angular/router';
import { CreateTaskDialogComponent } from '@entry/dialogs/create-task-dialog/create-task-dialog.component';
import {
  CREATE_TASK_PAGE_DATA,
  CreateTaskDialogData,
  CreateTaskDraftService,
} from '@entry/dialogs/create-task-dialog/create-task-draft';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';

function readId(params: ParamMap, key: string) {
  const value = Number(params.get(key));

  return Number.isInteger(value) && value > 0 ? value : undefined;
}

function createTaskPageData(): CreateTaskDialogData {
  const params = inject(ActivatedRoute).snapshot.queryParamMap;
  const draft = inject(CreateTaskDraftService).take() ?? undefined;

  return {
    projectId: readId(params, 'projectId'),
    sprintId: readId(params, 'sprintId'),
    draft,
  };
}

@Component({
  selector: 'app-create-task-page',
  imports: [CreateTaskDialogComponent, PageContainerComponent],
  providers: [
    { provide: CREATE_TASK_PAGE_DATA, useFactory: createTaskPageData },
  ],
  template: `
    <app-page-container layout="list" [horizontalPadding]="false">
      <app-create-task-dialog />
    </app-page-container>
  `,
})
export class CreateTaskPageComponent {}

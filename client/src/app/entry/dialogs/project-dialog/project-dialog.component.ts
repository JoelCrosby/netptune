import { ChangeDetectionStrategy, Component, OnDestroy } from '@angular/core';
import { UntypedFormControl, UntypedFormGroup, Validators } from '@angular/forms';
import { MatDialogRef } from '@angular/material/dialog';
import { AppState } from '@core/core.state';
import { AddProjectRequest } from '@core/models/project';
import { createProject } from '@core/store/projects/projects.actions';
import { selectCurrentWorkspace } from '@core/store/workspaces/workspaces.selectors';
import { Store } from '@ngrx/store';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-project-dialog',
  templateUrl: './project-dialog.component.html',
  styleUrls: ['./project-dialog.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProjectDialogComponent implements OnDestroy {
  currentWorkspace$ = this.store.select(selectCurrentWorkspace);
  subs = new Subscription();

  projectFromGroup = new UntypedFormGroup({
    name: new UntypedFormControl('', [Validators.required, Validators.minLength(4)]),
    repositoryUrl: new UntypedFormControl(),
    description: new UntypedFormControl(),
    workspace: new UntypedFormControl(),
    color: new UntypedFormControl('#673AB7'),
  });

  get name() {
    return this.projectFromGroup.get('name') as UntypedFormControl;
  }
  get description() {
    return this.projectFromGroup.get('description') as UntypedFormControl;
  }
  get repositoryUrl() {
    return this.projectFromGroup.get('repositoryUrl') as UntypedFormControl;
  }
  get color() {
    return this.projectFromGroup.get('color') as UntypedFormControl;
  }

  constructor(
    private store: Store<AppState>,
    public dialogRef: MatDialogRef<ProjectDialogComponent>
  ) {}

  ngOnDestroy() {
    this.subs.unsubscribe();
  }

  close() {
    this.dialogRef.close();
  }

  getResult() {
    this.subs = this.currentWorkspace$.subscribe({
      next: (workspace) => {
        if (!workspace?.slug) return;

        const project: AddProjectRequest = {
          name: this.name.value,
          description: this.description.value,
          repositoryUrl: this.repositoryUrl.value,
          workspace: workspace.slug,
          metaInfo: {
            color: this.color.value,
          },
        };

        this.store.dispatch(createProject({ project }));

        this.dialogRef.close();
      },
    });
  }
}

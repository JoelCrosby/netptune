import { ChangeDetectionStrategy, Component, OnDestroy } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { MatDialogRef } from '@angular/material/dialog';
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

  projectFromGroup = new FormGroup({
    name: new FormControl('', [Validators.required, Validators.minLength(4)]),
    repositoryUrl: new FormControl(),
    description: new FormControl(),
    workspace: new FormControl(),
    color: new FormControl('#673AB7'),
  });

  get name() {
    return this.projectFromGroup.get('name');
  }
  get description() {
    return this.projectFromGroup.get('description');
  }
  get repositoryUrl() {
    return this.projectFromGroup.get('repositoryUrl');
  }
  get color() {
    return this.projectFromGroup.get('color');
  }

  constructor(
    private store: Store,
    public dialogRef: MatDialogRef<ProjectDialogComponent>
  ) {}

  ngOnDestroy() {
    this.subs.unsubscribe();
  }

  close() {
    this.dialogRef.close();
  }

  getResult() {
    this.subs = this.currentWorkspace$.subscribe((workspace) => {
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
    });
  }
}

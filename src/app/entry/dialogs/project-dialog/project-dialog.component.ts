import {
  ChangeDetectionStrategy,
  Component,
  Inject,
  OnDestroy,
  OnInit,
  Optional,
} from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { AddProjectRequest, Project } from '@core/models/project';
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
export class ProjectDialogComponent implements OnInit, OnDestroy {
  project: Project;
  currentWorkspace$ = this.store.select(selectCurrentWorkspace);
  subs = new Subscription();

  projectFromGroup = new FormGroup({
    nameFormControl: new FormControl('', [
      Validators.required,
      Validators.minLength(4),
    ]),
    repositoryUrlFormControl: new FormControl(),
    descriptionFormControl: new FormControl(),
    workspaceFormControl: new FormControl(),
  });

  get name() {
    return this.projectFromGroup.get('nameFormControl');
  }
  get description() {
    return this.projectFromGroup.get('descriptionFormControl');
  }
  get repositoryUrl() {
    return this.projectFromGroup.get('repositoryUrlFormControl');
  }

  constructor(
    private store: Store,
    public dialogRef: MatDialogRef<ProjectDialogComponent>,
    @Optional() @Inject(MAT_DIALOG_DATA) public data: Project
  ) {
    if (data) {
      this.project = data;
    }
  }

  ngOnInit() {
    if (this.project) {
      this.name.setValue(this.project.name);
      this.description.setValue(this.project.description);
      this.repositoryUrl.setValue(this.project.repositoryUrl);
    } else {
      this.projectFromGroup.reset();
    }
  }

  ngOnDestroy(): void {
    this.subs.unsubscribe();
  }

  close(): void {
    this.dialogRef.close();
  }

  getResult() {
    this.subs = this.currentWorkspace$.subscribe((workspace) => {
      const project: AddProjectRequest = {
        name: this.name.value,
        description: this.description.value,
        repositoryUrl: this.repositoryUrl.value,
        workspace: workspace.slug,
      };

      this.store.dispatch(createProject({ project }));

      this.dialogRef.close();
    });
  }
}

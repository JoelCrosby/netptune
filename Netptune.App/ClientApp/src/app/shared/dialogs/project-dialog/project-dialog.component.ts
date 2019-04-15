import { Component, Inject, OnDestroy, OnInit, Optional } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material';
import { AppState } from '@app/core/core.state';
import { Project } from '@app/core/models/project';
import { SelectCurrentWorkspace } from '@app/core/state/core.selectors';
import { ActionCreateProject } from '@app/features/projects/store/projects.actions';
import { Store } from '@ngrx/store';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-project-dialog',
  templateUrl: './project-dialog.component.html',
  styleUrls: ['./project-dialog.component.scss'],
})
export class ProjectDialogComponent implements OnInit, OnDestroy {
  project: Project;
  currentWorkspace$ = this.store.select(SelectCurrentWorkspace);
  subs = new Subscription();

  constructor(
    private store: Store<AppState>,
    public dialogRef: MatDialogRef<ProjectDialogComponent>,
    @Optional() @Inject(MAT_DIALOG_DATA) public data: Project
  ) {
    if (data) {
      this.project = data;
    }
  }

  projectFromGroup = new FormGroup({
    nameFormControl: new FormControl('', [Validators.required, Validators.minLength(4)]),
    repositoryUrlFormControl: new FormControl(),
    descriptionFormControl: new FormControl(),
    workspaceFormControl: new FormControl(),
  });

  ngOnInit() {
    if (this.project) {
      this.projectFromGroup.controls['nameFormControl'].setValue(this.project.name);
      this.projectFromGroup.controls['descriptionFormControl'].setValue(this.project.description);
      this.projectFromGroup.controls['repositoryUrlFormControl'].setValue(
        this.project.repositoryUrl
      );
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
    this.subs = this.currentWorkspace$.subscribe(workspace => {
      const projectResult = new Project();

      if (this.project) {
        projectResult.id = this.project.id;
      }

      projectResult.name = this.projectFromGroup.get('nameFormControl').value;
      projectResult.description = this.projectFromGroup.get('descriptionFormControl').value;
      projectResult.repositoryUrl = this.projectFromGroup.get('repositoryUrlFormControl').value;

      projectResult.workspace = workspace;
      projectResult.workspaceId = workspace.id;

      this.store.dispatch(new ActionCreateProject(projectResult));

      this.dialogRef.close();
    });
  }
}

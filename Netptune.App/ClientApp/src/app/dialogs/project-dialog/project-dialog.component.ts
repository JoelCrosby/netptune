import { Component, Inject, OnInit, Optional } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material';
import { Project } from '@app/models/project';
import { WorkspaceService } from '@app/services/workspace/workspace.service';

@Component({
  selector: 'app-project-dialog',
  templateUrl: './project-dialog.component.html',
  styleUrls: ['./project-dialog.component.scss']
})
export class ProjectDialogComponent implements OnInit {

  public project: Project;
  public selectedTypeValue: number;

  constructor(
    public dialogRef: MatDialogRef<ProjectDialogComponent>,
    @Optional() @Inject(MAT_DIALOG_DATA) public data: Project,
    public workspaceService: WorkspaceService) {

    if (data) {
      this.project = data;
    }
  }

  projectFromGroup = new FormGroup({

    nameFormControl: new FormControl('', [
      Validators.required,
      Validators.minLength(4),
    ]),

    repositoryUrlFormControl: new FormControl('', [
    ]),

    descriptionFormControl: new FormControl('', [
    ])

  });

  ngOnInit() {

    if (this.project) {

      this.projectFromGroup.controls['nameFormControl'].setValue(this.project.name);
      this.projectFromGroup.controls['descriptionFormControl'].setValue(this.project.description);
      this.projectFromGroup.controls['repositoryUrlFormControl'].setValue(this.project.repositoryUrl);
    } else {
      this.projectFromGroup.reset();
    }
  }

  close(): void {
    this.dialogRef.close();
  }

  getResult() {
    const projectResult = new Project();

    if (this.project) {
      projectResult.id = this.project.id;
    }

    if (!this.workspaceService.currentWorkspace) {
      throw new Error('currentWorkspace  null or undefined!');
    }

    projectResult.name = this.projectFromGroup.controls['nameFormControl'].value;
    projectResult.description = this.projectFromGroup.controls['descriptionFormControl'].value;
    projectResult.repositoryUrl = this.projectFromGroup.controls['repositoryUrlFormControl'].value;
    projectResult.workspace = this.workspaceService.currentWorkspace;
    projectResult.workspaceId = this.workspaceService.currentWorkspace.id;

    this.dialogRef.close(projectResult);
  }


}

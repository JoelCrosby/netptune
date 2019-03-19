import { Component, Inject, OnInit, Optional } from '@angular/core';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material';
import { Project } from '@app/core/models/project';

@Component({
  selector: 'app-project-dialog',
  templateUrl: './project-dialog.component.html',
  styleUrls: ['./project-dialog.component.scss'],
})
export class ProjectDialogComponent implements OnInit {
  public project: Project;
  public selectedTypeValue: number;

  constructor(
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

  close(): void {
    this.dialogRef.close();
  }

  getResult() {
    const projectResult = new Project();

    this.dialogRef.close(projectResult);
  }
}

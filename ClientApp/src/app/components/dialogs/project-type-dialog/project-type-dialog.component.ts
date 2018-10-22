import { Component, Inject, OnInit, Optional } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material';
import { ProjectType } from '../../../models/project-type';

@Component({
  selector: 'app-project-type-dialog',
  templateUrl: './project-type-dialog.component.html',
  styleUrls: ['./project-type-dialog.component.scss']
})
export class ProjectTypeDialogComponent implements OnInit {

  public projectType: ProjectType;

  public selectedName: string;
  public selectedDescription: string;
  public selectedTypeCode: string;

  constructor(public dialogRef: MatDialogRef<ProjectTypeDialogComponent>,
    @Optional() @Inject(MAT_DIALOG_DATA) public data: ProjectType) {

    if (data) {
      this.projectType = data;

      this.selectedName = this.projectType.name;
      this.selectedDescription = this.projectType.description;
      this.selectedTypeCode = this.projectType.typeCode;
    }
  }

  ngOnInit() {
  }

  close(): void {
    this.dialogRef.close();
  }

  getResult() {
    const projectTypeResult = new ProjectType();

    if (this.projectType) {
      projectTypeResult.id = this.projectType.id;
    }

    projectTypeResult.name = this.selectedName;
    projectTypeResult.typeCode = this.selectedTypeCode;
    projectTypeResult.description = this.selectedDescription;

    return projectTypeResult;
  }

}

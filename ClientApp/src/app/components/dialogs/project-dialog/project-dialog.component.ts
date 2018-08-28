import { Component, OnInit, Inject, Optional } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material';
import { Project } from '../../../models/project';
import { ProjectType } from '../../../models/project-type';
import { ProjectTypeService } from '../../../services/project-type/project-type.service';
import { FormControl, Validators, FormGroup } from '@angular/forms';

@Component({
  selector: 'app-project-dialog',
  templateUrl: './project-dialog.component.html',
  styleUrls: ['./project-dialog.component.scss']
})
export class ProjectDialogComponent implements OnInit {

  public project: Project;
  public projectTypes: ProjectType[];

  public selectedTypeValue: number;

  constructor(
    public dialogRef: MatDialogRef<ProjectDialogComponent>,
    @Optional() @Inject(MAT_DIALOG_DATA) public data: Project,
    public projectTypeService: ProjectTypeService) {

      if (data) {
        this.project = data;
      }
  }

  projectFromGroup = new FormGroup({

    nameFormControl: new FormControl('', [
      Validators.required,
      Validators.minLength(4),
    ]),

    projectTypeFormControl: new FormControl('', [
      Validators.required,
    ]),

    repositoryUrlFormControl: new FormControl('', [
    ]),

    descriptionFormControl: new FormControl('', [
    ])

  });

  ngOnInit() {
    this.getProjectTypes();

    if (this.project) {

      this.projectFromGroup.controls['nameFormControl'].setValue(this.project.name);
      this.projectFromGroup.controls['descriptionFormControl'].setValue(this.project.description);
      this.projectFromGroup.controls['repositoryUrlFormControl'].setValue(this.project.repositoryUrl);
      this.projectFromGroup.controls['projectTypeFormControl'].setValue(this.project.projectTypeId);
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
      projectResult.projectId = this.project.projectId;
    }

    projectResult.name = this.projectFromGroup.controls['nameFormControl'].value;
    projectResult.description = this.projectFromGroup.controls['descriptionFormControl'].value;
    projectResult.repositoryUrl = this.projectFromGroup.controls['repositoryUrlFormControl'].value;
    projectResult.projectTypeId = this.projectFromGroup.controls['projectTypeFormControl'].value;

    return projectResult;
  }

  getProjectTypes(): void {
    this.projectTypeService.getProjectTypes()
      .subscribe(projectTypes => this.projectTypes = projectTypes);
  }

  getProjectTypeName(project: Project): string {
    if (!project.projectTypeId || !this.projectTypes) { return; }
    return this.projectTypes.filter(item => item.id === project.projectTypeId)[0].name;
  }

}

import { Component, OnInit, Inject, Optional } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material';
import { Project } from '../../../../models/project';
import { ProjectType } from '../../../../models/project-type';
import { ProjectTypeService } from '../../../../services/project-type/project-type.service';

@Component({
  selector: 'app-project-dialog',
  templateUrl: './project-dialog.component.html',
  styleUrls: ['./project-dialog.component.scss']
})
export class ProjectDialogComponent implements OnInit {

  public project: Project;
  public projectTypes: ProjectType[];

  public selectedProjectType: string;
  public selectedTypeId: number;
  public selectedName: string;
  public selectedDescription: string;

  constructor(
    public dialogRef: MatDialogRef<ProjectDialogComponent>,
    @Optional() @Inject(MAT_DIALOG_DATA) public data: Project,
    public projectTypeService: ProjectTypeService) {

      if (data) {
        this.project = data;

        this.selectedName = this.project.name;
        this.selectedDescription = this.project.description;
        this.selectedTypeId = this.project.projectTypeId;
      }

      console.log(data);
  }

  close(): void {
    this.dialogRef.close();
  }

  getResult() {
    const projectResult = new Project();

    if (this.project) {
      projectResult.projectId = this.project.projectId;
    }

    projectResult.name = this.selectedName;
    projectResult.description = this.selectedDescription;
    projectResult.projectTypeId = this.selectedTypeId;

    return projectResult;
  }

  ngOnInit() {
    this.getProjectTypes();
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

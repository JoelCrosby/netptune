import { Component, OnInit } from '@angular/core';
import { ProjectType } from '../../models/project-type';
import { ProjectTypeService } from '../../services/project-type/project-type.service';
import { AlertService } from '../../services/alert/alert.service';
import { MatSnackBar, MatDialog } from '@angular/material';
import { ProjectTypeDialogComponent } from '../dialogs/project-type-dialog/project-type-dialog.component';
import { ConfirmDialogComponent } from '../dialogs/confirm-dialog/confirm-dialog.component';
import { dropIn } from '../../animations';

@Component({
  selector: 'app-project-types',
  templateUrl: './project-types.component.html',
  styleUrls: ['./project-types.component.scss'],
  animations: [dropIn]
})
export class ProjectTypesComponent implements OnInit {

  public showAddForm = false;

  closeResult: string;
  selectedProjectType: ProjectType;

  constructor(
    public projectTypeService: ProjectTypeService,
    private alertsService: AlertService,
    public snackBar: MatSnackBar,
    public dialog: MatDialog) {

  }

  ngOnInit() {
    this.projectTypeService.refreshProjectTypes();
  }

  trackById(index: number, project: ProjectType) {
    return project.id;
  }

  showAddModal(): void {
    this.open();
  }

  showUpdateModal(projectType: ProjectType): void {
    if (projectType == null) { return; }

    this.selectedProjectType = projectType;
    this.open(this.selectedProjectType);
  }

  deleteClicked(projectType: ProjectType) {

    const dialogRef = this.dialog.open(ConfirmDialogComponent, {
      width: '500px',
      data: {
        title: 'Remove Project Type ' + projectType.name,
        content: `Are you sure you wish to remove ${projectType.name}?`,
        confirm: 'Remove'
      }
    });

    dialogRef.afterClosed().subscribe((result: Boolean) => {

      if (result) {
        this.projectTypeService.deleteProjectType(projectType)
          .subscribe((data: ProjectType) => {
            projectType = data;
            this.projectTypeService.refreshProjectTypes();
            this.snackBar.open('Workspace Removed.', 'Undo', {
              duration: 3000,
            });

          }, error => {
            this.snackBar.open('An error occured while trying to remove project type ' + error, null, {
              duration: 2000,
            });
          });
      }

      this.clearModalValues();
    });
  }

  addProjectType(projectType: ProjectType): void {
    this.projectTypeService.addProjectType(projectType)
      .subscribe((projectTypeResult) => {
        if (projectTypeResult) {
          this.projectTypeService.refreshProjectTypes();
          this.alertsService.changeSuccessMessage('Project added!');
        }
      }, error => {
        this.alertsService.
          changeErrorMessage('An error occured while trying to create the Project. ' + error);
      });
  }

  getFaIcon(projectType: ProjectType): string {

    switch (projectType.typeCode) {
      case 'node':
        return 'fab fa-node-js';
      case 'angular':
        return 'fab fa-angular';
      case 'winforms':
        return 'fab fa-windows';
      case 'aspcore':
        return 'fas fa-code';
      default:
        return 'fas fa-chalkboard';
    }
  }

  updateProjectType(projectType: ProjectType): void {
    this.projectTypeService.updateProjectType(projectType)
      .subscribe(result => {
        projectType = result;
        this.projectTypeService.refreshProjectTypes();
        this.alertsService.changeSuccessMessage('Project updated!');
      });
  }

  open(projectType?: ProjectType) {

    const dialogRef = this.dialog.open(ProjectTypeDialogComponent, {
      width: '500px',
      data: projectType
    });

    dialogRef.afterClosed().subscribe((result: ProjectType) => {

      if (!result) { return; }

      if (this.selectedProjectType) {
        const newProjectType = new ProjectType();
        newProjectType.id = this.selectedProjectType.id;
        newProjectType.name = result.name;
        newProjectType.description = result.description;
        newProjectType.typeCode = result.typeCode;
        this.updateProjectType(newProjectType);
      } else {
        const newProjectType = new ProjectType();
        newProjectType.name = result.name;
        newProjectType.description = result.description;
        newProjectType.typeCode = result.typeCode;
        this.addProjectType(newProjectType);
      }

      this.clearModalValues();

    });
  }

  clearModalValues(): void {
    // finally clear selecetd project
    this.selectedProjectType = null;
  }

}

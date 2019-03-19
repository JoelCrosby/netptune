import { Component } from '@angular/core';
import { MatSnackBar } from '@angular/material';
import { MatDialog } from '@angular/material/dialog';
import { dropIn } from '@app/core/animations/animations';
import { Project } from '@app/core/models/project';

@Component({
  selector: 'app-projects',
  templateUrl: './projects.index.component.html',
  styleUrls: ['./projects.index.component.scss'],
  animations: [dropIn],
})
export class ProjectsComponent {
  projects$ = [];

  constructor(public snackBar: MatSnackBar, public dialog: MatDialog) {}

  trackById(index: number, project: Project) {
    return project.id;
  }
}

import { Component, OnInit } from '@angular/core';
import { MatSnackBar } from '@angular/material';
import { MatDialog } from '@angular/material/dialog';
import { dropIn } from '@core/animations/animations';
import { AppState } from '@core/core.state';
import { Project } from '@core/models/project';
import { Store } from '@ngrx/store';
import { ActionLoadProjects } from '../store/projects.actions';
import { selectProjects } from '../store/projects.selectors';
import { ProjectDialogComponent } from '@app/shared/dialogs/project-dialog/project-dialog.component';
import { AppUser } from '@core/models/appuser';
import { UsernameConverter } from '@core/models/converters/username.converter';

@Component({
  selector: 'app-projects',
  templateUrl: './projects.index.component.html',
  styleUrls: ['./projects.index.component.scss'],
  animations: [dropIn],
})
export class ProjectsComponent implements OnInit {
  projects$ = this.store.select(selectProjects);

  constructor(
    public snackBar: MatSnackBar,
    public dialog: MatDialog,
    private store: Store<AppState>
  ) {}

  ngOnInit() {
    this.store.dispatch(new ActionLoadProjects());
  }

  trackById(index: number, project: Project) {
    return project.id;
  }

  showAddModal() {
    this.dialog.open<ProjectDialogComponent>(ProjectDialogComponent);
  }

  toDisplay(user: AppUser) {
    return UsernameConverter.toDisplay(user);
  }
}

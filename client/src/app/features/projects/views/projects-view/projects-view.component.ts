import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
} from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { loadProjects } from '@core/store/projects/projects.actions';
import { selectProjectsLoading } from '@core/store/projects/projects.selectors';
import { ProjectDialogComponent } from '@entry/dialogs/project-dialog/project-dialog.component';
import { Store } from '@ngrx/store';

@Component({
  templateUrl: './projects-view.component.html',
  styleUrls: ['./projects-view.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProjectsViewComponent implements AfterViewInit {
  loading$ = this.store.select(selectProjectsLoading);

  constructor(public dialog: MatDialog, private store: Store) {}

  ngAfterViewInit() {
    this.store.dispatch(loadProjects());
  }

  showAddModal() {
    this.dialog.open(ProjectDialogComponent);
  }
}

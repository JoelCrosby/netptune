import { AfterViewInit, ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { DialogService } from '@core/services/dialog.service';
import { loadProjects } from '@core/store/projects/projects.actions';
import { selectProjectsLoading } from '@core/store/projects/projects.selectors';
import { ProjectDialogComponent } from '@entry/dialogs/project-dialog/project-dialog.component';
import { Store } from '@ngrx/store';
import { debounceTime } from 'rxjs/operators';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';
import { AsyncPipe } from '@angular/common';
import { MatProgressSpinner } from '@angular/material/progress-spinner';
import { ProjectListComponent } from '@projects/components/project-list/project-list.component';

@Component({
  templateUrl: './projects-view.component.html',
  styleUrls: ['./projects-view.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    PageContainerComponent,
    PageHeaderComponent,
    MatProgressSpinner,
    ProjectListComponent,
    AsyncPipe
],
})
export class ProjectsViewComponent implements AfterViewInit {
  dialog = inject(DialogService);
  private store = inject(Store);

  loading$ = this.store.select(selectProjectsLoading).pipe(debounceTime(200));

  ngAfterViewInit() {
    this.store.dispatch(loadProjects());
  }

  showAddModal() {
    this.dialog.open(ProjectDialogComponent, { width: '512px' });
  }
}

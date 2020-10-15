import {
  ChangeDetectionStrategy,
  Component,
  Input,
  OnInit,
} from '@angular/core';
import { ProjectViewModel } from '@core/models/view-models/project-view-model';
import { deleteProject } from '@core/store/projects/projects.actions';
import { selectCurrentWorkspaceIdentifier } from '@core/store/workspaces/workspaces.selectors';
import { HeaderAction } from '@core/types/header-action';
import { Store } from '@ngrx/store';
import { Observable } from 'rxjs';
import { filter, first, map } from 'rxjs/operators';

@Component({
  selector: 'app-project-list-item',
  templateUrl: './project-list-item.component.html',
  styleUrls: ['./project-list-item.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProjectListItemComponent implements OnInit {
  @Input() project: ProjectViewModel;

  actions$: Observable<HeaderAction[]>;

  constructor(private store: Store) {}

  ngOnInit() {
    this.actions$ = this.store.select(selectCurrentWorkspaceIdentifier).pipe(
      filter((val) => !!val),
      first(),
      map((identifier) => {
        return [
          {
            label: 'Go To Board',
            isLink: true,
            icon: 'assessment',
            routerLink: [
              '/',
              identifier,
              'boards',
              this.project.defaultBoardIdentifier,
            ],
          },
        ];
      })
    );
  }

  onDeleteClicked() {
    this.store.dispatch(deleteProject({ project: this.project }));
  }
}

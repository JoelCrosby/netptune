import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  input,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { ProjectViewModel } from '@core/models/view-models/project-view-model';
import { deleteProject } from '@core/store/projects/projects.actions';
import { selectCurrentWorkspaceIdentifier } from '@core/store/workspaces/workspaces.selectors';
import { Store } from '@ngrx/store';
import { CardListItemComponent } from '@static/components/card-list-item/card-list-item.component';

@Component({
  selector: 'app-project-list-item',
  templateUrl: './project-list-item.component.html',
  styleUrls: ['./project-list-item.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, CardListItemComponent],
})
export class ProjectListItemComponent {
  private store = inject(Store);
  readonly project = input.required<ProjectViewModel>();

  workspaceId = this.store.selectSignal(selectCurrentWorkspaceIdentifier);

  actions = computed(() => {
    const identifier = this.workspaceId();

    if (!identifier) return [];

    return [
      {
        label: 'Go To Board',
        isLink: true,
        icon: 'assessment',
        routerLink: [
          '/',
          identifier,
          'boards',
          this.project().defaultBoardIdentifier,
        ],
      },
    ];
  });

  onDeleteClicked() {
    this.store.dispatch(deleteProject({ project: this.project() }));
  }
}

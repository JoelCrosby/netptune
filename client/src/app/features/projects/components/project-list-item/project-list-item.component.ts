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
import { CardListItemComponent } from '@app/static/components/card/card-list-item.component';
import { LucidePanelsTopLeft } from '@lucide/angular';
import { netptunePermissions } from '@app/core/auth/permissions';
import { selectHasPermission } from '@app/core/auth/store/auth.selectors';

@Component({
  selector: 'app-project-list-item',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, CardListItemComponent],
  template: `
    <a [routerLink]="canUpdateProjects() ? ['.', project().key] : null">
      <app-card-list-item
        [title]="project().name"
        [description]="project().description"
        [actions]="actions()"
        [canDelete]="candDeleteProjects()"
        (delete)="onDeleteClicked()">
        <div class="flex flex-col">
          @if (project().repositoryUrl) {
            <h5 class="mt-4 mb-[0.4rem]">Repository</h5>

            <a
              [href]="project().repositoryUrl"
              class="block max-w-full overflow-hidden py-[0.6rem] text-ellipsis whitespace-nowrap underline">
              {{ project().repositoryUrl }}
            </a>
          }
        </div>
      </app-card-list-item>
    </a>
  `,
})
export class ProjectListItemComponent {
  private store = inject(Store);
  readonly project = input.required<ProjectViewModel>();

  workspaceId = this.store.selectSignal(selectCurrentWorkspaceIdentifier);

  candDeleteProjects = this.store.selectSignal(
    selectHasPermission(netptunePermissions.projects.delete)
  );

  canUpdateProjects = this.store.selectSignal(
    selectHasPermission(netptunePermissions.projects.update)
  );

  actions = computed(() => {
    const identifier = this.workspaceId();

    if (!identifier) return [];

    return [
      {
        label: 'Go To Board',
        isLink: true,
        icon: LucidePanelsTopLeft,
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

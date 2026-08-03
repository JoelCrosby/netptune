import { DatePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { Params, Router, RouterLink } from '@angular/router';
import { netptunePermissions } from '@app/core/auth/permissions';
import { selectHasPermission } from '@app/core/store/auth/auth.selectors';
import { ProjectViewModel } from '@core/models/view-models/project-view-model';
import { DialogService } from '@core/services/dialog.service';
import {
  deleteProject,
  loadProjects,
} from '@core/store/projects/projects.actions';
import {
  selectAllProjects,
  selectProjectsLoading,
} from '@core/store/projects/projects.selectors';
import { selectCurrentWorkspaceIdentifier } from '@core/store/workspaces/workspaces.selectors';
import { dispatchForWorkspace } from '@core/util/dispatch-for-workspace';
import { ProjectDialogComponent } from '@entry/dialogs/project-dialog/project-dialog.component';
import {
  LucideFolderOpen,
  LucidePanelsTopLeft,
  LucidePlus,
  LucideTrash2,
} from '@lucide/angular';
import { Store } from '@ngrx/store';
import { AvatarComponent } from '@static/components/avatar/avatar.component';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { DatatableCellTemplateDirective } from '@static/components/datatable/datatable-cell-template.directive';
import { DatatableComponent } from '@static/components/datatable/datatable.component';
import {
  DatatableColumn,
  DatatableDataSource,
  DatatableMenuItem,
} from '@static/components/datatable/datatable.types';
import { DatatableEmptyDirective } from '@static/components/datatable/datatable-empty.directive';
import { EmptyStateComponent } from '@static/components/empty-state/empty-state.component';
import { PageContainerComponent } from '@static/components/page-container/page-container.component';
import { PageHeaderComponent } from '@static/components/page-header/page-header.component';

@Component({
  selector: 'app-projects-view',
  imports: [
    DatePipe,
    RouterLink,
    PageContainerComponent,
    PageHeaderComponent,
    AvatarComponent,
    DatatableComponent,
    DatatableCellTemplateDirective,
    DatatableEmptyDirective,
    EmptyStateComponent,
    FlatButtonComponent,
    LucideFolderOpen,
    LucidePlus,
  ],
  template: `
    <app-page-container [centerPage]="true" [marginBottom]="true">
      @if (canCreateProjects()) {
        <app-page-header
          i18n-title="Page title for the project list"
          title="Projects"
          i18n-actionTitle="Button that opens the create-project dialog"
          actionTitle="Create Project"
          [count]="count()"
          (actionClick)="showAddModal()" />
      } @else {
        <app-page-header
          i18n-title="Page title for the project list"
          title="Projects"
          [count]="count()" />
      }

      <app-datatable
        i18n-errorMessage="Shown when the project list fails to load"
        errorMessage="Projects could not be loaded."
        i18n-itemLabel="
          Plural noun for the rows of the project table, shown beside counts
        "
        itemLabel="projects"
        containerClass="h-[calc(100vh-253px)] min-h-80 overflow-auto"
        tableClass="min-w-[900px] table-fixed"
        [data]="data()"
        [customizableColumns]="true"
        [stickyHeader]="true">
        <ng-template appDatatableCell="name" let-project>
          @let link = projectLink(project);

          <a
            class="block truncate font-medium hover:underline"
            [routerLink]="link"
            [class.pointer-events-none]="!link">
            {{ project.name }}
          </a>
        </ng-template>

        <ng-template appDatatableCell="key" let-project>
          <span
            class="bg-foreground/5 rounded px-1.5 py-0.5 font-mono text-xs uppercase">
            {{ project.key }}
          </span>
        </ng-template>

        <ng-template appDatatableCell="description" let-project>
          @if (project.description) {
            <span class="block truncate text-sm">{{
              project.description
            }}</span>
          } @else {
            <span class="text-muted text-sm">&mdash;</span>
          }
        </ng-template>

        <ng-template appDatatableCell="owner" let-project>
          <div class="flex min-w-0 items-center gap-2">
            <app-avatar
              class="flex-none"
              size="sm"
              [name]="project.ownerDisplayName"
              [imageUrl]="project.ownerPictureUrl" />
            <span class="min-w-0 truncate text-sm">
              {{ project.ownerDisplayName }}
            </span>
          </div>
        </ng-template>

        <ng-template appDatatableCell="repositoryUrl" let-project>
          @if (project.repositoryUrl) {
            <a
              class="block truncate text-sm underline"
              target="_blank"
              rel="noreferrer noopener"
              [href]="project.repositoryUrl">
              {{ project.repositoryUrl }}
            </a>
          } @else {
            <span class="text-muted text-sm">&mdash;</span>
          }
        </ng-template>

        <ng-template appDatatableCell="updatedAt" let-project>
          <span class="text-muted text-sm whitespace-nowrap">
            {{ project.updatedAt ?? project.createdAt | date: 'mediumDate' }}
          </span>
        </ng-template>

        <app-empty-state
          appDatatableEmpty
          compact
          i18n-title="Heading of the empty project list"
          title="There are currently no projects."
          i18n-description="
            Explains what a project is for, on the empty project list
          "
          description="Create your first project to organise related boards and tasks.">
          <svg emptyStateIcon size="38" lucideFolderOpen></svg>

          @if (canCreateProjects()) {
            <button
              emptyStateAction
              app-flat-button
              type="button"
              (click)="showAddModal()">
              <svg size="20" lucidePlus></svg>
              <span i18n="Button that opens the create-project dialog">
                Create Project
              </span>
            </button>
          }
        </app-empty-state>
      </app-datatable>
    </app-page-container>
  `,
})
export class ProjectsViewComponent {
  private dialog = inject(DialogService);
  private router = inject(Router);
  private store = inject(Store);

  readonly loading = this.store.selectSignal(selectProjectsLoading);
  readonly projects = this.store.selectSignal(selectAllProjects);
  readonly workspaceId = this.store.selectSignal(
    selectCurrentWorkspaceIdentifier
  );

  readonly count = computed(() => {
    return this.loading() ? null : this.projects().length;
  });

  readonly canCreateProjects = this.store.selectSignal(
    selectHasPermission(netptunePermissions.projects.create)
  );

  readonly canUpdateProjects = this.store.selectSignal(
    selectHasPermission(netptunePermissions.projects.update)
  );

  readonly canDeleteProjects = this.store.selectSignal(
    selectHasPermission(netptunePermissions.projects.delete)
  );

  private readonly params = signal<Params>({});

  private readonly columns: DatatableColumn<ProjectViewModel>[] = [
    { id: 'name', header: 'Name', accessor: 'name', sortable: true },
    { id: 'key', header: 'Key', sortable: true, widthClass: 'w-24' },
    { id: 'description', header: 'Description', sortable: true },
    { id: 'owner', header: 'Owner', sortable: true, widthClass: 'w-48' },
    { id: 'repositoryUrl', header: 'Repository', widthClass: 'w-56' },
    { id: 'updatedAt', header: 'Updated', sortable: true, widthClass: 'w-36' },
  ];

  private readonly goToBoardItem: DatatableMenuItem<ProjectViewModel> = {
    label: $localize`:Label shown in the interface:Go To Board`,
    icon: LucidePanelsTopLeft,
    onClick: (project) => this.onGoToBoard(project),
  };

  private readonly deleteItem: DatatableMenuItem<ProjectViewModel> = {
    label: $localize`:Row action that deletes the project:Delete`,
    icon: LucideTrash2,
    onClick: (project) => this.onDelete(project),
  };

  readonly data = computed<DatatableDataSource<ProjectViewModel>>(() => {
    const menu = this.canDeleteProjects()
      ? [this.goToBoardItem, this.deleteItem]
      : [this.goToBoardItem];

    return {
      key: 'project-list',
      columns: this.columns,
      resource: { url: 'api/projects', params: this.params },
      rows: (response) => {
        return Array.isArray(response) ? (response as ProjectViewModel[]) : [];
      },
      trackBy: (_: number, project: ProjectViewModel) => project.id,
      menu,
      reloadSignal: this.projects,
    };
  });

  constructor() {
    dispatchForWorkspace(() => loadProjects.init());
  }

  projectLink(project: ProjectViewModel) {
    return this.canUpdateProjects() ? ['.', project.key] : null;
  }

  showAddModal() {
    this.dialog.openWizard(ProjectDialogComponent, {
      title: $localize`:Title of a dialog or section:Create Project`,
      width: '720px',
    });
  }

  onGoToBoard(project: ProjectViewModel) {
    const identifier = this.workspaceId();

    if (!identifier) return;

    this.router.navigate([
      '/',
      identifier,
      'boards',
      project.defaultBoardIdentifier,
    ]);
  }

  onDelete(project: ProjectViewModel) {
    this.store.dispatch(deleteProject.init({ project }));
  }
}

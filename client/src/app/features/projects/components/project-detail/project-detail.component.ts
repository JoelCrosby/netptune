import { Component, inject, input, linkedSignal } from '@angular/core';
import {
  apply,
  disabled,
  form,
  FormField,
  maxLength,
  required,
  submit,
} from '@angular/forms/signals';
import { ProjectViewModel } from '@app/core/models/view-models/project-view-model';
import { FlatButtonComponent } from '@app/static/components/button/flat-button.component';
import { UpdateProjectRequest } from '@core/models/requests/upadte-project-request';
import { ProjectCommandsService } from '@core/services/project-commands.service';
import { statusResource } from '@core/resources/status.resource';
import { LucideFolderOpen } from '@lucide/angular';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { ProjectBrandingComponent } from '@projects/components/project-branding/project-branding.component';
import { FormSelectOptionComponent } from '@static/components/form-select/form-select-option.component';
import { FormSelectComponent } from '@static/components/form-select/form-select.component';
import { FormTextAreaComponent } from '@static/components/form-textarea/form-textarea.component';
import { NotificationSubscribeComponent } from '@shared/components/notification-subscribe/notification-subscribe.component';
import { NotificationScope } from '@core/models/notification-subscription';
import { requiredTextSchema } from '@core/util/forms/validation.schemas';
import { PanelComponent } from '@static/components/panel.component';
import { PanelBodyComponent } from '@static/components/panel-body.component';
import { PanelFooterComponent } from '@static/components/panel-footer.component';
import { PanelHeaderComponent } from '@static/components/panel-header.component';

@Component({
  selector: 'app-project-detail',
  imports: [
    FlatButtonComponent,
    FormField,
    FormInputComponent,
    FormSelectComponent,
    FormSelectOptionComponent,
    FormTextAreaComponent,
    NotificationSubscribeComponent,
    PanelBodyComponent,
    PanelComponent,
    PanelFooterComponent,
    PanelHeaderComponent,
    ProjectBrandingComponent,
  ],
  host: { class: 'block' },
  template: `
    <form app-panel surface="card" (submit)="updateClicked($event)">
      <app-panel-header
        density="comfortable"
        [icon]="detailsIcon"
        i18n-heading="Section heading for the project detail form"
        heading="Project details"
        i18n-description="Explains what the project detail form controls"
        description="How this project is named, identified and where its new tasks start.">
        <div panelHeaderActions>
          @if (project(); as project) {
            <app-notification-subscribe
              class="shrink-0"
              [scope]="notificationScope.project"
              [scopeEntityId]="project.id"
              [scopeName]="project.name" />
          }
        </div>
      </app-panel-header>

      <app-panel-body class="grid gap-6 lg:grid-cols-2 lg:gap-10">
        <div class="grid max-w-2xl content-start gap-4">
          <app-form-input
            [formField]="projectForm.name"
            i18n-label="Label of the name field"
            label="Name"
            maxLength="1024" />

          <app-form-textarea
            [formField]="projectForm.description"
            i18n-label="Label of the description field"
            label="Description"
            rows="6" />

          <app-form-input
            class="max-w-64"
            [formField]="projectForm.key"
            i18n-label="Label of the project key field"
            label="Project ID"
            maxLength="6"
            i18n-hint="
              Explains where the project key appears and what it may contain
            "
            hint="Shown as the first part of every task ID. Up to 6 characters, unique to this workspace." />

          <app-form-input
            [formField]="projectForm.repositoryUrl"
            i18n-label="Label of the source repository URL field"
            label="Repository URL"
            maxLength="1024" />

          @if (statuses.value()) {
            <app-form-select
              [formField]="projectForm.defaultStatusId"
              i18n-label="Label of the default task status field"
              label="Default task status"
              i18n-hint="Explains what the default task status field controls"
              hint="New tasks in this project start with this status.">
              @for (status of statuses.value(); track status.id) {
                <app-form-select-option [value]="status.id">
                  {{ status.name }}
                </app-form-select-option>
              }
            </app-form-select>
          }
        </div>

        <app-project-branding [project]="project()" />
      </app-panel-body>

      <app-panel-footer>
        <button
          app-flat-button
          type="submit"
          [disabled]="projectForm().disabled()">
          <span i18n="Button that saves the project details">Save Changes</span>
        </button>
      </app-panel-footer>
    </form>
  `,
})
export class ProjectDetailComponent {
  private projectCommands = inject(ProjectCommandsService);

  protected readonly detailsIcon = LucideFolderOpen;
  protected readonly notificationScope = NotificationScope;

  project = input<ProjectViewModel>();
  loading = this.projectCommands.isUpdating;
  statuses = statusResource();

  projectFormModel = linkedSignal(() => ({
    id: this.project()?.id ?? (null as number | null),
    key: this.project()?.key ?? '',
    name: this.project()?.name ?? '',
    description: this.project()?.description ?? '',
    repositoryUrl: this.project()?.repositoryUrl ?? '',
    defaultStatusId: this.project()?.defaultStatusId ?? 0,
  }));

  projectForm = form(this.projectFormModel, (schema) => {
    required(schema.id);
    apply(
      schema.key,
      requiredTextSchema({
        label: $localize`:Label shown in the interface:Project ID`,
        maxLength: 6,
      })
    );
    apply(
      schema.name,
      requiredTextSchema({
        label: $localize`:Label shown in the interface:Name`,
        maxLength: 128,
      })
    );
    required(schema.defaultStatusId);
    maxLength(schema.description, 4096);
    maxLength(schema.repositoryUrl, 1024);
    disabled(schema, { when: () => this.loading() });
  });

  updateClicked(event: Event) {
    event.preventDefault();

    submit(this.projectForm, async () => {
      const { name, description, repositoryUrl, key, defaultStatusId } =
        this.projectForm;
      const id = this.projectForm.id().value();

      if (id === null || id === undefined) return;

      const project: UpdateProjectRequest = {
        id,
        name: name().value().trim(),
        description: description().value().trim(),
        repositoryUrl: repositoryUrl().value().trim(),
        key: key().value().trim(),
        defaultStatusId: defaultStatusId().value() || null,
      };

      this.projectCommands.update(project);
    });
  }
}

import { DialogRef } from '@angular/cdk/dialog';
import { Component, computed, inject, signal } from '@angular/core';
import {
  apply,
  FormField,
  form,
  maxLength,
  submit,
} from '@angular/forms/signals';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { AddProjectRequest } from '@core/models/project';
import { createProject } from '@core/store/projects/projects.actions';
import { selectCurrentWorkspace } from '@core/store/workspaces/workspaces.selectors';
import { Store } from '@ngrx/store';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { FormTextAreaComponent } from '@static/components/form-textarea/form-textarea.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { SetupTemplatePickerComponent } from '../../components/setup-template-picker/setup-template-picker.component';
import { StepperComponent } from '@static/components/stepper/stepper.component';
import { StepComponent } from '@static/components/stepper/step.component';
import { WorkspaceSetupTemplatesService } from '@core/services/workspace-setup-templates.service';
import { SetupCreationSummaryComponent } from '../../components/setup-creation-summary/setup-creation-summary.component';
import { LucideChevronLeft, LucideChevronRight } from '@lucide/angular';
import { requiredTextSchema } from '@core/util/forms/validation.schemas';

@Component({
  selector: 'app-project-dialog',
  imports: [
    FormField,
    FormInputComponent,
    FormTextAreaComponent,
    DialogActionsDirective,
    FlatButtonComponent,
    StrokedButtonComponent,
    SetupTemplatePickerComponent,
    StepperComponent,
    StepComponent,
    SetupCreationSummaryComponent,
    LucideChevronLeft,
    LucideChevronRight,
  ],
  template: `
    <form app-dialog-content class="min-w-0">
      <app-stepper mode="wizard" [(activeIndex)]="currentStep">
        <app-step
          title="Project details"
          description="Describe the project and optionally link its repository.">
          <div class="form-auth">
            <app-form-input
              [formField]="projectForm.name"
              label="Name"
              maxLength="1024">
            </app-form-input>

            <app-form-input
              [formField]="projectForm.repositoryUrl"
              label="Repository URL"
              maxLength="1024">
            </app-form-input>

            <app-form-textarea
              [formField]="projectForm.description"
              label="Description"
              maxLength="4096">
            </app-form-textarea>
          </div>
        </app-step>

        <app-step
          title="Workflow setup"
          description="Choose the layout for the project's default board.">
          <app-setup-template-picker
            [selectedKey]="projectForm.templateKey().value()"
            (selectedKeyChange)="setTemplate($event)" />
        </app-step>

        <app-step title="Summary" description="Review what will be created.">
          <app-setup-creation-summary
            entityType="Project"
            secondaryLabel="Repository"
            [name]="projectForm.name().value()"
            [secondaryValue]="projectForm.repositoryUrl().value()"
            [templateKey]="projectForm.templateKey().value()"
            [template]="selectedTemplate()" />
        </app-step>
      </app-stepper>
    </form>

    <div app-dialog-actions>
      @if (currentStep() > 0) {
        <button app-stroked-button type="button" (click)="previousStep()">
          <svg lucideChevronLeft class="h-4 w-4" aria-hidden="true"></svg>
          Back
        </button>
      }
      @if (currentStep() < finalStep) {
        <button
          app-flat-button
          class="ml-auto"
          type="button"
          (click)="nextStep()">
          Next
          <svg lucideChevronRight class="h-4 w-4" aria-hidden="true"></svg>
        </button>
      } @else {
        <button
          app-flat-button
          class="ml-auto"
          type="button"
          (click)="getResult()">
          Create Project
        </button>
      }
    </div>
  `,
})
export class ProjectDialogComponent {
  private store = inject(Store);
  private setupTemplates = inject(WorkspaceSetupTemplatesService);
  dialogRef = inject<DialogRef<ProjectDialogComponent>>(DialogRef);
  currentStep = signal(0);
  readonly finalStep = 2;

  currentWorkspace = this.store.selectSignal(selectCurrentWorkspace);

  projectFormModel = signal({
    name: '',
    repositoryUrl: '',
    description: '',
    workspace: '',
    color: '',
    templateKey: 'software',
  });

  projectForm = form(this.projectFormModel, (schema) => {
    apply(
      schema.name,
      requiredTextSchema({ label: 'Name', minLength: 4, maxLength: 1024 })
    );
    maxLength(schema.repositoryUrl, 1024);
    maxLength(schema.description, 4096);
  });

  selectedTemplate = computed(() =>
    this.setupTemplates
      .templates()
      .find(
        (template) => template.key === this.projectForm.templateKey().value()
      )
  );

  nextStep() {
    if (this.currentStep() === 0 && this.projectForm().invalid()) {
      this.projectForm().markAsTouched();
      return;
    }

    this.currentStep.update((step) => Math.min(step + 1, this.finalStep));
  }

  previousStep() {
    this.currentStep.update((step) => Math.max(step - 1, 0));
  }

  getResult() {
    const workspace = this.currentWorkspace();

    if (!workspace?.slug) return;

    submit(this.projectForm, async () => {
      const { name, repositoryUrl, description, color, templateKey } =
        this.projectForm;

      const project: AddProjectRequest = {
        name: name().value().trim(),
        description: description().value().trim(),
        repositoryUrl: repositoryUrl().value().trim(),
        metaInfo: {
          color: color().value() as string,
        },
        templateKey: templateKey().value(),
      };

      this.store.dispatch(createProject.init({ project }));
      this.dialogRef.close();
    });
  }

  setTemplate(templateKey: string) {
    this.projectFormModel.update((model) => ({ ...model, templateKey }));
  }
}

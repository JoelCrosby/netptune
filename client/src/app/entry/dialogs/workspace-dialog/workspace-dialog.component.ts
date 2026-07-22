import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import {
  Component,
  computed,
  effect,
  inject,
  resource,
  signal,
} from '@angular/core';
import {
  debounce,
  disabled,
  apply,
  FormField,
  form,
  maxLength,
  required,
  submit,
  validateAsync,
} from '@angular/forms/signals';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { AddWorkspaceRequest } from '@core/models/requests/add-workspace-request';
import { UpdateWorkspaceRequest } from '@core/models/requests/update-workspace-request';
import { Workspace } from '@core/models/workspace';
import * as Actions from '@core/store/workspaces/workspaces.actions';
import { WorkspacesService } from '@core/store/workspaces/workspaces.service';
import { colorDictionary } from '@core/util/colors/colors';
import { toUrlSlug } from '@core/util/strings';
import { Store } from '@ngrx/store';
import { LucideCheck } from '@lucide/angular';
import { ColorSelectComponent } from '@static/components/color-select/color-select.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { FormTextAreaComponent } from '@static/components/form-textarea/form-textarea.component';
import { DialogTitleComponent } from '@static/components/dialog-title/dialog-title.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { DialogCloseDirective } from '@static/directives/dialog-close.directive';
import { firstValueFrom, map } from 'rxjs';
import { SetupTemplatePickerComponent } from '../../components/setup-template-picker/setup-template-picker.component';
import { StepperComponent } from '@static/components/stepper/stepper.component';
import { StepComponent } from '@static/components/stepper/step.component';
import { WorkspaceSetupTemplatesService } from '@core/services/workspace-setup-templates.service';
import { SetupCreationSummaryComponent } from '../../components/setup-creation-summary/setup-creation-summary.component';
import { LucideChevronLeft, LucideChevronRight } from '@lucide/angular';
import { selectEffectiveTheme } from '@core/store/settings/settings.selectors';
import { workspaceBrandVariables } from '@core/util/colors/workspace-branding';
import { requiredTextSchema } from '@core/util/forms/validation.schemas';

@Component({
  selector: 'app-workspace-dialog',
  host: {
    '[style.--primary-rgb]': 'primaryRgb()',
  },
  styles: `
    :host {
      --primary: rgba(var(--primary-rgb));
      --primary-selected: rgba(var(--primary-rgb), 0.6);
      --primary-selected-hover: rgba(var(--primary-rgb), 0.8);
      --card-selected: var(--primary);
    }
  `,
  imports: [
    DialogTitleComponent,
    FormInputComponent,
    FormTextAreaComponent,
    ColorSelectComponent,
    DialogActionsDirective,
    FlatButtonComponent,
    StrokedButtonComponent,
    DialogCloseDirective,
    FormField,
    SetupTemplatePickerComponent,
    StepperComponent,
    StepComponent,
    SetupCreationSummaryComponent,
    LucideChevronLeft,
    LucideChevronRight,
  ],
  template: `
    @if (isEditMode) {
      <app-dialog-title>Edit Workspace</app-dialog-title>

      <form app-dialog-content class="form-auth min-w-0">
        <app-form-input
          [formField]="dialogForm.name"
          label="Name"
          maxLength="1024" />

        <app-form-input
          [formField]="dialogForm.identifier"
          label="Identifier"
          maxLength="1024"
          [icon]="identifierIcon()"
          [loading]="dialogForm.identifier().pending()"
          hint="Used in workspace URLs." />

        <app-form-textarea
          [formField]="dialogForm.description"
          label="Description"
          maxLength="4096" />

        <app-color-select [formField]="dialogForm.color" label="Color" />
      </form>

      <div app-dialog-actions align="end">
        <button app-stroked-button app-dialog-close type="button">Close</button>
        <button app-flat-button type="button" (click)="getResult()">
          Save Changes
        </button>
      </div>
    } @else {
      <form app-dialog-content class="min-w-0">
        <app-stepper mode="wizard" [(activeIndex)]="currentStep">
          <app-step
            title="Workspace details"
            description="Name and identify the workspace.">
            <div class="form-auth">
              <app-form-input
                [formField]="dialogForm.name"
                label="Name"
                maxLength="1024" />

              <app-form-input
                [formField]="dialogForm.identifier"
                label="Identifier"
                maxLength="1024"
                [icon]="identifierIcon()"
                [loading]="dialogForm.identifier().pending()"
                hint="Used in workspace URLs." />

              <app-form-textarea
                [formField]="dialogForm.description"
                label="Description"
                maxLength="4096" />

              <app-color-select [formField]="dialogForm.color" label="Color" />
            </div>
          </app-step>

          <app-step
            title="Workflow setup"
            description="Choose the statuses, tags, relations, and board layout to start with.">
            <app-setup-template-picker
              [selectedKey]="dialogForm.templateKey().value()"
              (selectedKeyChange)="setTemplate($event)" />
          </app-step>

          <app-step title="Summary" description="Review what will be created.">
            <app-setup-creation-summary
              entityType="Workspace"
              secondaryLabel="Identifier"
              [name]="dialogForm.name().value()"
              [secondaryValue]="dialogForm.identifier().value()"
              [templateKey]="dialogForm.templateKey().value()"
              [template]="selectedTemplate()"
              [showWorkspaceDefaults]="true" />
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
            [disabled]="dialogForm().pending()"
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
            Save Workspace
          </button>
        }
      </div>
    }
  `,
})
export class WorkspaceDialogComponent {
  private store = inject(Store);
  private workspaceServcie = inject(WorkspacesService);
  private setupTemplates = inject(WorkspaceSetupTemplatesService);
  private theme = this.store.selectSignal(selectEffectiveTheme);

  dialogRef = inject<DialogRef<WorkspaceDialogComponent>>(DialogRef);
  data = inject<Workspace>(DIALOG_DATA, { optional: true });
  currentStep = signal(0);
  readonly finalStep = 2;

  dialogFormModel = signal({
    name: this.data?.name ?? '',
    identifier: this.data?.slug ?? '',
    description: this.data?.description ?? '',
    color: this.data?.metaInfo?.color ?? '',
    templateKey: 'software',
  });

  dialogForm = form(this.dialogFormModel, (schema) => {
    apply(schema.name, requiredTextSchema({ label: 'Name', maxLength: 1024 }));
    apply(
      schema.identifier,
      requiredTextSchema({
        label: 'Identifier',
        minLength: 4,
        maxLength: 1024,
      })
    );
    maxLength(schema.description, 4096);
    required(schema.color);
    disabled(schema.identifier, { when: () => this.isEditMode });
    debounce(schema.identifier, 600);
    validateAsync(schema.identifier, {
      params: ({ value }) => {
        const identifier = value();
        if (!identifier || identifier.length < 4) return undefined;
        return identifier;
      },
      factory: (params) =>
        resource({
          params: params,
          loader: ({ params }) => {
            const request = this.workspaceServcie
              .isSlugUnique(params)
              .pipe(map((response) => response?.payload?.isUnique ?? false));

            return firstValueFrom(request);
          },
        }),
      onSuccess: (isUnique) => {
        if (isUnique) {
          return undefined;
        }

        return {
          kind: 'identifierTaken',
          message: 'Identifier is already taken',
        };
      },
      onError: () => ({
        kind: 'networkError',
        message: 'Could not veify Identifier availability',
      }),
    });
  });

  readonly primaryRgb = computed(
    () =>
      workspaceBrandVariables(
        this.dialogForm.color().value(),
        this.theme() === 'dark'
      )['--primary-rgb']
  );

  selectedTemplate = computed(() =>
    this.setupTemplates
      .templates()
      .find(
        (template) => template.key === this.dialogForm.templateKey().value()
      )
  );

  identifierIcon = computed(() => {
    if (this.dialogForm.identifier().valid()) {
      return LucideCheck;
    }

    return null;
  });

  colors = colorDictionary();

  get isEditMode() {
    return !!this.data;
  }

  constructor() {
    effect(() => {
      if (this.data) return;

      const current = this.dialogForm.identifier().value();
      const name = this.dialogForm.name().value();
      const identifier = toUrlSlug(name);

      if (identifier === current) return;

      this.dialogFormModel.update((model) => {
        const name = model.name;
        const identifier = toUrlSlug(name);

        return { ...model, identifier };
      });
    });
  }

  getResult() {
    submit(this.dialogForm, async () => {
      if (this.isEditMode) {
        this.editWorkspace();
      } else {
        this.createWorkspace();
      }

      this.dialogRef.close();
    });
  }

  nextStep() {
    if (this.currentStep() === 0) {
      if (this.dialogForm().pending()) return;

      if (this.dialogForm().invalid()) {
        this.dialogForm().markAsTouched();
        return;
      }
    }

    this.currentStep.update((step) => Math.min(step + 1, this.finalStep));
  }

  previousStep() {
    this.currentStep.update((step) => Math.max(step - 1, 0));
  }

  editWorkspace() {
    const { name, identifier, description, color } = this.dialogForm;

    const request: UpdateWorkspaceRequest = {
      name: name().value().trim(),
      slug: identifier().value().trim(),
      description: description().value().trim(),
      metaInfo: {
        color: color().value(),
      },
    };

    this.store.dispatch(Actions.editWorkspace.init({ request }));
  }

  setTemplate(templateKey: string) {
    this.dialogFormModel.update((model) => ({ ...model, templateKey }));
  }

  createWorkspace() {
    const { name, identifier, description, color, templateKey } =
      this.dialogForm;

    const request: AddWorkspaceRequest = {
      name: name().value().trim(),
      slug: identifier().value().trim(),
      description: description().value().trim(),
      metaInfo: {
        color: color().value(),
      },
      templateKey: templateKey().value(),
    };

    this.store.dispatch(Actions.createWorkspace.init({ request }));
  }

  getColorLabel(value: string) {
    const obj = this.colors.find((color) => color.name === value);
    return obj?.label;
  }
}

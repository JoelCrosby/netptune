import { Location } from '@angular/common';
import {
  Component,
  computed,
  effect,
  inject,
  resource,
  signal,
  untracked,
} from '@angular/core';
import {
  apply,
  debounce,
  form,
  FormField,
  maxLength,
  submit,
  validateAsync,
} from '@angular/forms/signals';
import { Router } from '@angular/router';
import { AuthCommandsService } from '@core/services/auth-commands.service';
import { ThemeService } from '@core/services/theme.service';
import { WorkspaceCommandsService } from '@core/services/workspace-commands.service';
import { WorkspaceListService } from '@core/services/workspace-list.service';
import { WorkspaceSetupTemplatesService } from '@core/services/workspace-setup-templates.service';
import { WorkspacesService } from '@core/services/workspaces-api.service';
import { colorDictionary, fallbackColor } from '@core/util/colors/colors';
import { workspaceBrandVariables } from '@core/util/colors/workspace-branding';
import { requiredTextSchema } from '@core/util/forms/validation.schemas';
import { mutation } from '@core/util/mutation';
import { toUrlSlug } from '@core/util/strings';
import { SetupTemplatePickerComponent } from '@entry/components/setup-template-picker/setup-template-picker.component';
import {
  LucideChevronLeft,
  LucideChevronRight,
  LucideCheck,
  LucideCircleAlert,
} from '@lucide/angular';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@static/components/button/stroked-button.component';
import { CalloutComponent } from '@static/components/callout/callout.component';
import { ColorSelectComponent } from '@static/components/color-select/color-select.component';
import { FormControlShapeDirective } from '@static/components/form-control/form-control.directives';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { FormTextAreaComponent } from '@static/components/form-textarea/form-textarea.component';
import { FlowPageComponent } from '@static/components/flow-page/flow-page.component';
import { FlowStepListComponent } from '@static/components/flow-step-list/flow-step-list.component';
import { firstValueFrom, map } from 'rxjs';
import {
  workspaceFlowOptionalSteps,
  workspaceFlowSetupStep,
  workspaceFlowWorkflowStep,
} from '../../workspace-flow-steps';

type CreateStep = 'details' | 'workflow';

@Component({
  selector: 'app-create-workspace-view',
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
    CalloutComponent,
    ColorSelectComponent,
    FlatButtonComponent,
    FlowPageComponent,
    FlowStepListComponent,
    FormControlShapeDirective,
    FormField,
    FormInputComponent,
    FormTextAreaComponent,
    LucideChevronLeft,
    LucideChevronRight,
    SetupTemplatePickerComponent,
    StrokedButtonComponent,
  ],
  template: `
    <form
      appFormShape="rounded"
      novalidate
      (submit)="$event.preventDefault(); onSubmit()">
      <app-flow-page
        i18n-eyebrow="Eyebrow above the name of a workspace being created"
        eyebrow="Create workspace"
        i18n-railLabel="Accessible label of the create workspace steps list"
        railLabel="Workspace steps"
        [title]="title()"
        [titleMuted]="!workspaceName()"
        [position]="position()"
        [heading]="heading()"
        [description]="description()">
        <button
          flowPageActions
          app-stroked-button
          type="button"
          [disabled]="request.pending()"
          (click)="exit()">
          {{ exitLabel() }}
        </button>

        <button
          flowPageActions
          app-flat-button
          type="button"
          [disabled]="!canCreate()"
          (click)="create()">
          <span i18n="Button that creates the workspace">Create Workspace</span>
        </button>

        <app-flow-step-list
          flowPageRail
          [items]="railItems()"
          [selected]="step()"
          (selectStep)="selectStep($event)" />

        @if (request.error(); as message) {
          <app-callout
            flowPageNotice
            class="mb-5"
            color="warn"
            role="alert"
            [icon]="errorIcon">
            {{ message }}
          </app-callout>
        }

        @switch (step()) {
          @case ('details') {
            <div class="flex flex-col">
              <app-form-input
                [formField]="workspaceForm.name"
                i18n-label="Label of the name field"
                label="Name"
                maxLength="1024" />

              <app-form-input
                [formField]="workspaceForm.identifier"
                i18n-label="Label of the workspace URL identifier field"
                label="Identifier"
                maxLength="1024"
                [icon]="identifierIcon()"
                [loading]="workspaceForm.identifier().pending()"
                i18n-hint="
                  Hint explaining what the workspace identifier is used for
                "
                hint="Used in workspace URLs." />

              <app-form-textarea
                [formField]="workspaceForm.description"
                i18n-label="Label of the description field"
                label="Description"
                maxLength="4096" />

              <app-color-select
                [formField]="workspaceForm.color"
                i18n-label="Label of the colour picker field"
                label="Color" />
            </div>
          }
          @case ('workflow') {
            <app-setup-template-picker
              [selectedKey]="workspaceForm.templateKey().value()"
              (selectedKeyChange)="setTemplate($event)" />
          }
        }

        <ng-container flowPageFooter>
          @if (step() === 'workflow') {
            <button
              app-stroked-button
              class="gap-1.5"
              type="button"
              (click)="step.set('details')">
              <svg lucideChevronLeft class="h-3.5 w-3.5"></svg>
              <span i18n="Button that returns to the workspace setup step">
                Setup
              </span>
            </button>
          }

          <span class="flex-1"></span>

          @if (step() === 'details') {
            <button app-stroked-button class="gap-1.5" type="submit">
              <span i18n="Button that advances to the workflow step">
                Workflow
              </span>
              <svg lucideChevronRight class="h-3.5 w-3.5"></svg>
            </button>
          } @else {
            <button
              app-stroked-button
              class="gap-1.5"
              type="submit"
              [disabled]="!canCreate()">
              <span i18n="Button that creates the workspace"
                >Create Workspace</span
              >
              <svg lucideChevronRight class="h-3.5 w-3.5"></svg>
            </button>
          }
        </ng-container>
      </app-flow-page>
    </form>
  `,
})
export class CreateWorkspaceViewComponent {
  private readonly authCommands = inject(AuthCommandsService);
  private readonly location = inject(Location);
  private readonly router = inject(Router);
  private readonly setupTemplates = inject(WorkspaceSetupTemplatesService);
  private readonly theme = inject(ThemeService).theme;
  private readonly workspaceCommands = inject(WorkspaceCommandsService);
  private readonly workspaceList = inject(WorkspaceListService);
  private readonly workspacesApi = inject(WorkspacesService);

  protected readonly errorIcon = LucideCircleAlert;
  protected readonly step = signal<CreateStep>('details');

  protected readonly request = mutation({
    fallbackError: $localize`:Shown when creating a workspace fails:The workspace could not be created.`,
  });

  private readonly colors = colorDictionary();

  private readonly formModel = signal({
    name: '',
    identifier: '',
    description: '',
    color: fallbackColor as string,
    templateKey: 'software',
  });

  protected readonly workspaceForm = form(this.formModel, (schema) => {
    apply(
      schema.name,
      requiredTextSchema({
        label: $localize`:Label shown in the interface:Name`,
        maxLength: 1024,
      })
    );
    apply(
      schema.identifier,
      requiredTextSchema({
        label: $localize`:Label shown in the interface:Identifier`,
        minLength: 4,
        maxLength: 1024,
      })
    );
    maxLength(schema.description, 4096);
    debounce(schema.identifier, 600);
    validateAsync(schema.identifier, {
      params: ({ value }) => {
        const identifier = value();

        if (!identifier || identifier.length < 4) return undefined;

        return identifier;
      },
      factory: (params) => {
        return resource({
          params: params,
          loader: ({ params }) => {
            const request = this.workspacesApi
              .isSlugUnique(params)
              .pipe(map((response) => response?.payload?.isUnique ?? false));

            return firstValueFrom(request);
          },
        });
      },
      onSuccess: (isUnique) => {
        if (isUnique) {
          return undefined;
        }

        return {
          kind: 'identifierTaken',
          message: $localize`:Body of a dialog or validation message:Identifier is already taken`,
        };
      },
      onError: () => {
        return {
          kind: 'networkError',
          message: $localize`:Body of a dialog or validation message:Could not verify Identifier availability`,
        };
      },
    });
  });

  protected readonly primaryRgb = computed(() => {
    return workspaceBrandVariables(
      this.workspaceForm.color().value(),
      this.theme() === 'dark'
    )['--primary-rgb'];
  });

  protected readonly identifierIcon = computed(() => {
    return this.workspaceForm.identifier().valid() ? LucideCheck : null;
  });

  protected readonly canCreate = computed(() => {
    return !this.workspaceForm().pending() && !this.request.pending();
  });

  protected readonly workspaceName = computed(() => {
    return this.workspaceForm.name().value().trim();
  });

  protected readonly title = computed(() => {
    return (
      this.workspaceName() ||
      $localize`:Page title until the new workspace has a name:Untitled workspace`
    );
  });

  protected readonly position = computed(() => {
    const stepNumber = this.step() === 'details' ? 1 : 2;

    return $localize`:Position of the open step when creating a workspace. STEP is its number:Step ${stepNumber}:STEP:`;
  });

  protected readonly heading = computed(() => {
    return this.step() === 'details'
      ? $localize`:Title of the workspace details step:Workspace details`
      : $localize`:Title of the workflow setup step:Workflow setup`;
  });

  protected readonly description = computed(() => {
    return this.step() === 'details'
      ? $localize`:Description of the workspace details step:Name and identify the workspace.`
      : $localize`:Description of the workflow setup step:Choose the statuses, tags, relations, and board layout to start with.`;
  });

  private readonly selectedTemplate = computed(() => {
    const key = this.workspaceForm.templateKey().value();

    return this.setupTemplates.templates().find((item) => item.key === key);
  });

  protected readonly railItems = computed(() => {
    const color = this.workspaceForm.color().value();
    const colorLabel =
      this.colors.find((item) => item.name === color)?.label ?? color;

    return [
      workspaceFlowSetupStep({
        identifier: this.workspaceForm.identifier().value(),
        colorLabel,
        hasDescription: !!this.workspaceForm.description().value().trim(),
      }),
      workspaceFlowWorkflowStep(this.selectedTemplate()),
      ...workspaceFlowOptionalSteps(),
    ];
  });

  // Someone without a workspace has nowhere to go back to: the picker sends them straight here.
  private readonly hasNoWorkspaces = computed(() => {
    const isLoaded = this.workspaceList.loaded();

    return isLoaded && this.workspaceList.workspaces().length === 0;
  });

  protected readonly exitLabel = computed(() => {
    return this.hasNoWorkspaces()
      ? $localize`:Button that signs out instead of cancelling, for someone with no workspaces:Sign Out`
      : $localize`:Button that leaves the create workspace page without creating anything:Cancel`;
  });

  constructor() {
    effect(() => {
      const name = this.workspaceForm.name().value();
      const isIdentifierEdited = untracked(() => {
        return this.workspaceForm.identifier().dirty();
      });

      if (isIdentifierEdited) return;

      const identifier = toUrlSlug(name);

      this.formModel.update((model) => ({ ...model, identifier }));
    });
  }

  protected selectStep(key: string) {
    if (key === 'details' || key === 'workflow') {
      this.step.set(key);
    }
  }

  protected setTemplate(templateKey: string) {
    this.formModel.update((model) => ({ ...model, templateKey }));
  }

  protected exit() {
    if (this.hasNoWorkspaces()) {
      this.authCommands.logout();

      return;
    }

    const cameFromInsideTheApp =
      !!this.router.lastSuccessfulNavigation()?.previousNavigation;

    if (cameFromInsideTheApp) {
      this.location.back();

      return;
    }

    void this.router.navigate(['/workspaces']);
  }

  protected onSubmit() {
    if (this.step() === 'details') {
      this.step.set('workflow');

      return;
    }

    this.create();
  }

  protected create() {
    // submit() only flushes the root's debounce, so Enter would otherwise send the previous identifier.
    this.workspaceForm.identifier().markAsTouched();

    if (!this.canCreate()) return;

    void submit(this.workspaceForm, async () => this.send());

    const isNameInvalid = this.workspaceForm.name().invalid();
    const isIdentifierInvalid = this.workspaceForm.identifier().invalid();

    if (isNameInvalid || isIdentifierInvalid) {
      this.step.set('details');
    }
  }

  private send() {
    const { name, identifier, description, color, templateKey } =
      this.formModel();

    this.request.run(
      this.workspaceCommands.create({
        name: name.trim(),
        slug: identifier.trim(),
        description: description.trim(),
        metaInfo: { color },
        templateKey,
      }),
      {
        onSuccess: (workspace) => {
          void this.router.navigate(['/', workspace.slug, 'setup'], {
            queryParams: { workflow: templateKey },
            replaceUrl: true,
          });
        },
      }
    );
  }
}

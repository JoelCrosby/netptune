import { hostTimeZone } from '@core/util/dates';
import {
  Component,
  computed,
  effect,
  inject,
  resource,
  signal,
} from '@angular/core';
import { WorkspaceCommandsService } from '@core/services/workspace-commands.service';
import { CurrentWorkspaceService } from '@core/services/current-workspace.service';
import {
  apply,
  debounce,
  FormField,
  form,
  maxLength,
  required,
  submit,
  validateAsync,
} from '@angular/forms/signals';
import { UpdateWorkspaceRequest } from '@core/models/requests/update-workspace-request';
import { ConfirmationService } from '@core/services/confirmation.service';
import { ConfirmDialogOptions } from '@entry/dialogs/confirm-dialog/confirm-dialog.component';
import { WorkspacesService } from '@core/services/workspaces-api.service';
import {
  LucideBuilding2,
  LucideCheck,
  LucideTriangleAlert,
} from '@lucide/angular';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { ColorSelectComponent } from '@static/components/color-select/color-select.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { FormTextAreaComponent } from '@static/components/form-textarea/form-textarea.component';
import { IconTileComponent } from '@static/components/icon-tile.component';
import { WorkspaceBrandingComponent } from '@settings/components/workspace-branding/workspace-branding.component';
import { firstValueFrom, map } from 'rxjs';
import { requiredTextSchema } from '@core/util/forms/validation.schemas';

@Component({
  selector: 'app-workspace-details',
  imports: [
    FormField,
    FormInputComponent,
    FormTextAreaComponent,
    ColorSelectComponent,
    FlatButtonComponent,
    IconTileComponent,
    WorkspaceBrandingComponent,
  ],
  host: { class: 'block' },
  template: `
    <form
      class="border-border bg-card overflow-hidden rounded-lg border shadow-sm"
      (submit)="save($event)">
      <header class="border-border border-b px-6 py-5">
        <div class="flex min-w-0 items-center gap-3">
          <app-icon-tile [icon]="detailsIcon" />

          <div class="min-w-0">
            <h2
              class="font-overpass text-base font-semibold"
              i18n="Section heading for the workspace detail form">
              Workspace details
            </h2>
            <p
              class="text-muted mt-1 text-sm"
              i18n="Explains what the workspace detail form controls">
              How this workspace is named, described and addressed.
            </p>
          </div>
        </div>
      </header>

      <div class="grid gap-6 px-6 py-5 lg:grid-cols-2 lg:gap-10">
        <div class="grid max-w-2xl content-start gap-4">
          <app-form-input
            [formField]="detailsForm.name"
            i18n-label="Label of the name field"
            label="Name"
            maxLength="1024" />

          <app-form-input
            [formField]="detailsForm.identifier"
            i18n-label="Label of the workspace URL identifier field"
            label="Identifier"
            maxLength="1024"
            [icon]="identifierIcon()"
            [loading]="detailsForm.identifier().pending()"
            i18n-hint="
              Warns that changing the workspace identifier breaks existing links
            "
            hint="Changing the identifier changes the workspace URL and will break existing shared links." />

          <app-form-textarea
            [formField]="detailsForm.description"
            i18n-label="Label of the description field"
            label="Description"
            maxLength="4096" />

          <app-color-select
            [formField]="detailsForm.color"
            i18n-label="Label of the colour picker field"
            label="Color" />

          <app-form-input
            [formField]="detailsForm.timeZone"
            i18n-label="Label of the workspace time zone field"
            label="Timezone"
            i18n-hint="
              Hint under the time zone field. Europe/London is a literal IANA
              zone name
            "
            hint="Use an IANA timezone, for example Europe/London." />
        </div>

        <app-workspace-branding />
      </div>

      <footer class="border-border border-t px-6 py-4">
        <button app-flat-button type="submit">
          <span i18n="Button that saves the workspace details">
            Save Changes
          </span>
        </button>
      </footer>
    </form>
  `,
})
export class WorkspaceDetailsComponent {
  private workspaceCommands = inject(WorkspaceCommandsService);
  private workspaceService = inject(WorkspacesService);
  private confirmation = inject(ConfirmationService);

  protected readonly detailsIcon = LucideBuilding2;

  workspace = inject(CurrentWorkspaceService).workspace;

  detailsFormModel = signal({
    name: '',
    identifier: '',
    description: '',
    color: '',
    timeZone: 'UTC',
  });

  detailsForm = form(this.detailsFormModel, (schema) => {
    apply(
      schema.name,
      requiredTextSchema({
        label: $localize`:Field name used inside validation messages, e.g. "Name is required.":Name`,
        maxLength: 1024,
      })
    );
    apply(
      schema.identifier,
      requiredTextSchema({
        label: $localize`:Field name used inside validation messages, e.g. "Identifier is required.":Identifier`,
        minLength: 4,
        maxLength: 1024,
      })
    );
    maxLength(schema.description, 4096);
    required(schema.color);
    debounce(schema.identifier, 600);
    validateAsync(schema.identifier, {
      params: ({ value }) => {
        const identifier = value();
        if (!identifier || identifier.length < 4) return undefined;
        // The workspace keeping its own identifier is always valid.
        if (identifier === this.workspace()?.slug) return undefined;
        return identifier;
      },
      factory: (params) =>
        resource({
          params: params,
          loader: ({ params }) => {
            const request = this.workspaceService
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
          message: $localize`:Validation error when a workspace identifier is already in use:Identifier is already taken`,
        };
      },
      onError: () => ({
        kind: 'networkError',
        message: $localize`:Shown when the workspace identifier availability check fails:Could not verify Identifier availability`,
      }),
    });
  });

  identifierIcon = computed(() => {
    if (this.detailsForm.identifier().valid()) {
      return LucideCheck;
    }

    return null;
  });

  constructor() {
    // Keep the form in sync with the loaded workspace.
    effect(() => {
      const workspace = this.workspace();

      if (!workspace) return;

      this.detailsFormModel.set({
        name: workspace.name ?? '',
        identifier: workspace.slug ?? '',
        description: workspace.description ?? '',
        color: workspace.metaInfo?.color ?? '',
        timeZone: workspace.metaInfo?.timeZone ?? hostTimeZone(),
      });
    });
  }

  save(event: Event) {
    event.preventDefault();

    const currentSlug = this.workspace()?.slug;

    if (!currentSlug) return;

    submit(this.detailsForm, async () => {
      const { name, identifier, description, color, timeZone } =
        this.detailsForm;

      const nextSlug = identifier().value().trim();
      const isRename = nextSlug !== currentSlug;

      if (isRename) {
        const confirmation = this.confirmation.open(
          identifierChangeConfirmation(currentSlug, nextSlug)
        );
        const confirmed = await firstValueFrom(confirmation);

        if (!confirmed) return;
      }

      const request: UpdateWorkspaceRequest = {
        name: name().value().trim(),
        slug: currentSlug,
        newSlug: isRename ? nextSlug : undefined,
        description: description().value().trim(),
        metaInfo: {
          color: color().value(),
          timeZone: timeZone().value().trim(),
        },
      };

      this.workspaceCommands.edit(request);
    });
  }
}

const identifierChangeConfirmation = (
  currentSlug: string,
  nextSlug: string
): ConfirmDialogOptions => {
  return {
    title: $localize`:Title of a confirmation dialog:Change workspace identifier?`,
    message: $localize`:Body of a confirmation dialog. CURRENT and NEXT are the old and new workspace identifiers:This workspace moves from /${currentSlug}:CURRENT: to /${nextSlug}:NEXT:.`,
    messageExtended: $localize`:Body of a confirmation dialog listing what breaks when a workspace identifier changes:Links that have already been shared or bookmarked will stop working, and any integration calling the API with the old identifier will start failing. The old identifier is released immediately and can be claimed by another workspace.`,
    confirmationCheckboxLabel: $localize`:Checkbox a user must tick to confirm a dangerous action:I understand that existing links to this workspace will break.`,
    acceptLabel: $localize`:Confirms the action in a dialog:Change identifier`,
    cancelLabel: $localize`:Dismisses a dialog without acting:Cancel`,
    color: 'warn',
    icon: LucideTriangleAlert,
  };
};

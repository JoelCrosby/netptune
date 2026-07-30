import { hostTimeZone } from '@core/util/dates';
import {
  Component,
  computed,
  effect,
  inject,
  resource,
  signal,
} from '@angular/core';
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
import * as Actions from '@core/store/workspaces/workspaces.actions';
import { selectCurrentWorkspace } from '@core/store/workspaces/workspaces.selectors';
import { WorkspacesService } from '@core/store/workspaces/workspaces.service';
import { LucideCheck } from '@lucide/angular';
import { Store } from '@ngrx/store';
import { FlatButtonComponent } from '@static/components/button/flat-button.component';
import { ColorSelectComponent } from '@static/components/color-select/color-select.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { FormTextAreaComponent } from '@static/components/form-textarea/form-textarea.component';
import { SectionHeaderComponent } from '@static/components/section-header/section-header.component';
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
    SectionHeaderComponent,
  ],
  template: `
    <app-section-header
      i18n-heading="Section heading for the workspace detail form"
      heading="Workspace Details" />

    <form class="grid max-w-2xl gap-4" (submit)="save($event)">
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
          Hint under the time zone field. Europe/London is a literal IANA zone
          name
        "
        hint="Use an IANA timezone, for example Europe/London." />

      <div>
        <button app-flat-button type="submit">
          <span i18n="Button that saves the workspace details">
            Save Changes
          </span>
        </button>
      </div>
    </form>
  `,
})
export class WorkspaceDetailsComponent {
  private store = inject(Store);
  private workspaceService = inject(WorkspacesService);

  workspace = this.store.selectSignal(selectCurrentWorkspace);

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

    submit(this.detailsForm, async () => {
      const { name, identifier, description, color, timeZone } =
        this.detailsForm;

      const request: UpdateWorkspaceRequest = {
        name: name().value().trim(),
        slug: identifier().value().trim(),
        description: description().value().trim(),
        metaInfo: {
          color: color().value(),
          timeZone: timeZone().value().trim(),
        },
      };

      this.store.dispatch(Actions.editWorkspace.init({ request }));
    });
  }
}

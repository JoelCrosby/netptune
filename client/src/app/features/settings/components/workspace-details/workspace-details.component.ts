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
  template: `<app-section-header heading="Workspace Details" />

    <form class="grid max-w-2xl gap-4" (submit)="save($event)">
      <app-form-input
        [formField]="detailsForm.name"
        label="Name"
        maxLength="1024" />

      <app-form-input
        [formField]="detailsForm.identifier"
        label="Identifier"
        maxLength="1024"
        [icon]="identifierIcon()"
        [loading]="detailsForm.identifier().pending()"
        hint="Changing the identifier changes the workspace URL and will break existing shared links." />

      <app-form-textarea
        [formField]="detailsForm.description"
        label="Description"
        maxLength="4096" />

      <app-color-select [formField]="detailsForm.color" label="Color" />

      <div>
        <button app-flat-button type="submit">Save Changes</button>
      </div>
    </form>`,
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
  });

  detailsForm = form(this.detailsFormModel, (schema) => {
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
          message: 'Identifier is already taken',
        };
      },
      onError: () => ({
        kind: 'networkError',
        message: 'Could not verify Identifier availability',
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
      });
    });
  }

  save(event: Event) {
    event.preventDefault();

    submit(this.detailsForm, async () => {
      const { name, identifier, description, color } = this.detailsForm;

      const request: UpdateWorkspaceRequest = {
        name: name().value().trim(),
        slug: identifier().value().trim(),
        description: description().value().trim(),
        metaInfo: {
          color: color().value(),
        },
      };

      this.store.dispatch(Actions.editWorkspace.init({ request }));
    });
  }
}

import { Component, computed, effect, inject, resource, signal } from '@angular/core';
import {
  debounce,
  FormField,
  form,
  required,
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
import { firstValueFrom, map } from 'rxjs';

@Component({
  selector: 'app-workspace-details',
  imports: [
    FormField,
    FormInputComponent,
    FormTextAreaComponent,
    ColorSelectComponent,
    FlatButtonComponent,
  ],
  template: `<h3 class="font-overpass text-[1.4rem] font-normal">
      Workspace Details
    </h3>

    <form class="mt-4 grid max-w-2xl gap-4">
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
        [hint]="
          detailsForm.identifier().errors()[0]?.message ??
          'Changing the identifier changes the workspace URL and will break existing shared links.'
        " />

      <app-form-textarea
        [formField]="detailsForm.description"
        label="Description"
        maxLength="4096" />

      <app-color-select [formField]="detailsForm.color" label="Color" />

      <div>
        <button app-flat-button type="button" (click)="save()">
          Save Changes
        </button>
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
    required(schema.name);
    required(schema.identifier);
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
          return null;
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

  save() {
    if (this.detailsForm().pending()) {
      return;
    }

    if (this.detailsForm().invalid()) {
      this.detailsForm().markAsTouched();
      return;
    }

    const { name, identifier, description, color } = this.detailsForm;

    const request: UpdateWorkspaceRequest = {
      name: name().value(),
      slug: identifier().value(),
      description: description().value(),
      metaInfo: {
        color: color().value(),
      },
    };

    this.store.dispatch(Actions.editWorkspace.init({ request }));
  }
}

import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  resource,
  signal,
} from '@angular/core';
import {
  customError,
  debounce,
  disabled,
  Field,
  form,
  required,
  validateAsync,
} from '@angular/forms/signals';
import { MatButton } from '@angular/material/button';
import { AddWorkspaceRequest } from '@core/models/requests/add-workspace-request';
import { UpdateWorkspaceRequest } from '@core/models/requests/update-workspace-request';
import { Workspace } from '@core/models/workspace';
import * as Actions from '@core/store/workspaces/workspaces.actions';
import { WorkspacesService } from '@core/store/workspaces/workspaces.service';
import { colorDictionary } from '@core/util/colors/colors';
import { Store } from '@ngrx/store';
import { ColorSelectComponent } from '@static/components/color-select/color-select.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { FormTextAreaComponent } from '@static/components/form-textarea/form-textarea.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { DialogCloseDirective } from '@static/directives/dialog-close.directive';
import { firstValueFrom, map } from 'rxjs';

@Component({
  selector: 'app-workspace-dialog',
  templateUrl: './workspace-dialog.component.html',
  styleUrls: ['./workspace-dialog.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormInputComponent,
    FormTextAreaComponent,
    ColorSelectComponent,
    DialogActionsDirective,
    MatButton,
    DialogCloseDirective,
    Field,
  ],
})
export class WorkspaceDialogComponent {
  private store = inject(Store);
  private workspaceServcie = inject(WorkspacesService);

  dialogRef = inject<DialogRef<WorkspaceDialogComponent>>(DialogRef);
  data = inject<Workspace>(DIALOG_DATA, { optional: true });

  dialogFormModel = signal({
    name: this.data?.name ?? '',
    identifier: this.data?.slug ?? '',
    description: this.data?.description ?? '',
    color: this.data?.metaInfo?.color ?? '',
  });

  dialogForm = form(this.dialogFormModel, (schema) => {
    required(schema.name);
    required(schema.identifier);
    required(schema.color);
    disabled(schema.identifier, () => this.isEditMode);
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
          return null;
        }

        return customError({
          kind: 'identifierTaken',
          message: 'Identifier is already taken',
        });
      },
      onError: () => ({
        kind: 'networkError',
        message: 'Could not veify Identifier availability',
      }),
    });
  });

  identifierCheck = signal(false);

  identifierIcon = computed(() => {
    if (this.dialogForm.identifier().valid()) {
      return 'check';
    }

    return '';
  });

  identifierHint = computed(() => {
    const taken = this.dialogForm
      .identifier()
      .errors()
      .find((x) => x.message === 'already-taken');
    return taken ? 'Identifier is already taken' : null;
  });

  colors = colorDictionary();

  get isEditMode() {
    return !!this.data;
  }

  getResult() {
    if (this.dialogForm().pending()) {
      return;
    }

    if (this.dialogForm().invalid()) {
      this.dialogForm().markAsTouched();
      return;
    }

    if (this.isEditMode) {
      this.editWorkspace();
    } else {
      this.createWorkspace();
    }

    this.dialogRef.close();
  }

  editWorkspace() {
    const { name, identifier, description, color } = this.dialogForm;

    const request: UpdateWorkspaceRequest = {
      name: name().value(),
      slug: identifier().value(),
      description: description().value(),
      metaInfo: {
        color: color().value(),
      },
    };

    this.store.dispatch(Actions.editWorkspace({ request }));
  }

  createWorkspace() {
    const { name, identifier, description, color } = this.dialogForm;

    const request: AddWorkspaceRequest = {
      name: name().value(),
      slug: identifier().value(),
      description: description().value(),
      metaInfo: {
        color: color().value(),
      },
    };

    this.store.dispatch(Actions.createWorkspace({ request }));
  }

  getColorLabel(value: string) {
    const obj = this.colors.find((color) => color.color === value);
    return obj && obj.name;
  }
}

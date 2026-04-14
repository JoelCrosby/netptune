import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  resource,
  signal,
} from '@angular/core';
import {
  disabled,
  form,
  FormField,
  required,
  validateAsync,
} from '@angular/forms/signals';
import { FlatButtonComponent } from '@app/static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@app/static/components/button/stroked-button.component';
import { createBoard } from '@boards/store/boards/boards.actions';
import { BoardsService } from '@boards/store/boards/boards.service';
import { Board } from '@core/models/board';
import { AddBoardRequest } from '@core/models/requests/add-board-request';
import { loadProjects } from '@core/store/projects/projects.actions';
import { selectAllProjects } from '@core/store/projects/projects.selectors';
import { colorDictionary } from '@core/util/colors/colors';
import { Logger } from '@core/util/logger';
import { toUrlSlug } from '@core/util/strings';
import { LucideCheck } from '@lucide/angular';
import { Store } from '@ngrx/store';
import { ColorSelectComponent } from '@static/components/color-select/color-select.component';
import { DialogTitleComponent } from '@static/components/dialog-title/dialog-title.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { FormSelectOptionComponent } from '@static/components/form-select/form-select-option.component';
import { FormSelectComponent } from '@static/components/form-select/form-select.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { DialogCloseDirective } from '@static/directives/dialog-close.directive';
import { firstValueFrom } from 'rxjs';
import { map } from 'rxjs/operators';

@Component({
  selector: 'app-create-board',
  template: `
    <app-dialog-title>{{
      isEditMode ? 'Edit Board' : 'Create Board'
    }}</app-dialog-title>

    <form app-dialog-content class="form-auth">
      <app-form-input
        [formField]="boardForm.name"
        label="Board Name"
        maxLength="1024">
      </app-form-input>

      <app-form-input
        [formField]="boardForm.identifier"
        label="Board Identifier"
        maxLength="1024"
        [icon]="identifierIcon()"
        [loading]="boardForm.identifier().pending()">
      </app-form-input>

      <app-form-select [formField]="boardForm.projectId" label="Project">
        @for (project of projects(); track project.id) {
          <app-form-select-option [value]="project.id">
            {{ project.name }}
          </app-form-select-option>
        }
      </app-form-select>

      <app-color-select
        [formField]="boardForm.color"
        label="Color"></app-color-select>
    </form>

    <div app-dialog-actions align="end">
      <button app-stroked-button app-dialog-close>Close</button>
      <button app-flat-button (click)="getResult()">
        {{ isEditMode ? 'Save Changes' : 'Create Board' }}
      </button>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    DialogTitleComponent,
    FormInputComponent,
    FormSelectComponent,
    FormSelectOptionComponent,
    ColorSelectComponent,
    DialogActionsDirective,
    DialogCloseDirective,
    FormField,
    StrokedButtonComponent,
    FlatButtonComponent,
  ],
})
export class CreateBoardComponent {
  private store = inject(Store);
  private boardsService = inject(BoardsService);

  dialogRef = inject<DialogRef<CreateBoardComponent>>(DialogRef);
  data = inject<Board>(DIALOG_DATA, { optional: true });
  isEditMode = !!this.data;

  boardFormModel = signal({
    name: this.data?.name ?? '',
    identifier: this.data?.identifier ?? '',
    color: this.data?.metaInfo?.color ?? '',
    projectId: this.data?.projectId ?? (null as number | null),
  });

  boardForm = form(this.boardFormModel, (schema) => {
    required(schema.name);
    required(schema.identifier);
    required(schema.color);
    required(schema.projectId);
    disabled(schema.identifier, () => this.isEditMode);
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
            const request = this.boardsService
              .isIdentifierUnique(params)
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
        message: 'Could not veify Identifier availability',
      }),
    });
  });

  projects = this.store.selectSignal(selectAllProjects);

  identifierIcon = computed(() => {
    if (this.boardForm.identifier().pending()) {
      return null;
    }

    if (this.boardForm.identifier().valid()) {
      return LucideCheck;
    }

    return null;
  });

  colors = colorDictionary();

  constructor() {
    effect(() => {
      if (this.data) return;

      const current = this.boardForm.identifier().value();
      const name = this.boardForm.name().value();
      const identifier = toUrlSlug(name);

      if (identifier === current) return;

      this.boardFormModel.update((model) => {
        const name = model.name;
        const identifier = toUrlSlug(name);

        return { ...model, identifier };
      });
    });

    this.store.dispatch(loadProjects());
  }

  getResult() {
    if (this.boardForm().pending()) {
      return;
    }

    if (this.boardForm().invalid()) {
      this.boardForm().markAsTouched();
      return;
    }

    const { name, identifier, color } = this.boardForm;
    const projectId = this.boardForm.projectId().value();

    if (!projectId) return;

    const request: AddBoardRequest = {
      ...this.data,
      name: name().value(),
      identifier: identifier().value(),
      projectId,
      meta: {
        color: color().value(),
      },
    };

    if (this.isEditMode) {
      Logger.warn('Edit is not implemented');
    } else {
      this.store.dispatch(createBoard({ request }));
    }

    this.dialogRef.close();
  }

  getColorLabel(value: string) {
    const obj = this.colors.find((color) => color.color === value);
    return obj && obj.name;
  }
}

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
  form,
  FormField,
  required,
  validateAsync,
} from '@angular/forms/signals';
import { FlatButtonComponent } from '@app/static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@app/static/components/button/stroked-button.component';
import {
  createBoard,
  updateBoard,
} from '@app/core/store/boards/boards.actions';
import { BoardsService } from '@app/core/store/boards/boards.service';
import { Board } from '@core/models/board';
import { AddBoardRequest } from '@core/models/requests/add-board-request';
import { UpdateBoardRequest } from '@core/models/requests/update-board-request';
import { loadProjects } from '@core/store/projects/projects.actions';
import { selectAllProjects } from '@core/store/projects/projects.selectors';
import { colorDictionary } from '@core/util/colors/colors';
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
import { SetupTemplatePickerComponent } from '@app/entry/components/setup-template-picker/setup-template-picker.component';

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

      @if (!isEditMode) {
        <app-form-select [formField]="boardForm.projectId" label="Project">
          @for (project of projects(); track project.id) {
            <app-form-select-option [value]="project.id">
              {{ project.name }}
            </app-form-select-option>
          }
        </app-form-select>
      }

      <app-color-select
        [formField]="boardForm.color"
        label="Color"></app-color-select>

      @if (!isEditMode) {
        <app-setup-template-picker
          [selectedKey]="boardForm.templateKey().value()"
          (selectedKeyChange)="setTemplate($event)" />
      }
    </form>

    <div app-dialog-actions align="end">
      <button app-stroked-button app-dialog-close>Close</button>
      <button app-flat-button (click)="getResult()">
        {{ isEditMode ? 'Save Changes' : 'Create Board' }}
      </button>
    </div>
  `,
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
    SetupTemplatePickerComponent,
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
    templateKey: 'software',
  });

  boardForm = form(this.boardFormModel, (schema) => {
    required(schema.name);
    required(schema.identifier);
    required(schema.color);
    required(schema.projectId, { when: () => !this.isEditMode });
    validateAsync(schema.identifier, {
      params: ({ value }) => {
        const identifier = value();
        if (this.isEditMode && identifier === this.data?.identifier) {
          return undefined;
        }
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

    this.store.dispatch(loadProjects.init());
  }

  getResult() {
    if (this.boardForm().pending()) {
      return;
    }

    if (this.boardForm().invalid()) {
      this.boardForm().markAsTouched();
      return;
    }

    const { name, identifier, color, templateKey } = this.boardForm;

    if (this.isEditMode) {
      if (!this.data?.id) return;

      const request: UpdateBoardRequest = {
        id: this.data.id,
        name: name().value(),
        identifier: identifier().value(),
        meta: {
          color: color().value(),
        },
      };

      this.store.dispatch(updateBoard.init({ request }));
    } else {
      const projectId = this.boardForm.projectId().value();

      if (!projectId) return;

      const request: AddBoardRequest = {
        name: name().value(),
        identifier: identifier().value(),
        projectId,
        meta: {
          color: color().value(),
        },
        templateKey: templateKey().value(),
      };

      this.store.dispatch(createBoard.init({ request }));
    }

    this.dialogRef.close();
  }

  setTemplate(templateKey: string) {
    this.boardFormModel.update((model) => ({ ...model, templateKey }));
  }

  getColorLabel(value: string) {
    const obj = this.colors.find((color) => color.name === value);
    return obj?.label;
  }
}

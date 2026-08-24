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
  apply,
  form,
  FormField,
  required,
  submit,
  validateAsync,
} from '@angular/forms/signals';
import { FlatButtonComponent } from '@app/static/components/button/flat-button.component';
import { StrokedButtonComponent } from '@app/static/components/button/stroked-button.component';
import { BoardCommandsService } from '@core/services/board-commands.service';
import { BoardsService } from '@core/services/boards.service';
import { Board } from '@core/models/board';
import { AddBoardRequest } from '@core/models/requests/add-board-request';
import { UpdateBoardRequest } from '@core/models/requests/update-board-request';
import { projectResource } from '@core/resources/project.resource';
import { colorDictionary } from '@core/util/colors/colors';
import { toUrlSlug } from '@core/util/strings';
import { LucideCheck } from '@lucide/angular';
import { ColorSelectComponent } from '@static/components/color-select/color-select.component';
import { DialogTitleComponent } from '@static/components/dialog-title/dialog-title.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { FormSelectOptionComponent } from '@static/components/form-select/form-select-option.component';
import { FormSelectComponent } from '@static/components/form-select/form-select.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { DialogCloseDirective } from '@static/directives/dialog-close.directive';
import { firstValueFrom } from 'rxjs';
import { map } from 'rxjs/operators';
import { BoardBrandingComponent } from '@boards/components/board-branding/board-branding.component';
import { SetupTemplatePickerComponent } from '@app/entry/components/setup-template-picker/setup-template-picker.component';
import { requiredTextSchema } from '@core/util/forms/validation.schemas';

@Component({
  selector: 'app-create-board',
  template: `
    <app-dialog-title>{{ titleLabel }}</app-dialog-title>

    <form app-dialog-content class="form-auth">
      <app-form-input
        [formField]="boardForm.name"
        i18n-label="Label of the board name field"
        label="Board Name"
        maxLength="1024"></app-form-input>

      <app-form-input
        [formField]="boardForm.identifier"
        i18n-label="Label of the board URL identifier field"
        label="Board Identifier"
        maxLength="1024"
        [icon]="identifierIcon()"
        [loading]="boardForm.identifier().pending()"></app-form-input>

      @if (!isEditMode) {
        <app-form-select
          [formField]="boardForm.projectId"
          i18n-label="Label of the project field"
          label="Project">
          @for (project of projects(); track project.id) {
            <app-form-select-option [value]="project.id">
              {{ project.name }}
            </app-form-select-option>
          }
        </app-form-select>
      }

      <app-color-select
        [formField]="boardForm.color"
        i18n-label="Label of the colour picker field"
        label="Color"></app-color-select>

      @if (!isEditMode) {
        <app-setup-template-picker
          [selectedKey]="boardForm.templateKey().value()"
          (selectedKeyChange)="setTemplate($event)" />
      }

      @if (editingBoardId; as boardId) {
        <app-board-branding
          [boardId]="boardId"
          [initialLogoFileId]="data?.metaInfo?.logoFileId ?? null"
          [initialBackgroundFileId]="
            data?.metaInfo?.backgroundFileId ?? null
          " />
      }
    </form>

    <div app-dialog-actions align="end">
      <button app-stroked-button app-dialog-close type="button">
        <span i18n="Dismisses a dialog without saving">Close</span>
      </button>
      <button app-flat-button type="button" (click)="getResult()">
        {{ submitLabel }}
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
    BoardBrandingComponent,
  ],
})
export class CreateBoardComponent {
  private boardCommands = inject(BoardCommandsService);
  private boardsService = inject(BoardsService);

  dialogRef = inject<DialogRef<CreateBoardComponent>>(DialogRef);
  data = inject<Board>(DIALOG_DATA, { optional: true });
  isEditMode = !!this.data;

  /** Ternaries in a template expression cannot be marked, so build the copy here. */
  readonly titleLabel = this.isEditMode
    ? $localize`:Title of the edit-board dialog:Edit Board`
    : $localize`:Title of the create-board dialog:Create Board`;

  readonly editingBoardId = this.isEditMode ? (this.data?.id ?? null) : null;

  readonly submitLabel = this.isEditMode
    ? $localize`:Button that saves edits to the board:Save Changes`
    : $localize`:Button that creates the board:Create Board`;

  boardFormModel = signal({
    name: this.data?.name ?? '',
    identifier: this.data?.identifier ?? '',
    color: this.data?.metaInfo?.color ?? '',
    projectId: this.data?.projectId ?? (null as number | null),
    templateKey: 'software',
  });

  boardForm = form(this.boardFormModel, (schema) => {
    apply(
      schema.name,
      requiredTextSchema({
        label: $localize`:Field name used inside validation messages, e.g. "Board name is required.":Board name`,
        maxLength: 1024,
      })
    );
    apply(
      schema.identifier,
      requiredTextSchema({
        label: $localize`:Field name used inside validation messages, e.g. "Board identifier is required.":Board identifier`,
        minLength: 4,
        maxLength: 1024,
      })
    );
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
          return undefined;
        }

        return {
          kind: 'identifierTaken',
          message: $localize`:Validation error when a board identifier is already in use:Identifier is already taken`,
        };
      },
      onError: () => ({
        kind: 'networkError',
        message: $localize`:Shown when the board identifier availability check fails:Could not veify Identifier availability`,
      }),
    });
  });

  readonly projectsResource = projectResource();
  readonly projects = this.projectsResource.value;

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
  }

  getResult() {
    submit(this.boardForm, async () => {
      const { name, identifier, color, templateKey } = this.boardForm;

      if (this.isEditMode) {
        if (!this.data?.id) return;

        const request: UpdateBoardRequest = {
          id: this.data.id,
          name: name().value().trim(),
          identifier: identifier().value().trim(),
          meta: {
            color: color().value(),
          },
        };

        this.boardCommands.update(request);
      } else {
        const projectId = this.boardForm.projectId().value();

        if (!projectId) return;

        const request: AddBoardRequest = {
          name: name().value().trim(),
          identifier: identifier().value().trim(),
          projectId,
          meta: {
            color: color().value(),
          },
          templateKey: templateKey().value(),
        };

        this.boardCommands.create(request);
      }

      this.dialogRef.close();
    });
  }

  setTemplate(templateKey: string) {
    this.boardFormModel.update((model) => ({ ...model, templateKey }));
  }

  getColorLabel(value: string) {
    const obj = this.colors.find((color) => color.name === value);
    return obj?.label;
  }
}

import { DIALOG_DATA, DialogRef } from '@angular/cdk/dialog';
import {
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  computed,
  effect,
  inject,
  signal,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import {
  AbstractControl,
  FormControl,
  FormGroup,
  FormsModule,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { MatButton } from '@angular/material/button';
import { AddWorkspaceRequest } from '@core/models/requests/add-workspace-request';
import { UpdateWorkspaceRequest } from '@core/models/requests/update-workspace-request';
import { Workspace } from '@core/models/workspace';
import * as Actions from '@core/store/workspaces/workspaces.actions';
import { WorkspacesService } from '@core/store/workspaces/workspaces.service';
import { colorDictionary } from '@core/util/colors/colors';
import { debouncedSignal } from '@core/util/signals';
import { toUrlSlug } from '@core/util/strings';
import { Store } from '@ngrx/store';
import { ColorSelectComponent } from '@static/components/color-select/color-select.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { FormTextAreaComponent } from '@static/components/form-textarea/form-textarea.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { DialogCloseDirective } from '@static/directives/dialog-close.directive';
import { debounceTime, map, tap } from 'rxjs/operators';

@Component({
  selector: 'app-workspace-dialog',
  templateUrl: './workspace-dialog.component.html',
  styleUrls: ['./workspace-dialog.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    ReactiveFormsModule,
    FormInputComponent,
    FormTextAreaComponent,
    ColorSelectComponent,
    DialogActionsDirective,
    MatButton,
    DialogCloseDirective,
  ],
})
export class WorkspaceDialogComponent {
  private store = inject(Store);
  private cd = inject(ChangeDetectorRef);
  private workspaceServcie = inject(WorkspacesService);

  dialogRef = inject<DialogRef<WorkspaceDialogComponent>>(DialogRef);
  data = inject<Workspace>(DIALOG_DATA, { optional: true });

  isUniqueLoading = signal(false);
  identifierCheck = signal(false);
  identifierStatusChange = toSignal(this.identifier.statusChanges);
  nameValueChanges = toSignal(this.name.valueChanges);
  identifierIcon = computed(() => {
    if (this.isUniqueLoading()) return null;

    if (this.identifier?.valid) {
      return 'check';
    }

    return '';
  });

  showIdentifierCheck = computed(() => {
    const check = this.identifierCheck();
    const loading = this.isUniqueLoading();

    return !loading && check;
  });

  isUniqueLoadingDebounced = debouncedSignal(this.isUniqueLoading, 640);

  formGroup = new FormGroup(
    {
      name: new FormControl('', {
        validators: [Validators.required],
        updateOn: 'change',
        nonNullable: true,
      }),
      identifier: new FormControl('', {
        validators: [Validators.required],
        asyncValidators: this.data ? null : this.validate.bind(this),
        updateOn: 'change',
      }),
      description: new FormControl(''),
      color: new FormControl('#673AB7'),
    },
    { updateOn: 'blur' }
  );

  colors = colorDictionary();

  get name() {
    return this.formGroup.controls.name;
  }

  get identifier() {
    return this.formGroup.controls.identifier;
  }

  get description() {
    return this.formGroup.controls.description;
  }

  get color() {
    return this.formGroup.controls.color;
  }

  get selectedColor() {
    return this.color.value;
  }

  get isEditMode() {
    return !!this.data;
  }

  constructor() {
    const editMode = this.data && this.data.slug;

    effect(() => {
      const value = this.nameValueChanges();

      if (!value) {
        this.identifier.setValue('');
        this.identifierCheck.set(false);
        return;
      }

      if (typeof value !== 'string') return;

      this.identifier.setValue(toUrlSlug(value));
    });

    if (editMode) {
      const workspace = this.data as Required<Workspace>;

      this.name.setValue(workspace.name, { emitEvent: false });
      this.identifier.setValue(workspace.slug, { emitEvent: false });
      this.description.setValue(workspace.description, { emitEvent: false });
      this.color.setValue(workspace.metaInfo?.color ?? null, {
        emitEvent: false,
      });

      this.identifier.disable({ emitEvent: false });
    }
  }

  validate(control: AbstractControl) {
    this.isUniqueLoading.set(true);
    return this.workspaceServcie.isSlugUnique(control.value as string).pipe(
      debounceTime(640),
      map((val) => {
        this.isUniqueLoading.set(false);
        if (val?.payload?.isUnique) {
          this.identifierCheck.set(true);
          return null;
        } else {
          this.identifierCheck.set(false);
          return { 'already-taken': true };
        }
      }),
      tap(() => this.cd.markForCheck())
    );
  }

  getResult() {
    if (this.formGroup.pending) {
      return;
    }

    if (this.formGroup.invalid) {
      this.formGroup.markAllAsTouched();
      return;
    }

    if (this.isEditMode) {
      const request: UpdateWorkspaceRequest = {
        name: this.name.value,
        slug: this.identifier.value as string,
        description: this.description.value as string,
        metaInfo: {
          color: this.selectedColor as string,
        },
      };

      this.store.dispatch(Actions.editWorkspace({ request }));
    } else {
      const request: AddWorkspaceRequest = {
        name: this.name.value,
        slug: this.identifier.value as string,
        description: this.description.value as string,
        metaInfo: {
          color: this.selectedColor as string,
        },
      };

      this.store.dispatch(Actions.createWorkspace({ request }));
    }

    this.dialogRef.close();
  }

  getColorLabel(value: string) {
    const obj = this.colors.find((color) => color.color === value);
    return obj && obj.name;
  }
}

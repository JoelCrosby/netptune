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
  FormBuilder,
  FormsModule,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { MatButton } from '@angular/material/button';
import { createBoard } from '@boards/store/boards/boards.actions';
import { BoardsService } from '@boards/store/boards/boards.service';
import { Board } from '@core/models/board';
import { AddBoardRequest } from '@core/models/requests/add-board-request';
import { loadProjects } from '@core/store/projects/projects.actions';
import { selectAllProjects } from '@core/store/projects/projects.selectors';
import { colorDictionary } from '@core/util/colors/colors';
import { Logger } from '@core/util/logger';
import { toUrlSlug } from '@core/util/strings';
import { Store } from '@ngrx/store';
import { ColorSelectComponent } from '@static/components/color-select/color-select.component';
import { FormInputComponent } from '@static/components/form-input/form-input.component';
import { FormSelectOptionComponent } from '@static/components/form-select/form-select-option.component';
import { FormSelectComponent } from '@static/components/form-select/form-select.component';
import { DialogActionsDirective } from '@static/directives/dialog-actions.directive';
import { DialogCloseDirective } from '@static/directives/dialog-close.directive';
import { animationFrameScheduler } from 'rxjs';
import { debounceTime, map, observeOn, tap } from 'rxjs/operators';

@Component({
  selector: 'app-create-board',
  templateUrl: './create-board.component.html',
  styleUrls: ['./create-board.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    ReactiveFormsModule,
    FormInputComponent,
    FormSelectComponent,
    FormSelectOptionComponent,
    ColorSelectComponent,
    DialogActionsDirective,
    MatButton,
    DialogCloseDirective,
  ],
})
export class CreateBoardComponent {
  private store = inject(Store);
  private fb = inject(FormBuilder);
  private cd = inject(ChangeDetectorRef);
  private boardsService = inject(BoardsService);

  dialogRef = inject<DialogRef<CreateBoardComponent>>(DialogRef);
  data = inject<Board>(DIALOG_DATA, { optional: true });
  isEditMode = !!this.data;

  form = this.fb.nonNullable.group(
    {
      name: ['', [Validators.required]],
      identifier: [
        '',
        [Validators.required],
        this.data ? null : this.validate.bind(this),
        'change',
      ],
      color: ['#673AB7' as string | undefined],
      projectId: [null as number | null, [Validators.required]],
    },
    { updateOn: 'blur' }
  );

  isUniqueLoading = signal(false);
  identifierStatusChanges = toSignal(
    this.form.controls.identifier.statusChanges
  );

  nameValueChanges = toSignal(this.form.controls.name.valueChanges);
  projects = this.store.selectSignal(selectAllProjects);

  identifierIcon = computed(() => {
    this.identifierStatusChanges();

    if (this.isUniqueLoading()) {
      return null;
    }

    if (this.form.controls.identifier?.valid) {
      return 'check';
    }

    return '';
  });

  colors = colorDictionary();

  constructor() {
    const { name, identifier, color, projectId } = this.form.controls;

    effect(() => {
      if (this.data) return;
      const value = this.nameValueChanges();
      if (typeof value !== 'string') return;
      identifier.setValue(toUrlSlug(value));
    });

    if (this.data) {
      const board = this.data;

      name.setValue(board.name, { emitEvent: false });
      identifier.setValue(board.identifier, { emitEvent: false });
      color.setValue(board.metaInfo?.color, { emitEvent: false });
      projectId.setValue(board.projectId, { emitEvent: false });
      identifier.disable({ emitEvent: false });
    }

    this.store.dispatch(loadProjects());
  }

  validate(control: AbstractControl) {
    this.isUniqueLoading.set(true);
    return this.boardsService.isIdentifierUnique(control.value as string).pipe(
      observeOn(animationFrameScheduler),
      debounceTime(240),
      map((val) => {
        this.isUniqueLoading.set(false);
        if (val?.payload?.isUnique) {
          return null;
        } else {
          return { 'already-taken': true };
        }
      }),
      tap(() => this.cd.markForCheck())
    );
  }

  getResult() {
    if (this.form.pending) {
      return;
    }

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { name, identifier, color, projectId } = this.form.controls;

    if (!identifier.value || !projectId.value) return;

    const request: AddBoardRequest = {
      ...this.data,
      name: name.value,
      identifier: identifier.value,
      projectId: projectId.value,
      meta: {
        color: color.value,
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

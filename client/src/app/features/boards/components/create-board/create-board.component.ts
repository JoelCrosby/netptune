import {
  AfterViewInit,
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  Inject,
  OnInit,
  Optional,
} from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  FormControl,
  FormGroup,
  Validators,
} from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import * as Actions from '@boards/store/boards/boards.actions';
import { BoardsService } from '@boards/store/boards/boards.service';
import { AppState } from '@core/core.state';
import { Board } from '@core/models/board';
import { AddBoardRequest } from '@core/models/requests/add-board-request';
import { ProjectViewModel } from '@core/models/view-models/project-view-model';
import { loadProjects } from '@core/store/projects/projects.actions';
import { selectAllProjects } from '@core/store/projects/projects.selectors';
import { colorDictionary } from '@core/util/colors/colors';
import { Logger } from '@core/util/logger';
import { toUrlSlug } from '@core/util/strings';
import { Store } from '@ngrx/store';
import {
  animationFrameScheduler,
  combineLatest,
  Observable,
  Subject,
} from 'rxjs';
import { debounceTime, map, observeOn, takeUntil, tap } from 'rxjs/operators';

@Component({
  selector: 'app-create-board',
  templateUrl: './create-board.component.html',
  styleUrls: ['./create-board.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CreateBoardComponent implements OnInit, AfterViewInit {
  isUniqueLoading$ = new Subject<boolean>();

  projects$!: Observable<ProjectViewModel[]>;
  identifierIcon$!: Observable<string | null>;

  onDestroy$ = new Subject<void>();

  formGroup!: FormGroup;

  colors = colorDictionary();

  get name() {
    return this.formGroup.controls.name;
  }

  get identifier() {
    return this.formGroup.controls.identifier;
  }

  get color() {
    return this.formGroup.controls.color;
  }

  get projectId() {
    return this.formGroup.controls.projectId;
  }

  get selectedColor() {
    return this.color.value;
  }

  get isEditMode() {
    return !!this.data;
  }

  constructor(
    private store: Store<AppState>,
    private fb: FormBuilder,
    private cd: ChangeDetectorRef,
    private boardsService: BoardsService,
    public dialogRef: MatDialogRef<CreateBoardComponent>,
    @Optional() @Inject(MAT_DIALOG_DATA) public data: Board
  ) {}

  ngOnInit() {
    this.projects$ = this.store.select(selectAllProjects);
    this.formGroup = this.fb.group(
      {
        name: new FormControl('', {
          validators: [Validators.required],
          updateOn: 'change',
        }),
        identifier: new FormControl('', {
          validators: [Validators.required],
          asyncValidators: this.data ? null : this.validate.bind(this),
          updateOn: 'change',
        }),
        color: new FormControl('#673AB7'),
        projectId: new FormControl(null, {
          validators: [Validators.required],
        }),
      },
      { updateOn: 'blur' }
    );

    this.identifierIcon$ = combineLatest([
      this.isUniqueLoading$.pipe(),
      this.identifier.statusChanges,
    ]).pipe(
      map(([loading]) => {
        if (loading) return null;

        if (this.identifier?.valid) {
          return 'check';
        }

        return '';
      })
    );

    if (this.data) {
      const board = this.data;

      this.name.setValue(board.name, { emitEvent: false });
      this.identifier.setValue(board.identifier, { emitEvent: false });
      this.color.setValue(board.metaInfo?.color, { emitEvent: false });
      this.projectId.setValue(board.projectId, { emitEvent: false });
      this.identifier.disable({ emitEvent: false });
    } else {
      this.name.valueChanges
        .pipe(
          takeUntil(this.onDestroy$),
          debounceTime(240),
          tap((value: string | undefined) => {
            if (typeof value !== 'string') return;

            this.identifier.setValue(toUrlSlug(value));
          })
        )
        .subscribe();
    }
  }

  ngAfterViewInit() {
    this.store.dispatch(loadProjects());
  }

  validate(control: AbstractControl) {
    this.isUniqueLoading$.next(true);
    return this.boardsService.isIdentifierUnique(control.value as string).pipe(
      observeOn(animationFrameScheduler),
      debounceTime(240),
      map((val) => {
        this.isUniqueLoading$.next(false);
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
    if (this.formGroup.pending) {
      return;
    }

    if (this.formGroup.invalid) {
      this.formGroup.markAllAsTouched();
      return;
    }

    const request: AddBoardRequest = {
      ...this.data,
      name: this.name.value,
      identifier: this.identifier.value,
      projectId: this.projectId.value,
      meta: {
        color: this.color.value,
      },
    };

    if (this.isEditMode) {
      Logger.warn('Edit is not implemented');
    } else {
      this.store.dispatch(Actions.createBoard({ request }));
    }

    this.dialogRef.close();
  }

  getColorLabel(value: string) {
    const obj = this.colors.find((color) => color.color === value);
    return obj && obj.name;
  }
}

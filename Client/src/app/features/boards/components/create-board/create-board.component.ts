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
import { Board } from '@core/models/board';
import { AddBoardRequest } from '@core/models/requests/add-board-request';
import { ProjectViewModel } from '@core/models/view-models/project-view-model';
import { loadProjects } from '@core/store/projects/projects.actions';
import { selectAllProjects } from '@core/store/projects/projects.selectors';
import { colorDictionary } from '@core/util/colors/colors';
import * as Actions from '@boards/store/boards/boards.actions';
import { BoardsService } from '@boards/store/boards/boards.service';
import { Store } from '@ngrx/store';
import { Observable, Subject } from 'rxjs';
import { debounceTime, map, tap } from 'rxjs/operators';

@Component({
  selector: 'app-create-board',
  templateUrl: './create-board.component.html',
  styleUrls: ['./create-board.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CreateBoardComponent implements OnInit, AfterViewInit {
  isUniqueLoading$ = new Subject<boolean>();
  projects$: Observable<ProjectViewModel[]>;

  onDestroy$ = new Subject();

  formGroup: FormGroup;

  colors = colorDictionary();

  get name() {
    return this.formGroup.get('name');
  }

  get identifier() {
    return this.formGroup.get('identifier');
  }

  get color() {
    return this.formGroup.get('color');
  }

  get projectId() {
    return this.formGroup.get('projectId');
  }

  get selectedColor() {
    return this.color.value;
  }

  get isEditMode() {
    return !!this.data;
  }

  constructor(
    private store: Store,
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
        }),
        identifier: new FormControl('', {
          validators: [Validators.required],
          asyncValidators: this.data ? null : this.validate.bind(this),
          updateOn: 'change',
        }),
        color: new FormControl(''),
        projectId: new FormControl(null, {
          validators: [Validators.required],
        }),
      },
      { updateOn: 'blur' }
    );

    if (this.data) {
      const board = this.data;

      this.name.setValue(board.name, { emitEvent: false });
      this.identifier.setValue(board.identifier, { emitEvent: false });
      this.color.setValue(board.metaInfo.color, { emitEvent: false });
      this.projectId.setValue(board.projectId, { emitEvent: false });
      this.identifier.disable({ emitEvent: false });
    }
  }

  ngAfterViewInit() {
    this.store.dispatch(loadProjects());
  }

  validate(control: AbstractControl) {
    this.isUniqueLoading$.next(true);
    return this.boardsService.isIdentifierUnique(control.value).pipe(
      debounceTime(140),
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
      console.log('Edit is not implemented');
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

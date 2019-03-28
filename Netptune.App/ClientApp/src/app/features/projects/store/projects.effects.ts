import { Injectable } from '@angular/core';
import { Actions, Effect, ofType } from '@ngrx/effects';
import { of } from 'rxjs';
import { catchError, map, switchMap } from 'rxjs/operators';
import { ActionAuthLoginFail } from '../../../core/auth/store/auth.actions';
import {
  ActionLoadProjectsSuccess,
  ProjectsActions,
  ProjectsActionTypes,
} from './projects.actions';
import { ProjectsService } from './projects.service';

@Injectable()
export class ProjectsEffects {
  constructor(
    private actions$: Actions<ProjectsActions>,
    private projectsService: ProjectsService
  ) {}

  @Effect()
  loadProjects$ = this.actions$.pipe(
    ofType(ProjectsActionTypes.LoadProjects),
    switchMap(action =>
      this.projectsService.get(1).pipe(
        map(projects => new ActionLoadProjectsSuccess(projects)),
        catchError(error => of(new ActionAuthLoginFail(error)))
      )
    )
  );
}

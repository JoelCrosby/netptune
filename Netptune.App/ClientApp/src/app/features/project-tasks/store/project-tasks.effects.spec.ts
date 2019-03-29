import { TestBed, inject } from '@angular/core/testing';
import { provideMockActions } from '@ngrx/effects/testing';
import { Observable } from 'rxjs';

import { ProjectTasksEffects } from './project-tasks.effects';

describe('ProjectTasksEffects', () => {
  let actions$: Observable<any>;
  let effects: ProjectTasksEffects;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        ProjectTasksEffects,
        provideMockActions(() => actions$)
      ]
    });

    effects = TestBed.get(ProjectTasksEffects);
  });

  it('should be created', () => {
    expect(effects).toBeTruthy();
  });
});

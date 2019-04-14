import { TestBed, inject } from '@angular/core/testing';
import { provideMockActions } from '@ngrx/effects/testing';
import { Observable } from 'rxjs';

import { WorkspacesEffects } from './workspaces.effects';

describe('WorkspacesEffects', () => {
  let actions$: Observable<any>;
  let effects: WorkspacesEffects;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        WorkspacesEffects,
        provideMockActions(() => actions$)
      ]
    });

    effects = TestBed.get(WorkspacesEffects);
  });

  it('should be created', () => {
    expect(effects).toBeTruthy();
  });
});

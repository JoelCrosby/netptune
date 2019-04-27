import { TestBed, inject } from '@angular/core/testing';
import { provideMockActions } from '@ngrx/effects/testing';
import { Observable } from 'rxjs';

import { ProfileEffects } from './profile.effects';

describe('ProfileEffects', () => {
  let actions$: Observable<any>;
  let effects: ProfileEffects;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [ProfileEffects, provideMockActions(() => actions$)],
    });

    effects = TestBed.get(ProfileEffects);
  });

  it('should be created', () => {
    expect(effects).toBeTruthy();
  });
});

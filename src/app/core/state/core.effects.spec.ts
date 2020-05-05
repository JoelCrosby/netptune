import { TestBed, inject } from '@angular/core/testing';
import { provideMockActions } from '@ngrx/effects/testing';
import { Observable } from 'rxjs';

import { CoreEffects } from './core.effects';

describe('CoreEffects', () => {
  let actions$: Observable<any>;
  let effects: CoreEffects;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        CoreEffects,
        provideMockActions(() => actions$)
      ]
    });

    effects = TestBed.get(CoreEffects);
  });

  it('should be created', () => {
    expect(effects).toBeTruthy();
  });
});

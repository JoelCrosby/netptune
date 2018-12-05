/* tslint:disable:no-unused-variable */

import { TestBed, async, inject } from '@angular/core/testing';
import { TransitionService } from './transition.service';

describe('Service: Transition', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [TransitionService]
    });
  });

  it('should ...', inject([TransitionService], (service: TransitionService) => {
    expect(service).toBeTruthy();
  }));
});

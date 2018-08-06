/* tslint:disable:no-unused-variable */

import { TestBed, async, inject } from '@angular/core/testing';
import { ProjectTypeService } from './project-type.service';

describe('Service: ProjectType', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [ProjectTypeService]
    });
  });

  it('should ...', inject([ProjectTypeService], (service: ProjectTypeService) => {
    expect(service).toBeTruthy();
  }));
});

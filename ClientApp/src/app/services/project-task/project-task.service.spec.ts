import { TestBed, inject } from '@angular/core/testing';

import { ProjectTaskService } from './project-task.service';

describe('TaskService', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [ProjectTaskService]
    });
  });

  it('should be created', inject([ProjectTaskService], (service: ProjectTaskService) => {
    expect(service).toBeTruthy();
  }));
});

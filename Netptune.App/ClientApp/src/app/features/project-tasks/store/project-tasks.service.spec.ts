import { TestBed } from '@angular/core/testing';

import { ProjectTasksService } from './project-tasks.service';

describe('ProjectTasksService', () => {
  beforeEach(() => TestBed.configureTestingModule({}));

  it('should be created', () => {
    const service: ProjectTasksService = TestBed.get(ProjectTasksService);
    expect(service).toBeTruthy();
  });
});

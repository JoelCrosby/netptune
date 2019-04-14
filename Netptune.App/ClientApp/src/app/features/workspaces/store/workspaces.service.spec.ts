import { TestBed } from '@angular/core/testing';

import { WorkspacesService } from './workspaces.service';

describe('WorkspacesService', () => {
  beforeEach(() => TestBed.configureTestingModule({}));

  it('should be created', () => {
    const service: WorkspacesService = TestBed.get(WorkspacesService);
    expect(service).toBeTruthy();
  });
});

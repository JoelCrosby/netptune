import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { ProjectTypeDialogComponent } from './project-type-dialog.component';

describe('ProjectTypeDialogComponent', () => {
  let component: ProjectTypeDialogComponent;
  let fixture: ComponentFixture<ProjectTypeDialogComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ ProjectTypeDialogComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ProjectTypeDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { TaskInlineComponent } from './task-inline.component';

describe('TaskInlineComponent', () => {
  let component: TaskInlineComponent;
  let fixture: ComponentFixture<TaskInlineComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ TaskInlineComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(TaskInlineComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

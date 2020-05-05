import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { TaskListGroupComponent } from './task-list-group.component';

describe('TaskListGroupComponent', () => {
  let component: TaskListGroupComponent;
  let fixture: ComponentFixture<TaskListGroupComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ TaskListGroupComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(TaskListGroupComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

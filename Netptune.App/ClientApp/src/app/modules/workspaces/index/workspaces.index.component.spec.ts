/* tslint:disable:no-unused-variable */
import { async, ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { DebugElement } from '@angular/core';

import { WorkspacesComponent } from './workspaces.index.component';

describe('DashboardComponent', () => {
  let component: WorkspacesComponent;
  let fixture: ComponentFixture<WorkspacesComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [WorkspacesComponent]
    })
      .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(WorkspacesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

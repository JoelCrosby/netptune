import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { BoardGroupTaskInlineComponent } from './board-group-task-inline.component';

describe('BoardGroupTaskInlineComponent', () => {
  let component: BoardGroupTaskInlineComponent;
  let fixture: ComponentFixture<BoardGroupTaskInlineComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ BoardGroupTaskInlineComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(BoardGroupTaskInlineComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

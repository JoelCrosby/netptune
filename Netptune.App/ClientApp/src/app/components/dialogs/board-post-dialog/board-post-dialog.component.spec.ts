import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { BoardPostDialogComponent } from './board-post-dialog.component';

describe('BoardPostDialogComponent', () => {
  let component: BoardPostDialogComponent;
  let fixture: ComponentFixture<BoardPostDialogComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ BoardPostDialogComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(BoardPostDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

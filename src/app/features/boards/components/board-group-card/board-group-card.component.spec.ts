import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { BoardGroupCardComponent } from './board-group-card.component';

describe('BoardGroupCardComponent', () => {
  let component: BoardGroupCardComponent;
  let fixture: ComponentFixture<BoardGroupCardComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ BoardGroupCardComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(BoardGroupCardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

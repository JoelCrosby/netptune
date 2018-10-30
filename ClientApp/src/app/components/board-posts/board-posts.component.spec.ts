import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { BoardPostsComponent } from './board-posts.component';

describe('BoardPostsComponent', () => {
  let component: BoardPostsComponent;
  let fixture: ComponentFixture<BoardPostsComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ BoardPostsComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(BoardPostsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

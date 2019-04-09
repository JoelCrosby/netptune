import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { SettingsIndexComponent } from './settings.index.component';

describe('Settings.IndexComponent', () => {
  let component: SettingsIndexComponent;
  let fixture: ComponentFixture<SettingsIndexComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [SettingsIndexComponent],
    }).compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(SettingsIndexComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';

@Component({
  templateUrl: './users-view.component.html',
  styleUrls: ['./users-view.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UsersViewComponent implements OnInit {
  constructor() {}

  ngOnInit(): void {}
}

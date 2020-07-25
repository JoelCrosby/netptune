import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';

@Component({
  templateUrl: './boards-view.component.html',
  styleUrls: ['./boards-view.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BoardsViewComponent implements OnInit {
  constructor() {}

  ngOnInit() {}
}

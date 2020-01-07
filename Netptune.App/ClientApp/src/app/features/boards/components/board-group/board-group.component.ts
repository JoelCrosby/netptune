import { Component, OnInit, Input } from '@angular/core';
import { BoardGroup } from '@app/core/models/board-group';

@Component({
  selector: 'app-board-group',
  templateUrl: './board-group.component.html',
  styleUrls: ['./board-group.component.scss'],
})
export class BoardGroupComponent implements OnInit {
  @Input() group: BoardGroup;

  ngOnInit() {}
}

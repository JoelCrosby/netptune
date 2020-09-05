import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { CreateBoardComponent } from '@boards/components/create-board/create-board.component';

@Component({
  templateUrl: './boards-view.component.html',
  styleUrls: ['./boards-view.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BoardsViewComponent implements OnInit {
  constructor(private dialog: MatDialog) {}

  ngOnInit() {}

  onCreateBoardClicked() {
    this.dialog.open(CreateBoardComponent, {
      width: '600px',
    });
  }
}

import {
  Component,
  OnInit,
  ViewChild,
  ElementRef,
  AfterViewInit,
} from '@angular/core';

@Component({
  selector: 'app-board-group-task-inline',
  templateUrl: './board-group-task-inline.component.html',
  styleUrls: ['./board-group-task-inline.component.scss'],
})
export class BoardGroupTaskInlineComponent implements OnInit, AfterViewInit {
  @ViewChild('taskInput') inputElementRef: ElementRef;

  constructor() {}

  ngOnInit() {}

  ngAfterViewInit(): void {
    this.inputElementRef.nativeElement.focus();
  }
}

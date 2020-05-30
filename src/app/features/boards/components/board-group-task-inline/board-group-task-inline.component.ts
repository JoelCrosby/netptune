import {
  AfterViewInit,
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  ElementRef,
  EventEmitter,
  OnDestroy,
  OnInit,
  Output,
  ViewChild,
} from '@angular/core';
import { fromEvent, Subject, Subscription } from 'rxjs';
import { takeUntil, tap, throttleTime } from 'rxjs/operators';

@Component({
  selector: 'app-board-group-task-inline',
  templateUrl: './board-group-task-inline.component.html',
  styleUrls: ['./board-group-task-inline.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BoardGroupTaskInlineComponent
  implements OnInit, OnDestroy, AfterViewInit {
  @ViewChild('taskInput') inputElementRef: ElementRef;
  @ViewChild('taskInlineContainer') containerElementRef: ElementRef;

  @Output() canceled = new EventEmitter();

  onDestroy$ = new Subject();
  outsideClickSubscription: Subscription;

  constructor(private cd: ChangeDetectorRef) {}

  ngOnInit() {
    fromEvent(document, 'mousedown', {
      passive: true,
    })
      .pipe(
        takeUntil(this.onDestroy$),
        throttleTime(200),
        tap(this.handleDocumentClick.bind(this))
      )
      .subscribe();
  }

  ngAfterViewInit() {
    this.inputElementRef.nativeElement.focus();
  }

  ngOnDestroy() {
    this.onDestroy$.next();
    this.onDestroy$.complete();
  }

  handleDocumentClick(event: Event) {
    if (!this.containerElementRef.nativeElement.contains(event.target)) {
      this.canceled.emit();
      this.cd.detectChanges();
    }
  }
}

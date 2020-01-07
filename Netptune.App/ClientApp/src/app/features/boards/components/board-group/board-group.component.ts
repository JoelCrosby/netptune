import { takeUntil } from 'rxjs/operators';
import {
  AfterViewInit,
  Component,
  ElementRef,
  Input,
  OnDestroy,
  OnInit,
  ViewChild,
} from '@angular/core';
import { BoardGroup } from '@app/core/models/board-group';
import { BehaviorSubject, fromEvent, Subject } from 'rxjs';

@Component({
  selector: 'app-board-group',
  templateUrl: './board-group.component.html',
  styleUrls: ['./board-group.component.scss'],
})
export class BoardGroupComponent implements OnInit, OnDestroy, AfterViewInit {
  @Input() group: BoardGroup;
  @ViewChild('container') container: ElementRef;

  focusedSubject = new BehaviorSubject<boolean>(false);
  onDestroy$ = new Subject();

  focused$ = this.focusedSubject.pipe();

  ngOnInit() {}

  ngAfterViewInit() {
    const el = this.container.nativeElement;

    fromEvent(el, 'mouseenter', { passive: true })
      .pipe(takeUntil(this.onDestroy$))
      .subscribe({
        next: () => this.focusedSubject.next(true),
      });

    fromEvent(el, 'mouseleave', { passive: true })
      .pipe(takeUntil(this.onDestroy$))
      .subscribe({
        next: () => this.focusedSubject.next(false),
      });
  }

  ngOnDestroy() {
    this.onDestroy$.next();
    this.onDestroy$.complete();
  }
}

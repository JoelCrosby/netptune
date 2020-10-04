import { Injectable } from '@angular/core';
import { fromEvent, Subject } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class DocumentService {
  private documentClickedTarget = new Subject<EventTarget>();

  constructor() {
    fromEvent(document, 'click').subscribe({
      next: (el) => this.documentClickedTarget.next(el.target),
    });
  }

  documentClicked() {
    return this.documentClickedTarget.pipe();
  }
}

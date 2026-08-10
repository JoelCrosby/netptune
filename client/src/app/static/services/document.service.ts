import { Service, signal } from '@angular/core';

@Service()
export class DocumentService {
  readonly documentClicked = signal<EventTarget>(document, {
    equal: () => false,
  });

  constructor() {
    document.addEventListener('click', (el) => {
      if (!el?.target) return;
      this.documentClicked.set(el.target);
    });
  }
}

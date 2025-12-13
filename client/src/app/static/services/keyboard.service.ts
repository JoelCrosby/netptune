import { Injectable, signal } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class KeyboardService {
  readonly keyDown = signal<KeyboardEvent>(null!, {
    equal: () => false,
  });

  constructor() {
    document.addEventListener('keydown', (el) => this.keyDown.set(el), {
      passive: true,
    });
  }
}

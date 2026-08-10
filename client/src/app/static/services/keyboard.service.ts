import { Service, signal } from '@angular/core';

@Service()
export class KeyboardService {
  readonly keyDown = signal<KeyboardEvent | null>(null, {
    equal: () => false,
  });

  constructor() {
    document.addEventListener('keydown', (el) => this.keyDown.set(el), {
      passive: true,
    });
  }
}

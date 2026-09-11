import { Service, signal } from '@angular/core';

@Service()
export class KeyboardService {
  readonly keyDown = signal<KeyboardEvent | null>(null, {
    equal: () => false,
  });

  constructor() {
    document.addEventListener(
      'keydown',
      (event: Event) => {
        const isKeyPress = event instanceof KeyboardEvent;

        if (!isKeyPress) {
          return;
        }

        this.keyDown.set(event);
      },
      { passive: true }
    );
  }
}

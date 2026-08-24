import { Service, signal } from '@angular/core';

// The board owns the image but the shell owns <main>, the only element that spans the whole
// board surface. The board view publishes here and the shell paints it.
@Service()
export class BoardBackgroundService {
  private readonly current = signal<string | null>(null);

  readonly imageUrl = this.current.asReadonly();

  set(url: string | null) {
    this.current.set(url);
  }

  clear() {
    this.current.set(null);
  }
}

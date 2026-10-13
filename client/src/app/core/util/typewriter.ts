/**
 * Draining the backlog over a fixed window rather than at a fixed speed keeps the reveal
 * in step with the stream: a burst types faster, a trickle types at the floor below.
 */
const DRAIN_WINDOW_MS = 220;
const MINIMUM_CHARACTERS_PER_SECOND = 120;

// A word with no space after it may still be arriving, so it waits this long for the rest
// before showing as it is. The stream ending is never signalled here, so the wait must end.
const PARTIAL_WORD_HOLD_MS = 150;

const WORD_END = /\s/;

export interface TypewriterOptions {
  // Called each time everything received so far has been written.
  drained?: () => void;
  maxCharactersPerSecond?: number;
  byCharacter?: boolean;
}

export class Typewriter {
  private readonly reducedMotion = matchMedia(
    '(prefers-reduced-motion: reduce)'
  );

  private pending = '';
  private frame: number | null = null;
  private lastFrameAt = 0;
  private carriedCharacters = 0;
  private lastPushAt = 0;

  constructor(
    private readonly write: (text: string) => void,
    private readonly options: TypewriterOptions = {}
  ) {}

  get isIdle(): boolean {
    return this.frame === null && this.pending.length === 0;
  }

  push(text: string) {
    if (this.reducedMotion.matches) {
      this.write(text);
      this.options.drained?.();

      return;
    }

    this.pending += text;
    this.lastPushAt = performance.now();
    this.start();
  }

  /** Ends the reveal early with everything received. */
  flush() {
    this.stop();

    const remaining = this.pending;

    this.pending = '';

    if (remaining.length > 0) {
      this.write(remaining);
    }
  }

  /** Drops text that is being discarded or replaced, so it is never written. */
  discard() {
    this.stop();
    this.pending = '';
  }

  private start() {
    const isRunning = this.frame !== null;

    if (isRunning) {
      return;
    }

    this.lastFrameAt = performance.now();
    this.carriedCharacters = 0;
    this.schedule();
  }

  private stop() {
    const frame = this.frame;

    if (frame === null) {
      return;
    }

    cancelAnimationFrame(frame);
    this.frame = null;
  }

  private schedule() {
    this.frame = requestAnimationFrame((now) => this.tick(now));
  }

  private tick(now: number) {
    const elapsedMs = now - this.lastFrameAt;

    this.lastFrameAt = now;
    this.frame = null;

    const taken = this.take(elapsedMs);
    const count = this.options.byCharacter ? taken : this.toWordEnd(taken, now);
    const isCapped = this.options.maxCharactersPerSecond !== undefined;

    // Rounding up to the end of a word runs ahead of the rate; a cap pays that back from the
    // next frames, or the words would come out well over it.
    if (isCapped) {
      this.carriedCharacters -= count - taken;
    }

    if (count > 0) {
      this.write(this.pending.slice(0, count));
      this.pending = this.pending.slice(count);
    }

    const hasMore = this.pending.length > 0;

    if (hasMore) {
      this.schedule();

      return;
    }

    this.options.drained?.();
  }

  // Revealing whole words keeps half-typed words and unclosed markdown off the screen.
  private toWordEnd(count: number, now: number): number {
    if (count === 0) {
      return 0;
    }

    const rest = this.pending.slice(count - 1);
    const boundary = rest.search(WORD_END);

    if (boundary >= 0) {
      return count + boundary;
    }

    const hasSettled = now - this.lastPushAt >= PARTIAL_WORD_HOLD_MS;

    if (hasSettled) {
      return this.pending.length;
    }

    const lastBoundary = this.pending.slice(0, count).search(/\s\S*$/);

    return lastBoundary >= 0 ? lastBoundary + 1 : 0;
  }

  private take(elapsedMs: number): number {
    const catchUpRate = (this.pending.length / DRAIN_WINDOW_MS) * 1000;
    const paced = Math.max(MINIMUM_CHARACTERS_PER_SECOND, catchUpRate);
    const perSecond = Math.min(
      paced,
      this.options.maxCharactersPerSecond ?? Infinity
    );
    const exact = this.carriedCharacters + (perSecond * elapsedMs) / 1000;
    const count = Math.max(0, Math.floor(exact));

    this.carriedCharacters = exact - count;

    return Math.min(count, this.pending.length);
  }
}

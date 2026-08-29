import { Service, inject } from '@angular/core';
import { AiTranscriptService } from '@core/services/ai-transcript.service';

/**
 * Draining the backlog over a fixed window rather than at a fixed speed keeps the reveal
 * in step with the stream: a burst types faster, a trickle types at the floor below.
 */
const DRAIN_WINDOW_MS = 220;
const MINIMUM_CHARACTERS_PER_SECOND = 120;

@Service()
export class AiTypewriterService {
  private readonly transcript = inject(AiTranscriptService);
  private readonly reducedMotion = matchMedia(
    '(prefers-reduced-motion: reduce)'
  );

  private pending = '';
  private frame: number | null = null;
  private lastFrameAt = 0;
  private carriedCharacters = 0;

  push(text: string) {
    if (this.reducedMotion.matches) {
      this.transcript.appendText(text);

      return;
    }

    this.pending += text;
    this.start();
  }

  /** Ends the reveal early with everything received, for a turn the user stopped. */
  flush() {
    this.stop();

    const remaining = this.pending;

    this.pending = '';

    if (remaining.length > 0) {
      this.transcript.appendText(remaining);
    }
  }

  /** Drops text whose reply is being discarded or replaced, so it never reaches the transcript. */
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

    const count = this.take(elapsedMs);

    if (count > 0) {
      this.transcript.appendText(this.pending.slice(0, count));
      this.pending = this.pending.slice(count);
    }

    const hasMore = this.pending.length > 0;

    if (hasMore) {
      this.schedule();
    }
  }

  private take(elapsedMs: number): number {
    const catchUpRate = (this.pending.length / DRAIN_WINDOW_MS) * 1000;
    const perSecond = Math.max(MINIMUM_CHARACTERS_PER_SECOND, catchUpRate);
    const exact = this.carriedCharacters + (perSecond * elapsedMs) / 1000;
    const count = Math.floor(exact);

    this.carriedCharacters = exact - count;

    return Math.min(count, this.pending.length);
  }
}

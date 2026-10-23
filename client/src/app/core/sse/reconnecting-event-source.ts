import { Logger } from '@core/util/logger';

const INITIAL_RETRY_DELAY = 2_000;
const MAX_RETRY_DELAY = 60_000;

/**
 * EventSource retries a dropped connection itself, but gives up for good on a non-200
 * response such as a 429 from the server's stream cap, so that case is retried here.
 */
export class ReconnectingEventSource {
  private source: EventSource | null = null;
  private retryTimer: ReturnType<typeof setTimeout> | null = null;
  private retryDelay = INITIAL_RETRY_DELAY;

  constructor(
    private readonly name: string,
    private readonly url: string,
    private readonly configure: (source: EventSource) => void
  ) {}

  open() {
    this.close();
    this.connect();
  }

  close() {
    this.cancelRetry();
    this.retryDelay = INITIAL_RETRY_DELAY;

    if (!this.source) return;

    this.source.close();
    this.source = null;
  }

  private connect() {
    const source = new EventSource(this.url, { withCredentials: true });

    source.addEventListener('open', () => {
      this.retryDelay = INITIAL_RETRY_DELAY;
    });

    source.addEventListener('error', () => {
      const hasGivenUp = source.readyState === EventSource.CLOSED;

      if (hasGivenUp) {
        this.scheduleRetry();
      }
    });

    this.configure(source);
    this.source = source;
  }

  private scheduleRetry() {
    this.source = null;

    const jitter = Math.random() * 1_000;
    const delay = this.retryDelay + jitter;

    this.retryDelay = Math.min(this.retryDelay * 2, MAX_RETRY_DELAY);

    Logger.warn(`[SSE][${this.name}] retrying in ${Math.round(delay / 1000)}s`);

    this.retryTimer = setTimeout(() => {
      this.retryTimer = null;
      this.connect();
    }, delay);
  }

  private cancelRetry() {
    if (this.retryTimer === null) return;

    clearTimeout(this.retryTimer);
    this.retryTimer = null;
  }
}

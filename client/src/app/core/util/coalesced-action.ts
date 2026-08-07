// Runs an action at most once per window, so a burst of events costs one run rather than one each.
// `now()` bypasses the window for the event that must not wait.
export class CoalescedAction {
  private timer: ReturnType<typeof setTimeout> | null = null;

  constructor(
    private readonly action: () => void,
    private readonly windowMs: number
  ) {}

  schedule(): void {
    if (this.timer) return;

    this.timer = setTimeout(() => {
      this.timer = null;
      this.action();
    }, this.windowMs);
  }

  now(): void {
    this.cancel();
    this.action();
  }

  cancel(): void {
    if (!this.timer) return;

    clearTimeout(this.timer);
    this.timer = null;
  }
}

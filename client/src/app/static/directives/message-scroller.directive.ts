import {
  DestroyRef,
  Directive,
  ElementRef,
  afterNextRender,
  computed,
  inject,
  input,
  signal,
} from '@angular/core';

export type ScrollAlignment = 'start' | 'end';

interface AnchorState {
  messageId: string;
  offset: number;
}

const DEFAULT_EDGE_THRESHOLD = 24;
const DEFAULT_SCROLL_MARGIN = 12;

/**
 * Owns transcript scrolling: anchoring a new turn, following streamed output,
 * holding the reader's place when older rows are prepended, and reporting which
 * rows are on screen. Rows opt in with `appMessageScrollerItem`.
 */
@Directive({
  selector: '[appMessageScroller]',
  exportAs: 'messageScroller',
  host: {
    'data-scroll-viewport': '',
    '(scroll)': 'onScroll()',
    '(wheel)': 'onUserGesture()',
    '(touchmove)': 'onUserGesture()',
    '(keydown)': 'onUserGesture()',
  },
})
export class MessageScrollerDirective {
  readonly autoScroll = input(true);
  readonly scrollEdgeThreshold = input(DEFAULT_EDGE_THRESHOLD);
  readonly scrollMargin = input(DEFAULT_SCROLL_MARGIN);
  readonly preserveScrollOnPrepend = input(true);

  private readonly elementRef = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly destroyRef = inject(DestroyRef);
  private readonly items = new Map<string, HTMLElement>();
  private readonly anchorIds = new Set<string>();

  private readonly visible = signal<ReadonlySet<string>>(new Set());
  private readonly following = signal(true);
  private readonly scrollable = signal({ start: false, end: false });

  private anchored: AnchorState | null = null;
  private lastScrollHeight = 0;
  private probe: HTMLElement | null = null;
  private probeOffset = 0;
  private observer?: ResizeObserver;

  readonly atEnd = computed(() => !this.scrollable().end);
  readonly atStart = computed(() => !this.scrollable().start);
  readonly visibleMessageIds = computed(() => [...this.visible()]);

  readonly currentAnchorId = computed(() => {
    const onScreen = this.visible();
    const anchors = [...this.anchorIds].filter((id) => onScreen.has(id));

    return anchors.at(-1) ?? null;
  });

  constructor() {
    afterNextRender(() => {
      this.lastScrollHeight = this.viewport.scrollHeight;
      this.trackContentStart();
      this.observeContent();
      this.measure();
    });

    this.destroyRef.onDestroy(() => this.observer?.disconnect());
  }

  private get viewport(): HTMLElement {
    return this.elementRef.nativeElement;
  }

  register(messageId: string, element: HTMLElement, isAnchor: boolean) {
    this.items.set(messageId, element);
    this.observer?.observe(element);

    if (isAnchor) {
      this.anchorIds.add(messageId);
    }
  }

  unregister(messageId: string) {
    const element = this.items.get(messageId);

    if (element) {
      this.observer?.unobserve(element);
    }

    this.items.delete(messageId);
    this.anchorIds.delete(messageId);
    this.visible.update((current) => {
      const next = new Set(current);

      next.delete(messageId);

      return next;
    });
  }

  setVisible(messageId: string, isVisible: boolean) {
    this.visible.update((current) => {
      const next = new Set(current);

      if (isVisible) {
        next.add(messageId);
      } else {
        next.delete(messageId);
      }

      return next;
    });
  }

  scrollToEnd(behavior: ScrollBehavior = 'auto') {
    this.anchored = null;
    this.following.set(true);
    this.viewport.scrollTo({ top: this.viewport.scrollHeight, behavior });
  }

  scrollToStart(behavior: ScrollBehavior = 'smooth') {
    this.following.set(false);
    this.viewport.scrollTo({ top: 0, behavior });
  }

  scrollToMessage(messageId: string, alignment: ScrollAlignment = 'start') {
    const element = this.items.get(messageId);

    if (!element) {
      return;
    }

    this.following.set(false);
    this.viewport.scrollTo({ top: this.offsetFor(element, alignment) });
  }

  /**
   * Pins the newest turn below the viewport top so the reply that follows has
   * room to stream into. Following resumes once the reply outgrows the fold.
   */
  anchorTurn(messageId: string) {
    const element = this.items.get(messageId);

    if (!element) {
      this.scrollToEnd();

      return;
    }

    const offset = this.offsetFor(element, 'start');

    this.following.set(false);
    this.anchored = { messageId, offset };
    this.viewport.scrollTo({ top: offset, behavior: 'smooth' });
  }

  protected onScroll() {
    this.measure();

    const isAtEnd = this.distanceFromEnd() <= this.scrollEdgeThreshold();

    this.following.set(isAtEnd);

    if (isAtEnd) {
      this.anchored = null;
    }
  }

  /**
   * A deliberate gesture always hands control back to the reader, including
   * during the window where a turn is anchored and holding its position.
   */
  protected onUserGesture() {
    this.anchored = null;
  }

  private observeContent() {
    this.observer = new ResizeObserver(() => this.onContentResize());
    this.observer.observe(this.viewport);

    for (const child of Array.from(this.viewport.children)) {
      this.observer.observe(child);
    }
  }

  private onContentResize() {
    const viewport = this.viewport;
    const grew = viewport.scrollHeight > this.lastScrollHeight;
    const prepended = this.prependedHeight();

    this.lastScrollHeight = viewport.scrollHeight;
    this.trackContentStart();
    this.measure();

    if (prepended > 0) {
      viewport.scrollTop += prepended;

      return;
    }

    if (!grew || !this.autoScroll()) {
      return;
    }

    if (this.anchored) {
      this.followFromAnchor();

      return;
    }

    if (this.following()) {
      viewport.scrollTop = viewport.scrollHeight;
    }
  }

  /**
   * Rows that land above the reader — older history — must not move the page
   * under them, so whatever they added is handed back to the scroll offset.
   */
  private prependedHeight(): number {
    const probe = this.probe;
    const isConnected = probe?.isConnected === true;

    if (!this.preserveScrollOnPrepend() || !isConnected) {
      return 0;
    }

    const moved = probe.offsetTop - this.probeOffset;
    const isReading = this.viewport.scrollTop > this.scrollEdgeThreshold();

    return isReading ? Math.max(0, moved) : 0;
  }

  private trackContentStart() {
    const first = this.viewport.querySelector<HTMLElement>('[data-message-id]');

    this.probe = first;
    this.probeOffset = first?.offsetTop ?? 0;
  }

  private followFromAnchor() {
    const viewport = this.viewport;
    const anchor = this.anchored;

    if (!anchor) {
      return;
    }

    const outgrewTheFold =
      viewport.scrollHeight - anchor.offset > viewport.clientHeight;

    if (!outgrewTheFold) {
      viewport.scrollTop = anchor.offset;

      return;
    }

    this.anchored = null;
    this.following.set(true);
    viewport.scrollTop = viewport.scrollHeight;
  }

  private offsetFor(element: HTMLElement, alignment: ScrollAlignment): number {
    const viewport = this.viewport;
    const top = element.offsetTop - viewport.offsetTop - this.scrollMargin();

    if (alignment === 'start') {
      return Math.max(0, top);
    }

    return Math.max(0, top + element.offsetHeight - viewport.clientHeight);
  }

  private distanceFromEnd(): number {
    const viewport = this.viewport;

    return viewport.scrollHeight - viewport.scrollTop - viewport.clientHeight;
  }

  private measure() {
    const viewport = this.viewport;

    this.scrollable.set({
      start: viewport.scrollTop > this.scrollEdgeThreshold(),
      end: this.distanceFromEnd() > this.scrollEdgeThreshold(),
    });
  }
}

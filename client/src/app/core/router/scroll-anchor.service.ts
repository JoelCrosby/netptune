import { inject, Service } from '@angular/core';
import { NavigationEnd, Router } from '@angular/router';
import { pathOf } from '@core/util/url';
import { filter } from 'rxjs';

const ANCHOR_TRACKING_MS = 5_000;
const USER_SCROLL_EVENTS = ['wheel', 'touchstart', 'pointerdown', 'keydown'];

interface AnchorTarget {
  id: string;
  smooth: boolean;
}

@Service()
export class ScrollAnchorService {
  private readonly router = inject(Router);
  private readonly elements = new Map<string, HTMLElement>();

  private target: AnchorTarget | null = null;
  private expiry: ReturnType<typeof setTimeout> | null = null;
  private frame: number | null = null;
  private previousUrl = this.router.url;

  private readonly clearTarget = () => this.clear();

  constructor() {
    const initialFragment = fragmentOf(this.previousUrl);

    if (initialFragment) this.request(initialFragment);

    this.router.events
      .pipe(filter((event) => event instanceof NavigationEnd))
      .subscribe((event) => this.onNavigated(event.urlAfterRedirects));
  }

  register(id: string, element: HTMLElement) {
    this.elements.set(id, element);

    if (this.target?.id === id) this.scheduleScroll(element, this.target);
  }

  unregister(id: string, element: HTMLElement) {
    const isCurrentElement = this.elements.get(id) === element;

    if (!isCurrentElement) return;

    this.elements.delete(id);

    if (this.target?.id === id) this.cancelFrame();
  }

  request(id: string, smooth = false) {
    this.clear();

    const target = { id, smooth };

    this.target = target;
    this.expiry = setTimeout(this.clearTarget, ANCHOR_TRACKING_MS);

    for (const event of USER_SCROLL_EVENTS) {
      window.addEventListener(event, this.clearTarget, {
        capture: true,
        passive: true,
      });
    }

    const element = this.elements.get(id);

    if (element) this.scheduleScroll(element, target);
  }

  private onNavigated(url: string) {
    const fragment = fragmentOf(url);
    const previousFragment = fragmentOf(this.previousUrl);
    const isSamePath = pathOf(url) === pathOf(this.previousUrl);

    this.previousUrl = url;

    if (isSamePath && fragment === previousFragment) return;

    if (fragment) {
      this.request(fragment, isSamePath);
    } else {
      this.clear();
    }
  }

  private scheduleScroll(element: HTMLElement, target: AnchorTarget) {
    this.cancelFrame();

    this.frame = requestAnimationFrame(() => {
      this.frame = null;
      this.scrollTo(element, target);
    });
  }

  private scrollTo(element: HTMLElement, { smooth }: AnchorTarget) {
    const reducedMotion = matchMedia(
      '(prefers-reduced-motion: reduce)'
    ).matches;
    const behavior = smooth && !reducedMotion ? 'smooth' : 'instant';

    element.scrollIntoView({ behavior, block: 'start' });
    element.focus({ preventScroll: true });

    if (smooth) {
      this.clear();

      return;
    }

    this.keepAligned(element);
  }

  // Sections load after the anchor renders. Those above push it down, and those
  // below can make a page that was too short to align it tall enough to.
  private keepAligned(element: HTMLElement) {
    const align = () => {
      element.scrollIntoView({ behavior: 'instant', block: 'start' });
      this.frame = requestAnimationFrame(align);
    };

    this.frame = requestAnimationFrame(align);
  }

  private clear() {
    this.target = null;
    this.cancelFrame();

    if (this.expiry !== null) {
      clearTimeout(this.expiry);
      this.expiry = null;
    }

    for (const event of USER_SCROLL_EVENTS) {
      window.removeEventListener(event, this.clearTarget, { capture: true });
    }
  }

  private cancelFrame() {
    if (this.frame === null) return;

    cancelAnimationFrame(this.frame);
    this.frame = null;
  }
}

function fragmentOf(url: string): string | null {
  const index = url.indexOf('#');

  return index === -1 ? null : url.slice(index + 1);
}

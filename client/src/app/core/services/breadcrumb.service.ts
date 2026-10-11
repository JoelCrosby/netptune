import {
  computed,
  DestroyRef,
  inject,
  linkedSignal,
  Service,
  Signal,
  signal,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, NavigationEnd, Router } from '@angular/router';
import { BreadcrumbItem } from '@static/components/breadcrumb/breadcrumb.component';
import { filter, map } from 'rxjs';

export type BreadcrumbLabel =
  string | ((route: ActivatedRoute) => string | null | undefined);

export type BreadcrumbOverride = string | readonly BreadcrumbItem[];

interface RouteCrumb {
  route: ActivatedRoute;
  item: BreadcrumbItem;
}

interface Override {
  owner: object;
  route: ActivatedRoute;
  items: Signal<readonly BreadcrumbItem[] | null>;
}

@Service()
export class BreadcrumbService {
  private readonly router = inject(Router);

  private readonly routeCrumbs = toSignal(
    this.router.events.pipe(
      filter((event) => event instanceof NavigationEnd),
      map(() => this.collect())
    ),
    { initialValue: this.collect() }
  );

  private readonly override = signal<Override | null>(null);

  readonly items = computed<readonly BreadcrumbItem[]>(() => {
    const crumbs = this.routeCrumbs();
    const override = this.override();
    const overrideItems = override?.items();

    if (!override || !overrideItems) {
      return crumbs.map((crumb) => crumb.item);
    }

    const owned = crumbs.findIndex((crumb) => crumb.route === override.route);
    const kept = owned === -1 ? crumbs : crumbs.slice(0, owned);

    return [...kept.map((crumb) => crumb.item), ...overrideItems];
  });

  register(
    owner: object,
    route: ActivatedRoute,
    items: Signal<readonly BreadcrumbItem[] | null>
  ) {
    this.override.set({ owner, route, items });
  }

  clear(owner: object) {
    this.override.update((current) => {
      return current?.owner === owner ? null : current;
    });
  }

  private collect(): RouteCrumb[] {
    const crumbs: RouteCrumb[] = [];
    let route: ActivatedRoute | null = this.router.routerState.root;

    while (route && this.isActivated(route)) {
      const label = this.labelOf(route);
      const previous = crumbs.at(-1);

      if (label && label !== previous?.item.label) {
        crumbs.push({
          route,
          item: {
            id: `route-${crumbs.length}`,
            label,
            link: this.linkOf(route),
            icon: route.routeConfig?.data?.['icon'],
          },
        });
      } else if (previous && this.ownsEmptyPath(route)) {
        previous.route = route;
      }

      route = route.firstChild;
    }

    return crumbs;
  }

  private labelOf(route: ActivatedRoute): string | null {
    const data = route.routeConfig?.data;
    const breadcrumb = data?.['breadcrumb'] as BreadcrumbLabel | undefined;

    if (typeof breadcrumb === 'function') {
      return breadcrumb(route) ?? null;
    }

    return breadcrumb ?? data?.['title'] ?? null;
  }

  private linkOf(route: ActivatedRoute): string[] {
    const segments = route.snapshot.pathFromRoot.flatMap((snapshot) => {
      return snapshot.url.map((segment) => segment.path);
    });

    return ['/', ...segments];
  }

  // Routes created mid-navigation have no snapshot until they are activated
  private isActivated(route: ActivatedRoute): boolean {
    return route.snapshot !== undefined;
  }

  private ownsEmptyPath(route: ActivatedRoute): boolean {
    return route.snapshot.url.length === 0;
  }
}

export function injectBreadcrumbs(
  value: () => BreadcrumbOverride | null | undefined,
  loading: () => boolean = () => false
) {
  const service = inject(BreadcrumbService);
  const owner = {};

  const items = linkedSignal<
    { loading: boolean; value: BreadcrumbOverride | null | undefined },
    readonly BreadcrumbItem[] | null
  >({
    source: () => ({ loading: loading(), value: value() }),
    computation: (source, previous) => {
      if (source.loading) {
        return previous?.value ?? null;
      }

      return toBreadcrumbItems(source.value);
    },
  });

  service.register(owner, inject(ActivatedRoute), items);

  inject(DestroyRef).onDestroy(() => service.clear(owner));
}

function toBreadcrumbItems(
  override: BreadcrumbOverride | null | undefined
): readonly BreadcrumbItem[] | null {
  if (!override) return null;

  if (typeof override === 'string') {
    return [{ id: 'current', label: override }];
  }

  return override;
}

import { BreakpointObserver, BreakpointState } from '@angular/cdk/layout';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { map, distinctUntilChanged } from 'rxjs/operators';

export enum MediaSize {
  'xs' = '575.98px',
  's' = '767.98px',
  'm' = '991.98px',
  'l' = '1199.98px',
  'xl' = '1599.98px',
}

@Injectable({ providedIn: 'root' })
export class MediaService {
  constructor(private breakpointObserver: BreakpointObserver) {}

  minWidth(mediaSize: MediaSize): Observable<boolean> {
    return this.breakpointObserver.observe([`(min-width: ${mediaSize})`]).pipe(
      map((res) => res.matches),
      distinctUntilChanged()
    );
  }

  maxWidth(mediaSize: MediaSize): Observable<boolean> {
    return this.breakpointObserver.observe([`(max-width: ${mediaSize})`]).pipe(
      map((res) => res.matches),
      distinctUntilChanged()
    );
  }

  matchesExact(
    sizeInPixels: number,
    query: 'max-width' | 'min-width' = 'max-width'
  ): Observable<boolean> {
    return this.breakpointObserver
      .observe([`(${query}: ${sizeInPixels}px)`])
      .pipe(
        map((res) => res.matches),
        distinctUntilChanged()
      );
  }

  getMobileQuery(mediaSize: MediaSize): Observable<BreakpointState> {
    return this.breakpointObserver.observe([mediaSize]);
  }
}

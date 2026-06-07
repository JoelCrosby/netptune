import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { ClientResponse } from '@core/models/client-response';
import { unwrapClientReposne } from '@core/util/rxjs-operators';
import { forkJoin, map, of, switchMap } from 'rxjs';
import {
  AutomationRule,
  AutomationRuleListItem,
  AutomationRuleRequest,
  AutomationRun,
} from '../models/automation.models';

@Injectable({ providedIn: 'root' })
export class AutomationsService {
  private http = inject(HttpClient);

  getRules() {
    return this.http
      .get<ClientResponse<AutomationRule[]>>('api/automations')
      .pipe(unwrapClientReposne());
  }

  getRulesWithLastRun() {
    return this.getRules().pipe(
      switchMap((rules) => {
        if (!rules.length) return of([]);

        return forkJoin(
          rules.map((rule) =>
            this.getRuns(rule.id).pipe(
              map((runs) => ({
                ...rule,
                lastRun: runs[0] ?? null,
              }))
            )
          )
        );
      })
    );
  }

  getRule(id: number) {
    return this.http
      .get<ClientResponse<AutomationRule>>(`api/automations/${id}`)
      .pipe(unwrapClientReposne());
  }

  getRuns(id: number) {
    return this.http
      .get<ClientResponse<AutomationRun[]>>(`api/automations/${id}/runs`)
      .pipe(unwrapClientReposne());
  }

  create(request: AutomationRuleRequest) {
    return this.http
      .post<ClientResponse<AutomationRule>>('api/automations', request)
      .pipe(unwrapClientReposne());
  }

  update(id: number, request: AutomationRuleRequest) {
    return this.http
      .put<ClientResponse<AutomationRule>>(`api/automations/${id}`, request)
      .pipe(unwrapClientReposne());
  }

  enable(id: number) {
    return this.http
      .post<ClientResponse>(`api/automations/${id}/enable`, null)
      .pipe(unwrapClientReposne());
  }

  disable(id: number) {
    return this.http
      .post<ClientResponse>(`api/automations/${id}/disable`, null)
      .pipe(unwrapClientReposne());
  }

  delete(id: number) {
    return this.http
      .delete<ClientResponse>(`api/automations/${id}`)
      .pipe(unwrapClientReposne());
  }
}

export type { AutomationRuleListItem };

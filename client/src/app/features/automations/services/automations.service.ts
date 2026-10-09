import { HttpClient, HttpParams } from '@angular/common/http';
import { Service, inject } from '@angular/core';
import { ClientResponse } from '@core/models/client-response';
import { appendPageParams, Page, PageQuery } from '@core/models/pagination';
import { isNotNullOrUndefined } from '@core/util/nullish';
import { unwrapClientResponse } from '@core/util/rxjs-operators';
import {
  AutomationDryRun,
  AutomationManualRun,
  AutomationRuleSummary,
  AutomationRule,
  AutomationRuleListItem,
  AutomationRuleRequest,
  AutomationRun,
  AutomationRunFilter,
  AutomationRunSummary,
} from '../models/automation.models';

@Service()
export class AutomationsService {
  private http = inject(HttpClient);

  getSummary() {
    return this.http
      .get<ClientResponse<AutomationRuleSummary>>('api/automations/summary')
      .pipe(unwrapClientResponse());
  }

  getRule(id: number) {
    return this.http
      .get<ClientResponse<AutomationRule>>(`api/automations/${id}`)
      .pipe(unwrapClientResponse());
  }

  getRuns(id: number, query?: PageQuery, filter?: AutomationRunFilter) {
    let params = appendPageParams(new HttpParams(), query);

    if (filter?.search) params = params.set('search', filter.search);
    if (isNotNullOrUndefined(filter?.status)) {
      params = params.set('statuses', filter.status);
    }

    return this.http
      .get<ClientResponse<Page<AutomationRun>>>(`api/automations/${id}/runs`, {
        params,
      })
      .pipe(unwrapClientResponse());
  }

  dryRun(id: number, taskId: number) {
    return this.http
      .get<ClientResponse<AutomationDryRun>>(
        `api/automations/${id}/dry-run/${taskId}`
      )
      .pipe(unwrapClientResponse());
  }

  runNow(id: number, taskIds: number[]) {
    return this.http
      .post<ClientResponse<AutomationManualRun>>(`api/automations/${id}/run`, {
        taskIds,
      })
      .pipe(unwrapClientResponse());
  }

  getRunSummary(id: number) {
    return this.http
      .get<ClientResponse<AutomationRunSummary>>(
        `api/automations/${id}/runs/summary`
      )
      .pipe(unwrapClientResponse());
  }

  clone(id: number, name: string) {
    return this.http
      .post<ClientResponse<AutomationRule>>(`api/automations/${id}/clone`, {
        name,
      })
      .pipe(unwrapClientResponse());
  }

  create(request: AutomationRuleRequest) {
    return this.http
      .post<ClientResponse<AutomationRule>>('api/automations', request)
      .pipe(unwrapClientResponse());
  }

  update(id: number, request: AutomationRuleRequest) {
    return this.http
      .put<ClientResponse<AutomationRule>>(`api/automations/${id}`, request)
      .pipe(unwrapClientResponse());
  }

  enable(id: number) {
    return this.http
      .post<ClientResponse>(`api/automations/${id}/enable`, null)
      .pipe(unwrapClientResponse());
  }

  disable(id: number) {
    return this.http
      .post<ClientResponse>(`api/automations/${id}/disable`, null)
      .pipe(unwrapClientResponse());
  }

  delete(id: number) {
    return this.http
      .delete<ClientResponse>(`api/automations/${id}`)
      .pipe(unwrapClientResponse());
  }
}

export type { AutomationRuleListItem };

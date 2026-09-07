import { Signal } from '@angular/core';
import { Params } from '@angular/router';
import { PERMISSIONS } from '../auth/permissions';
import { ClientResponse } from '../models/client-response';
import {
  FlowReport,
  SprintBurndownReport,
  VelocityReport,
  WorkloadReport,
} from '../models/reporting';
import { permissionResource } from './permission.resource';

// Every report is driven by the same filter set, which reaches these either as a
// parsed query string from the reporting page or as a small object built by a
// dashboard card. Each takes that as request params, plus the id a report needs
// for its own request.

export const flowReportResource = (params: Signal<Params>) => {
  return permissionResource<FlowReport | undefined>(
    PERMISSIONS.tasks.read,
    () => ({ url: 'api/reports/flow', params: params() }),
    { parse: (response) => (response as ClientResponse<FlowReport>).payload }
  );
};

export const workloadReportResource = (params: Signal<Params>) => {
  return permissionResource<WorkloadReport | undefined>(
    PERMISSIONS.members.read,
    () => ({ url: 'api/reports/workload', params: params() }),
    {
      parse: (response) => (response as ClientResponse<WorkloadReport>).payload,
    }
  );
};

export const sprintBurndownResource = (
  sprintId: Signal<number | undefined>,
  params: Signal<Params>
) => {
  return permissionResource<SprintBurndownReport | undefined>(
    PERMISSIONS.sprints.read,
    () => {
      const id = sprintId();

      return id === undefined
        ? undefined
        : { url: `api/reports/sprints/${id}/burndown`, params: params() };
    },
    {
      parse: (response) =>
        (response as ClientResponse<SprintBurndownReport>).payload,
    }
  );
};

export const velocityReportResource = (
  projectId: Signal<number | undefined>,
  params: Signal<Params>
) => {
  return permissionResource<VelocityReport | undefined>(
    PERMISSIONS.sprints.read,
    () => {
      const id = projectId();

      return id === undefined
        ? undefined
        : {
            url: 'api/reports/velocity',
            params: { projectId: id, ...params() },
          };
    },
    {
      parse: (response) => (response as ClientResponse<VelocityReport>).payload,
    }
  );
};

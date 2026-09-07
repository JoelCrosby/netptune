import { Signal } from '@angular/core';
import { Params } from '@angular/router';
import { PERMISSIONS } from '../auth/permissions';
import {
  FlowReport,
  SprintBurndownReport,
  VelocityReport,
  WorkloadReport,
} from '../models/reporting';
import { permissionResource } from './permission.resource';

export const flowReportResource = (params: Signal<Params>) => {
  return permissionResource<FlowReport | undefined>({
    permission: PERMISSIONS.tasks.read,
    request: () => ({ url: 'api/reports/flow', params: params() }),
    parse: (response) => response.payload,
  });
};

export const workloadReportResource = (params: Signal<Params>) => {
  return permissionResource<WorkloadReport | undefined>({
    permission: PERMISSIONS.members.read,
    request: () => ({ url: 'api/reports/workload', params: params() }),
    parse: (response) => response.payload,
  });
};

export const sprintBurndownResource = (
  sprintId: Signal<number | undefined>,
  params: Signal<Params>
) => {
  return permissionResource<SprintBurndownReport | undefined>({
    permission: PERMISSIONS.sprints.read,
    request: () => {
      const id = sprintId();

      return id === undefined
        ? undefined
        : { url: `api/reports/sprints/${id}/burndown`, params: params() };
    },
    parse: (response) => response.payload,
  });
};

export const velocityReportResource = (
  projectId: Signal<number | undefined>,
  params: Signal<Params>
) => {
  return permissionResource<VelocityReport | undefined>({
    permission: PERMISSIONS.sprints.read,
    request: () => {
      const id = projectId();

      return id === undefined
        ? undefined
        : {
            url: 'api/reports/velocity',
            params: { projectId: id, ...params() },
          };
    },
    parse: (response) => response.payload,
  });
};

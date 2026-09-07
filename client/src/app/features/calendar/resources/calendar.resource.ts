import { Signal } from '@angular/core';
import { PERMISSIONS } from '@core/auth/permissions';
import { permissionResource } from '@core/resources/permission.resource';
import { CalendarViewModel } from '../models/calendar.models';

export const calendarResource = (query: Signal<string>) =>
  permissionResource<CalendarViewModel | undefined>({
    permission: PERMISSIONS.tasks.read,
    request: () => ({ url: `api/roadmap?${query()}` }),
    parse: (response) => response.payload,
  });

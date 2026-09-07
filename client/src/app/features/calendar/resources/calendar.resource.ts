import { Signal } from '@angular/core';
import { PERMISSIONS } from '@core/auth/permissions';
import { ClientResponse } from '@core/models/client-response';
import { permissionResource } from '@core/resources/permission.resource';
import { CalendarViewModel } from '../models/calendar.models';

export const calendarResource = (query: Signal<string>) =>
  permissionResource<CalendarViewModel | undefined>(
    PERMISSIONS.tasks.read,
    () => ({ url: `api/roadmap?${query()}` }),
    {
      parse: (response) =>
        (response as ClientResponse<CalendarViewModel>).payload,
    }
  );

import { Signal } from '@angular/core';
import { PERMISSONS } from '@core/auth/permissions';
import { permissionResource } from '@core/resources/permission-resource';
import { CalendarViewModel } from '../models/calendar.models';

export const calendarResource = (query: Signal<string>) =>
  permissionResource<CalendarViewModel>(PERMISSONS.tasks.read, () => ({
    url: `api/roadmap?${query()}`,
  }));

import { IconClass } from '@core/consts/icon-class';

export interface HeaderAction {
  label: string;
  click: () => void;
  icon?: string;
  iconClass?: IconClass;
}

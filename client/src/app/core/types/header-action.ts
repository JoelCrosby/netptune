import { LucideIconInput } from '@lucide/angular';
import { MenuItemColor } from '@static/components/dropdown-menu/menu-item.component';

export interface HeaderAction {
  label: string;
  click?: (overflowTrigger?: HTMLElement) => void;
  icon?: LucideIconInput;
  color?: MenuItemColor;
  isLink?: boolean;
  routerLink?: string | unknown[];
}

import { LucideIconInput } from '@lucide/angular';

export interface HeaderAction {
  label: string;
  click?: () => void;
  icon?: LucideIconInput;
  isLink?: boolean;
  routerLink?: string | unknown[];
}

import { Injectable, signal } from '@angular/core';

export interface Command {
  id: string;
  label: string;
  keywords?: string[];
  group: 'navigation' | 'actions' | 'settings';
  icon?: string;
  shortcut?: readonly string[];
  execute: () => void;
}

@Injectable({ providedIn: 'root' })
export class CommandRegistry {
  private readonly commands = signal<Command[]>([]);

  register(cmds: Command[]) {
    this.commands.update((c) => [...c, ...cmds]);
  }

  unregister(ids: string[]) {
    const idSet = new Set(ids);
    this.commands.update((c) => c.filter((cmd) => !idSet.has(cmd.id)));
  }

  filter(q: string): Command[] {
    if (!q.trim()) return this.commands();

    const lower = q.toLowerCase();
    return this.commands().filter(
      (c) =>
        c.label.toLowerCase().includes(lower) ||
        c.keywords?.some((k) => k.toLowerCase().includes(lower))
    );
  }

  all(): Command[] {
    return this.commands();
  }
}

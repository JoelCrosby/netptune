import { For } from 'solid-js';
import { Check } from 'lucide-solid';
import Section from './Section';
import { roles } from '~/data/roles';

export default function RolesSection() {
  return (
    <Section class="bg-white dark:bg-black">
      <div class="mb-14 text-center">
        <p class="mb-3 text-sm font-semibold tracking-wider text-brand uppercase">Permissions</p>
        <h2 class="text-4xl font-bold tracking-tight text-slate-900 dark:text-white">
          The right access for every person on your team.
        </h2>
        <p class="mx-auto mt-4 max-w-xl text-slate-500 dark:text-white/55">
          Four role levels give you precise control over who can see and do what — from full
          workspace ownership down to read-only client access.
        </p>
      </div>

      <div class="grid gap-5 sm:grid-cols-2 lg:grid-cols-4">
        <For each={roles}>
          {(role) => (
            <div class={`rounded-xl border p-6 ${role.color}`}>
              <div class="mb-3 flex items-center gap-2">
                <div class={`h-2.5 w-2.5 rounded-full ${role.dot}`} />
                <h3 class="text-[15px] font-bold">{role.name}</h3>
              </div>
              <p class="mb-5 text-sm leading-snug opacity-70">{role.description}</p>
              <ul class="space-y-1.5">
                <For each={role.permissions}>
                  {(p) => (
                    <li class="flex items-center gap-2 text-xs opacity-80">
                      <Check size={12} />
                      {p}
                    </li>
                  )}
                </For>
              </ul>
            </div>
          )}
        </For>
      </div>
    </Section>
  );
}

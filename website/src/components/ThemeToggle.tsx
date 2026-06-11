import { createSignal, onMount } from 'solid-js';
import { Moon, Sun } from 'lucide-solid';

export default function ThemeToggle() {
  const [dark, setDark] = createSignal(false);

  onMount(() => {
    const stored = localStorage.getItem('theme');
    const prefersDark = window.matchMedia?.('(prefers-color-scheme: dark)').matches ?? false;
    const isDark = stored !== null ? stored === 'dark' : prefersDark;

    setDark(isDark);

    document.documentElement.classList.toggle('dark', isDark);
  });

  function toggle() {
    const isDark = !dark();
    setDark(isDark);
    document.documentElement.classList.toggle('dark', isDark);
    localStorage.setItem('theme', isDark ? 'dark' : 'light');
  }

  return (
    <button
      onClick={toggle}
      aria-label={dark() ? 'Switch to light mode' : 'Switch to dark mode'}
      class="flex h-8 w-8 items-center justify-center rounded-lg text-slate-500 transition-colors hover:bg-slate-100 hover:text-slate-900 dark:text-white/50 dark:hover:bg-white/10 dark:hover:text-white"
    >
      {dark() ? <Sun size={16} /> : <Moon size={16} />}
    </button>
  );
}

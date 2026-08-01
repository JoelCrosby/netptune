import { Component } from '@angular/core';

@Component({
  selector: 'app-ai-assistant-thinking',
  host: { class: 'text-muted flex items-center gap-2 text-sm' },
  template: `
    <svg
      class="h-4 w-4 shrink-0 [stroke-linecap:round]"
      viewBox="13.25 13.25 37.5 37.5"
      fill="none"
      stroke-width="2.5"
      aria-hidden="true">
      <circle class="stroke-current" cx="32" cy="32" r="17.5"></circle>
      <path class="stroke-primary" d="M14.5 32h35"></path>
      <ellipse
        class="stroke-primary animate-globe-meridian origin-center motion-reduce:animate-none"
        cx="32"
        cy="32"
        rx="8.75"
        ry="17.5"></ellipse>
    </svg>

    <span
      class="after:animate-thinking-dots after:inline-block after:content-['...'] motion-reduce:after:animate-none"
      i18n="Shown while the assistant is preparing its reply"
      >Thinking</span
    >
  `,
})
export class AiAssistantThinkingComponent {}

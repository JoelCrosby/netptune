import type { JSX } from 'solid-js';

type SectionProps = {
  children: JSX.Element;
  class?: string;
  innerClass?: string;
  id?: string;
};

export default function Section(props: SectionProps) {
  return (
    <section id={props.id} class={`px-6 py-20 ${props.class ?? ''}`}>
      <div class={`mx-auto max-w-6xl ${props.innerClass ?? ''}`}>{props.children}</div>
    </section>
  );
}

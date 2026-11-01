import { SolidMarkdown } from 'solid-markdown';
import remarkDirective from 'remark-directive';
import remarkGfm from 'remark-gfm';
import Nav from '~/components/Nav';
import Footer from '~/components/Footer';
import { parseDocument } from '~/lib/docs-content';
import remarkDocDirectives from '~/lib/remark-doc-directives';
import '~/components/docs/markdown.css';

type LegalPageProps = {
  source: string;
};

export default function LegalPage(props: LegalPageProps) {
  const document = () => parseDocument(props.source);

  return (
    <div class="bg-mist text-ink antialiased">
      <Nav />
      <main>
        <section class="home-hero relative overflow-hidden px-6 text-white">
          <div class="home-hero-stars" />
          <div class="home-hero-grid" />

          <div class="relative mx-auto flex max-w-190 flex-col gap-4.5 pt-24 pb-20">
            <p class="m-0 font-geist-mono text-xs tracking-[0.06em] text-fuchsia-300 uppercase">
              Legal
            </p>
            <h1 class="m-0 text-[clamp(40px,5.2vw,72px)] leading-[0.98] font-bold tracking-[-0.045em] text-balance">
              {document().frontmatter.title}
            </h1>
            <p class="m-0 font-geist-mono text-xs text-white/50">
              {document().frontmatter.description}
            </p>
          </div>
        </section>

        <div class="px-6 pt-14 pb-30">
          <SolidMarkdown
            class="docs-markdown mx-auto max-w-190"
            remarkPlugins={[remarkGfm, remarkDirective, remarkDocDirectives]}
            children={document().content}
          />
        </div>
      </main>
      <Footer />
    </div>
  );
}

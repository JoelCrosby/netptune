import Nav from '~/components/Nav';
import Hero from '~/components/Hero';
import FeaturesSection from '~/components/FeaturesSection';
import AssistantBand from '~/components/AssistantBand';
import CodeSection from '~/components/CodeSection';
import FeatureGrid from '~/components/FeatureGrid';
import CtaSection from '~/components/CtaSection';
import Footer from '~/components/Footer';

export default function Home() {
  return (
    <div class="bg-mist text-ink antialiased">
      <Nav />
      <main>
        <Hero />
        <FeaturesSection />
        <AssistantBand />
        <CodeSection />
        <FeatureGrid />
        <CtaSection />
      </main>
      <Footer />
    </div>
  );
}

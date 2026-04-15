import Nav from '~/components/Nav';
import Hero from '~/components/Hero';
import FeaturesSection from '~/components/FeaturesSection';
import FeatureHighlightsSection from '~/components/FeatureHighlightsSection';
import SelfHostSection from '~/components/SelfHostSection';
import FeatureGrid from '~/components/FeatureGrid';
import RolesSection from '~/components/RolesSection';
import ComparisonSection from '~/components/ComparisonSection';
import CtaSection from '~/components/CtaSection';
import Footer from '~/components/Footer';

export default function Home() {
  return (
    <div class="bg-white dark:bg-black">
      <Nav />
      <main>
        <Hero />
        <FeaturesSection />
        <FeatureHighlightsSection />
        <SelfHostSection />
        <FeatureGrid />
        <RolesSection />
        <ComparisonSection />
        <CtaSection />
      </main>
      <Footer />
    </div>
  );
}

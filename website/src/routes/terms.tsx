import LegalPage from '~/components/LegalPage';
import source from '~/content/legal/terms.md?raw';

export default function TermsPage() {
  return <LegalPage source={source} />;
}

import LegalPage from '~/components/LegalPage';
import source from '~/content/legal/privacy.md?raw';

export default function PrivacyPage() {
  return <LegalPage source={source} />;
}

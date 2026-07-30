/**
 * Fails if any locale catalog has an untranslated message.
 */
import { existsSync, readFileSync } from 'node:fs';

const localeDir = 'src/locale';
const locales = ['fr', 'de', 'es'];

const unitPattern = /<unit id="([^"]+)">([\s\S]*?)<\/unit>/g;

let failed = false;

for (const locale of locales) {
  const path = `${localeDir}/messages.${locale}.xlf`;

  if (!existsSync(path)) {
    console.error(`check-catalogues: ${path} is missing`);
    failed = true;
    continue;
  }

  const xml = readFileSync(path, 'utf8');
  const missing = [];

  for (const [, id, body] of xml.matchAll(unitPattern)) {
    if (/<target[^>]*>/.test(body)) continue;

    const source = /<source[^>]*>([\s\S]*?)<\/source>/.exec(body)?.[1] ?? '';
    const where = /category="location">([^<]*)</.exec(body)?.[1] ?? id;

    missing.push({ source: source.replace(/<[^>]+>/g, '…').trim(), where });
  }

  if (missing.length === 0) {
    console.log(`check-catalogues: ${locale} — all translated`);
    continue;
  }

  failed = true;
  console.error(`check-catalogues: ${locale} — ${missing.length} untranslated`);

  for (const { source, where } of missing.slice(0, 15)) {
    console.error(`    ${JSON.stringify(source.slice(0, 70))}  ${where}`);
  }

  if (missing.length > 15) {
    console.error(`    …and ${missing.length - 15} more`);
  }
}

if (failed) {
  console.error(
    '\n  Translate the messages above in src/locale/messages.<locale>.xlf.\n' +
      '  Run `pnpm i18n:extract` first if you have changed any source strings.'
  );
  process.exit(1);
}

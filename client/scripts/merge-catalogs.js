/**
 * Merges src/locale/messages.xlf (the freshly extracted source catalog) into each
 * per-locale catalog, preserving existing <target> translations.
 *
 * `ng extract-i18n` only ever writes the source catalog — it does not merge into
 * the locale files, so running it alone would leave the locale catalogs stale and
 * hand-editing them after every extraction is not viable. Run this immediately
 * after extraction:
 *
 *   pnpm i18n:extract && node scripts/merge-catalogs.js
 *
 * Units are matched by id. Existing targets are carried over, units new to the
 * source are emitted untranslated (and reported), and units no longer in the
 * source are dropped (and reported).
 */
import { existsSync, readFileSync, writeFileSync } from 'node:fs';

const localeDir = 'src/locale';
const sourcePath = `${localeDir}/messages.xlf`;
const locales = ['fr', 'de', 'es'];

const unitPattern = /<unit id="([^"]+)">([\s\S]*?)<\/unit>/g;
const targetPattern = /<target[^>]*>([\s\S]*?)<\/target>/;

const readUnits = (xml) => {
  const units = new Map();

  for (const [, id, body] of xml.matchAll(unitPattern)) {
    units.set(id, body);
  }

  return units;
};

if (!existsSync(sourcePath)) {
  console.error(`merge-catalogs: source catalog not found: ${sourcePath}`);
  process.exit(1);
}

const source = readFileSync(sourcePath, 'utf8');
const sourceUnits = readUnits(source);

if (sourceUnits.size === 0) {
  console.warn(`merge-catalogs: ${sourcePath} contains no units — nothing to merge`);
}

for (const locale of locales) {
  const localePath = `${localeDir}/messages.${locale}.xlf`;
  const existingUnits = existsSync(localePath)
    ? readUnits(readFileSync(localePath, 'utf8'))
    : new Map();

  let carried = 0;
  let added = 0;

  // Rebuild from the source catalog so notes and source text always match the
  // current code, then graft the existing translation back in.
  let merged = source.replace(unitPattern, (unit, id, body) => {
    const existing = existingUnits.get(id);
    const target = existing?.match(targetPattern)?.[1];

    if (target == null) {
      added += 1;

      return unit;
    }

    carried += 1;

    return unit
      .replace('<segment>', '<segment state="translated">')
      .replace(
        /(<source>[\s\S]*?<\/source>)/,
        `$1\n        <target>${target}</target>`
      );
  });

  merged = merged.replace('srcLang="en-GB"', `srcLang="en-GB" trgLang="${locale}"`);

  writeFileSync(localePath, merged);

  const orphaned = [...existingUnits.keys()].filter((id) => !sourceUnits.has(id));

  console.log(
    `merge-catalogs: ${locale} — ${carried} kept, ${added} untranslated, ${orphaned.length} removed`
  );
}

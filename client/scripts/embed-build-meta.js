import { existsSync, readFileSync, readdirSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';

const browserDir = 'dist/netptune/browser';

if (!existsSync(browserDir)) {
  console.error(`embed-build-meta: directory not found: ${browserDir}`);
  process.exit(1);
}

// A localized build emits one directory per locale (en-GB, fr, de, es), each with
// its own index.html; a non-localized build (development) emits a flat index.html
// at the root. Selecting directories by "contains an index.html" covers both and
// naturally skips media/ and any future non-locale output directory.
const localeIndexPaths = readdirSync(browserDir, { withFileTypes: true })
  .filter((entry) => entry.isDirectory())
  .map((entry) => join(browserDir, entry.name, 'index.html'))
  .filter((path) => existsSync(path));

const rootIndexPath = join(browserDir, 'index.html');

const indexPaths = existsSync(rootIndexPath)
  ? [rootIndexPath, ...localeIndexPaths]
  : localeIndexPaths;

if (indexPaths.length === 0) {
  console.error(`embed-build-meta: no index.html found under ${browserDir}`);
  process.exit(1);
}

const { COMMIT, GITHUB_REF, BUILD_NUMBER, RUN_ID } = process.env;

const meta = [
  `    <meta name="build:commit" content="${COMMIT ?? ''}" />`,
  `    <meta name="build:ref" content="${GITHUB_REF ?? ''}" />`,
  `    <meta name="build:number" content="${BUILD_NUMBER ?? ''}" />`,
  `    <meta name="build:run-id" content="${RUN_ID ?? ''}" />`,
].join('\n');

for (const indexPath of indexPaths) {
  const source = readFileSync(indexPath, 'utf8');

  if (!source.includes('</head>')) {
    console.error(`embed-build-meta: </head> tag not found in ${indexPath}`);
    process.exit(1);
  }

  writeFileSync(indexPath, source.replace('</head>', `${meta}\n  </head>`));

  console.log(`embed-build-meta: build metadata written to ${indexPath}`);
}

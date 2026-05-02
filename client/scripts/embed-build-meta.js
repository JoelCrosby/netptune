import { readFileSync, writeFileSync, existsSync } from 'fs';

const indexPath = 'dist/netptune/browser/index.html';

if (!existsSync(indexPath)) {
  console.error(`embed-build-meta: file not found: ${indexPath}`);
  process.exit(1);
}

const { COMMIT, GITHUB_REF, BUILD_NUMBER, RUN_ID } = process.env;

const meta = [
  `    <meta name="build:commit" content="${COMMIT ?? ''}" />`,
  `    <meta name="build:ref" content="${GITHUB_REF ?? ''}" />`,
  `    <meta name="build:number" content="${BUILD_NUMBER ?? ''}" />`,
  `    <meta name="build:run-id" content="${RUN_ID ?? ''}" />`,
].join('\n');

const source = readFileSync(indexPath, 'utf8');

if (!source.includes('</head>')) {
  console.error(`embed-build-meta: </head> tag not found in ${indexPath}`);
  process.exit(1);
}

const html = source.replace('</head>', `${meta}\n  </head>`);

writeFileSync(indexPath, html);

console.log(`embed-build-meta: build metadata written to ${indexPath}`);

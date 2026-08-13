import { defineConfig } from 'vite';
import { nitro } from "nitro/vite";
import { solidStart } from '@solidjs/start/config';
import tailwindcss from '@tailwindcss/vite';

export default defineConfig({
  plugins: [
    solidStart(),
    tailwindcss(),
    nitro()
  ],
  optimizeDeps: {
    // solid-markdown and @solidjs/start are excluded from pre-bundling, so they are
    // served raw and their CJS transitive deps come through untransformed and blow up
    // with "does not provide an export named ...". Force those deps through the
    // optimizer. The long paths are needed because pnpm keeps them out of the root
    // node_modules, so a bare specifier will not resolve.
    include: [
      // micromark's `development` export condition imports `debug`
      'remark-gfm > mdast-util-gfm > mdast-util-from-markdown > micromark > debug',
      'solid-markdown > unified > extend',
      // @solidjs/start's dev toolbar error viewer
      '@solidjs/start > source-map-js',
      '@solidjs/start > error-stack-parser',
    ],
  },
});

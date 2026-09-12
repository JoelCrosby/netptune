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
  build: {
    rollupOptions: {
      output: {
        advancedChunks: {
          // Keep @solidjs/router in its own chunk. Otherwise it is inlined into the
          // SSR entry (app.tsx imports Router), so the MarkdownDoc chunk has to import
          // useLocation back out of the entry while the entry imports the rolldown
          // __exportAll helper out of MarkdownDoc. That cycle is fine in real ESM, but
          // nitro flattens both chunks into a single ssr.mjs and emits the entry first,
          // so __exportAll is still undefined when it is called and every request 500s.
          groups: [
            { name: 'solid-router', test: /node_modules\/@solidjs\/router/ },
          ],
        },
      },
    },
  },
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

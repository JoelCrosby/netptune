// @refresh reload
import { createHandler, StartServer } from '@solidjs/start/server';

export default createHandler(() => (
  <StartServer
    document={({ assets, children, scripts }) => (
      <html lang="en">
        <head>
          <meta charset="utf-8" />
          <meta name="viewport" content="width=device-width, initial-scale=1" />
          <meta
            name="description"
            content="Netptune brings your boards, sprints, calendar, roadmap and pages together, with an AI assistant that knows your work and asks before it changes anything."
          />
          <meta property="og:type" content="website" />
          <meta property="og:site_name" content="Netptune" />
          <meta property="og:title" content="Netptune — Plan, write and ship in one place" />
          <meta
            property="og:description"
            content="Boards, sprints, pages and automations in one friendly workspace, plus an assistant that does the legwork and leaves the final say to you."
          />
          <meta property="og:url" content="https://netptune.co.uk" />
          <meta property="og:image" content="https://netptune.co.uk/screenshot-light.png" />
          <meta name="twitter:card" content="summary_large_image" />
          <link rel="icon" href="/favicon.svg" type="image/svg+xml" />

          {/* eslint-disable-next-line solid/no-innerhtml -- inline theme-init script, runs before first paint */}
          <script innerHTML="try{var s=localStorage.getItem('theme');var d=s?s==='dark':matchMedia('(prefers-color-scheme: dark)').matches;document.documentElement.classList.toggle('dark',d)}catch(e){}" />

          <link rel="preconnect" href="https://fonts.googleapis.com" />
          <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin="anonymous" />
          <link
            href="https://fonts.googleapis.com/css2?family=Inter:ital,opsz,wght@0,14..32,100..900;1,14..32,100..900&family=Overpass:ital,wght@0,100..900;1,100..900&display=swap"
            rel="stylesheet"
          />

          <link
            rel="preload"
            fetchpriority="high"
            as="image"
            href="/screenshot-light.webp"
            type="image/webp"
          />

          <link
            rel="preload"
            fetchpriority="high"
            as="image"
            href="/screenshot-dark.webp"
            type="image/webp"
          />

          <script
            defer
            src="https://static.cloudflareinsights.com/beacon.min.js"
            data-cf-beacon='{"token": "e6aa958973eb4605b393ae59a5e7752a"}'
          />

          <title>
            Netptune | Plan, write and ship in one place
          </title>
          {assets}
        </head>
        <body>
          <div id="app">{children}</div>
          {scripts}
        </body>
      </html>
    )}
  />
));

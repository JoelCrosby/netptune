// @refresh reload
import { createHandler, StartServer } from '@solidjs/start/server';

export default createHandler(() => (
  <StartServer
    document={({ assets, children, scripts }) => (
      <html lang="en">
        <head>
          <meta charset="utf-8" />
          <meta name="viewport" content="width=device-width, initial-scale=1" />
          <link rel="icon" href="/favicon.ico" />

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

          <title>
            Netptune | Open source project management for teams who need real control over their
            tools and data.
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

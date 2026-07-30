# i18n conventions

How to mark strings for translation in this app. The goal across ~330 components is
**consistency over per-case optimality** — a reviewer should be able to check a
component against this list mechanically.

Source locale is `en-GB`; targets are `fr`, `de`, `es`. Translations are inlined at
build time, so changing locale is a full page load, not a runtime toggle.

## Workflow

```bash
pnpm i18n:extract
```

One command does everything: it rewrites `src/locale/messages.xlf` from the code and
merges the result into `messages.{fr,de,es}.xlf`, preserving existing `<target>`s.
This uses the `ng-extract-i18n-merge` builder rather than Angular's own
`extract-i18n`, because the stock builder writes *only* the source catalogue —
running it alone leaves the locale catalogues stale, and hand-editing four XLIFF
files after every extraction is not viable.

Two of its defaults are load-bearing, both configured in `angular.json`:

- **`fuzzyMatch`** re-matches a unit by its source text when its id no longer
  resolves. Message ids are hashes of the message text *including* surrounding
  whitespace, so reformatting alone can change an id; this is what stops that from
  orphaning the translation.
- **`collapseWhitespace`** normalises whitespace in sources and targets. Note
  `trim` is deliberately left `false` — trimming would strip the leading space from
  `" · edited"` and break that separator's rendering.

Untranslated messages fall back to the English source, so an incomplete rollout
still ships.

**Do not measure progress with `grep -c 'No translation found'`.** That warning only
fires for a message id absent from the catalogue entirely. A unit written with a
`<source>` but no `<target>` is *not* reported — the build says zero missing while
still shipping English. Verified: 598 untranslated units produced 0 warnings.

The reliable count is untranslated segments in the catalogues:

```bash
grep -c '<segment>' src/locale/messages.fr.xlf   # untranslated
grep -c '<segment state="translated">' src/locale/messages.fr.xlf
```

## Seeing another language locally

The Angular CLI has no multi-locale serve or preview command, so there is no native
way to exercise the language switcher locally.

`ng serve` is limited to a single locale and forces flat output, so `/fr/` there
returns the *English* bundle with `<base href="/">` — and because `:workspace` is a
top-level route, the router reads `fr` as a workspace slug and drops you into a
workspace that does not exist. Do not use it to check translations at a prefixed URL.

```bash
pnpm start:fr     # French dev server: HMR, one locale, no switcher
```

To exercise the switcher — and `nginx.conf` with it — build the container in dev mode
and point it at a running Aspire stack:

```bash
pnpm docker:dev        # build + run; app on http://localhost:8080
```

That builds with the `docker-dev` Angular configuration: the same multi-locale,
production-shaped output, but unoptimised and with source maps. It swaps in
`environment.container.ts`, whose `apiEndpoint` is `/` so requests go through the
nginx `/api/` proxy rather than straight at the API — which is the point, since it
exercises the proxy too. It keeps the always-passes Turnstile test key, because the
production sitekey only validates on the real domain.

Use `pnpm docker:dev:build` and `pnpm docker:dev:run` separately to re-run without
rebuilding.

### Why host networking and NGINX_PORT

Aspire binds the API to `127.0.0.1` only, so a bridged container cannot reach it —
`host.containers.internal` / `host.docker.internal` resolve to the pasta gateway and
the connection is refused. Host networking fixes that, but under rootless Podman
nginx cannot bind port 80, so `NGINX_PORT=8080` moves it to an unprivileged port.

`NGINX_PORT` defaults to `80` via `ENV` in the Dockerfile, so the production image,
Helm chart and container port are all unchanged.

## Lint enforcement

`@angular-eslint/template/i18n` **does** reach inline templates — the
`processInlineTemplates` processor in `eslint.config.mjs` surfaces them to template
rules as virtual `.html` files, with line numbers pointing into the `.ts` file. It is
configured as an **error**, scoped by directory to areas that are already done.

When you finish an area, add its glob to the i18n block in `eslint.config.mjs`. That
keeps `pnpm lint` green and makes a regression in a finished area fail the build.

### Lint-clean does NOT mean the area is done

The rule only sees **text nodes and static attributes in templates**. It cannot see
`$localize`-able strings in TypeScript, nor English inside template *expressions*
(`{{ a ? 'Yes' : 'No' }}`, `'Untitled ' + kind`). Several areas passed lint while
still shipping English from label tables, `ConfirmDialogOptions`, validation
messages and row/overflow actions.

So before declaring an area finished, sweep its TypeScript too:

```bash
AREA=src/app/features/boards
grep -rnE "(label|title|message|actionTitle|acceptLabel|cancelLabel|placeholder|emptyMessage|description|hint):\s*'[^']+'|\?\s*'[^']+'\s*:\s*'[^']+'|snackbar\.(open|error|success|warn|info)\('" \
  --include='*.ts' "$AREA" | grep -v '\$localize'
```

Then confirm nothing English survives in a translated bundle:

```bash
grep -rl "Some English String" dist/netptune/browser/fr --include='*.js'
```

Expect legitimate hits from NgRx action types (`'[Boards] Create Board'`) and other
do-not-mark strings — those are compiled in identically for every locale. Anything
else is a miss. Note that **non-ASCII translations are escaped in minified output**,
so grep for an ASCII-only substring of the translation.

## Message IDs

**Use generated IDs. Do not author `@@custom-id` as a default.** There were no
catalogs before this work, so custom IDs have nothing to stabilise, and the naming
discipline will not survive a multi-pass rollout. Automatic dedup is a feature here:
the many identical `Cancel` / `Save` / `Delete` labels collapse to one catalog entry
each. And when English copy is reworded, a generated ID *should* invalidate the stale
translation rather than keep serving it.

### The ID is `text` + `meaning` only — NOT description

This is verified behaviour, and it is the most common way to get dedup wrong:

```ts
// These three ALL collapse into ONE catalog unit with ONE shared translation.
// The differing descriptions do not separate them.
$localize`:Page title for the signed-in user profile:Profile`
$localize`:Profile menu item that opens the user's profile:Profile`
$localize`:Heading shown when the account has no display name:Profile`
```

Consequences:

- A `description` is **translator documentation only**. Writing one does not protect
  against an unwanted merge.
- When units merge, only the **first** description survives, while every location is
  listed. The translator sees one description covering several call sites.
- To force a split you **must** add a `meaning` (left of the `|`).

```ts
// Now two units, translatable independently.
$localize`:Profile menu item that opens the user's profile:Profile`
$localize`:profile menu heading|Heading shown when the account has no display name:Profile`
```

Reserve explicit `@@ids` for long-form copy you expect to reword for tone without
changing meaning (e.g. `turnstile.component.ts`'s error paragraphs). Fewer than ~20
app-wide. Scheme: `@@<area>.<component>.<slug>`.

## meaning | description

Syntax: `i18n="meaning|description@@id"`, every part optional.

- **description** — add whenever the string is under ~3 words or its context is not
  obvious from the text. Translators see a flat list; `Open` with no description is a
  coin flip.
- **meaning** — add when the same English text needs different translations in
  different places (homonyms, or a noun used as a label vs. as a stand-in for data).
  This is the *only* thing that splits an ID.

## Templates

### Where `i18n` goes

On the **innermost element containing the complete sentence and nothing else**.
Nearly every button here is `icon + text`, so the most common edit is wrapping the
text in a `<span>` so the `<svg>` does not become a translation placeholder:

```html
<button app-menu-item type="button" (click)="logOut(profileMenu)">
  <svg lucideLogOut class="h-4 w-4 shrink-0"></svg>
  <span i18n="Profile menu item that signs the user out">Logout</span>
</button>
```

Use `<ng-container i18n>` where an extra `<span>` would change layout (flex/grid
children, `gap`, table cells).

### Whitespace, formatting, and message ids

Write the markup the readable way. Prettier's default `htmlWhitespaceSensitivity`
(`css`) leaves the natural form alone for block elements and `<ng-container>`:

```html
<td appTableEmptyCell colspan="2" i18n="Empty state for the tag list">
  No tags yet. Create one to group tasks across projects.
</td>
```

That message extracts with surrounding whitespace (`" No tags yet. … "`), which used
to matter because a message id is a hash of the message text *including* that
whitespace — so reformatting could change an id and orphan its translation without
anyone touching the words. **The merge builder handles this**: `fuzzyMatch` (on by
default) re-matches units by source text when the id no longer resolves, and
`collapseWhitespace` normalises the rest. You do not have to hand-hug tags to
protect ids.

Prettier will still hug inline elements itself when it has to break one:

```html
<span i18n="…"
  >No tags yet. Create one to group tasks across projects.</span
>
```

Let it. That is Prettier guaranteeing the reformat does not change what renders, and
it is why `<ng-container>` is usually the nicer wrapper when you need one — it is not
an inline element, so it never gets hugged.

**Do not set `htmlWhitespaceSensitivity: "ignore"`.** It was tried and reverted. It
tells Prettier that whitespace between inline elements is insignificant, which is
false: it inserted a rendered space before punctuation in five places
(`<strong>Public</strong> . This means …`) and would do so again on any long sentence
containing inline markup. It also made 380 messages padded and split 69 into
whitespace-only duplicate units (they share a translation via `fuzzyMatch`, but
still cost catalogue noise). Note that it is a one-way door — once it rewrites
the whitespace, switching back to `css` preserves the new whitespace rather than
undoing it.

When a sentence ends with an inline element, keep the punctuation against the closing
tag (`<strong>Public</strong>.`) so a line break can never separate them.

- **Never split one sentence across two `i18n` blocks** — word order differs by
  locale. If it spans inline markup, mark the parent and let the markup become a
  placeholder.
- **Never put `@if` / `@for` / `@switch` inside an `i18n` block.** It produces opaque
  nested-template placeholders. Mark the branches individually.

### Attributes

Mark static attributes with `i18n-<attr>`: `label`, `placeholder`, `title`, `alt`,
`aria-label`, plus this repo's component inputs (`heading`, `actionTitle`,
`description`, `emptyMessage`, `errorMessage`, `hint`, `itemLabel`, `acceptLabel`,
`cancelLabel`, `appTooltip`, `subheading`). Static component inputs work — the
runtime applies translated values to inputs, not just DOM attributes.

Three hard limits:

1. **You cannot mark a binding.** `[attr.aria-label]="expr"` and `[label]="expr"`
   have no `i18n-` form. Localize at the TS source instead.
2. **ICU is forbidden in attributes** — it throws at runtime. Build such labels in TS
   with a ternary over two messages.
3. **You cannot mark any attribute whose name starts with `on`.** The compiler
   rejects it with `NG5002: Translating attribute '…' is disallowed for security
   reasons`, because `name.toLowerCase().startsWith('on')` classifies it as an event
   property. This catches innocent input names — `onlineLabel` is blocked purely by
   its prefix. Localize in the component and pass it as a binding:

   ```html
   <!-- no: NG5002 at build time -->
   <app-avatar-filter i18n-onlineLabel onlineLabel="is viewing this board" />

   <!-- yes -->
   <app-avatar-filter [onlineLabel]="viewingLabel" />
   ```

### ICU plural / select

Every `count === 1 ? 'task' : 'tasks'` in a template becomes ICU. Put the *whole*
clause inside, including the verb (German puts it last), and handle `=0`:

```html
<ng-container i18n="Button that adds the selected tasks to the sprint">
  {selected().length, plural,
    =0 {Add tasks} =1 {Add 1 task} other {Add {{ selected().length }} tasks}}
</ng-container>
```

## TypeScript

Form: ``$localize`:meaning|description@@id:Text with ${value}:NAME: inside` ``.

`$localize` is a global — no import. It is typed via the `@angular/localize` entry in
both `tsconfig.json` and `tsconfig.app.json`.

Use it for anything a template cannot mark: bound attributes, snackbar and dialog
copy, validation messages, route `data.title` / `data.back`, label tables, and chart
options.

```ts
readonly profileMenuLabel = computed(() => {
  const user = this.user();
  const name =
    user?.displayName ||
    user?.email ||
    $localize`:Stands in for the user's name when the account has neither a display name nor an e-mail address:user`;

  // aria-label is a binding, so it cannot be marked with i18n- in the template.
  return $localize`:Accessible label for the button that opens the profile menu. USER_NAME is the display name, e-mail address, or a generic fallback:Open ${name}:USER_NAME: menu`;
});
```

**ICU does not work in `$localize`.** The runtime only concatenates message parts; it
never parses ICU, so `{n, plural, ...}` renders literally. Use a ternary over two
messages, hoist the condition into a named constant (repo convention), and leave a
note:

```ts
const isSingle = count === 1;

// fr/de/es share English's one/other plural split. A locale with more plural
// categories (ru, pl, ar) means moving this string into a template ICU.
return isSingle
  ? $localize`:Confirmation title, deleting one notification:Delete notification?`
  : $localize`:Confirmation title, deleting several notifications:Delete ${count}:COUNT: notifications?`;
```

## Placeholders

Unnamed interpolations extract as `INTERPOLATION_1`, which tells a translator
nothing. Name them.

- Templates: the comment goes **inside** the interpolation —
  `{{ sprint.taskCount // i18n(ph="TOTAL_COUNT") }}`
- TypeScript: the `:NAME:` suffix after the expression.

`SCREAMING_SNAKE_CASE`, describing the **role** (`USER_NAME`, `TASK_COUNT`), never the
variable name. Name every placeholder in any message with two or more.

## Do NOT mark

- NgRx action type strings (`'[Sprints] Create Sprint'`) and any reducer/effect id.
- Enum keys and API contract values: `netptunePermissions.*`, `'dark'` / `'light'`,
  status and kind discriminants, response field names.
- Entity and user data: `user.displayName`, `workspace.name`, `sprint.name`,
  `task.name`, `tag.name`.
- Route path segments, query param names, slugs.
- CSS classes, Tailwind strings, `id`, `autocomplete`, and ARIA *values* from a fixed
  vocabulary (`aria-haspopup="menu"`).
- `console.*` / logger output and dev-only strings.
- `KeyboardEvent.key` comparisons — `'Enter'` / `'Escape'` are key names, not UI text.
  A *rendered* shortcut hint is a separate, markable string.
- Third-party config keys (ApexCharts options, EditorJS tools, Turnstile params) and
  date format patterns.

The lint rule's `ignoreAttributes` list in `eslint.config.mjs` encodes the attribute
half of this. Only add an attribute there when its value is a fixed vocabulary, a CSS
class, an SVG primitive, or an element reference — when in doubt leave it out, so the
rule asks rather than silently skipping real copy.

## `Create <Entity>`

Repo convention is that creation buttons read `Create <Entity>`. Keep the whole label
as **one message per entity** — never `Create {{ entity }}`. French needs an article
and puts the noun after ("Créer un tableau"); German puts the verb last ("Neues Board
erstellen"). There are only ~10 entity types, so discrete messages are cheap and
correct.

Where a shared component takes a dynamic label, pass an **already-localized** string
from the call site rather than composing inside the component.

## Reviewer checklist

1. Every user-visible text node sits in exactly one `i18n` block.
2. No split sentences; no control flow inside a block.
3. Every static translatable attribute has `i18n-<attr>`; no ICU in attributes.
4. Every `[binding]`-supplied label is localized at its TS source.
5. Template `count === 1` is ICU; TS is a ternary with the plural note.
6. Placeholders named; sub-3-word strings have descriptions; homonyms have meanings.
7. Nothing from the do-not-mark list was marked.
8. `pnpm i18n:extract` runs clean, and the new
   entries read sensibly out of context.
9. `npx eslint <area>` is clean if the area is in the enforced glob.

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Entities.Contexts;

namespace Netptune.SeedData.Seeders;

public sealed class TaskSeeder : ISeeder
{
    public int Phase => 1;

    private static readonly Dictionary<string, (string Name, string Description)[]> ProjectTasks = new()
    {
        ["PAT"] =
        [
            (
                "Implement API key rotation with configurable expiry",
                "Customers need programmatic access without storing passwords. Design a key management API with creation, revocation, and per-key scope restrictions. Keys should expire after a configurable TTL defaulting to 90 days."
            ),
            (
                "Add idempotency keys to payment endpoints",
                "Clients retrying after a network failure can double-charge. Accept an `Idempotency-Key` header and store request results keyed by it for 24 hours so duplicate POSTs return the original response."
            ),
            (
                "Migrate authentication to use refresh token rotation",
                "Current implementation stores long-lived tokens in localStorage. Switch to short-lived access tokens and httpOnly refresh tokens with single-use rotation to meet the new security policy."
            ),
            (
                "Paginate all unbounded list endpoints",
                "Several list endpoints return every row without a page limit. Add cursor-based pagination with a configurable page size capped at 200. Update API docs and clients accordingly."
            ),
            (
                "Add OpenTelemetry tracing to service boundaries",
                "Instrument all controller actions and repository calls with activity spans. Export traces to the OpenTelemetry collector endpoint. Include baggage propagation for user and workspace context."
            ),
            (
                "Replace in-memory session store with Redis",
                "The current session store is lost on pod restart, causing logged-out users after every deploy. Migrate to the Redis-backed provider already in use for caching."
            ),
            (
                "Write integration tests for the billing webhook handler",
                "The Stripe webhook handler has no automated coverage and has caused two incidents this quarter. Add integration tests for the payment_succeeded, payment_failed, and subscription_deleted event types."
            ),
            (
                "Document all public API endpoints in OpenAPI",
                "Onboarding new integration partners takes weeks because the API is undocumented. Add XML doc comments to all controllers and configure Swashbuckle to produce a versioned OpenAPI spec."
            ),
        ],
        ["MOB"] =
        [
            (
                "Fix crash on background app refresh for iOS 17.2",
                "Users on iOS 17.2 report a crash when the app resumes from the background and triggers a fetch. The crash log points to a race condition in the state restoration handler. Affects approximately 12% of the active iOS user base."
            ),
            (
                "Implement biometric authentication fallback",
                "Add Face ID and Touch ID support using the LocalAuthentication framework. When biometrics fail three times, fall back to the standard PIN entry flow without logging the user out."
            ),
            (
                "Add offline mode with local SQLite cache",
                "Users in areas with intermittent connectivity lose work when the app goes offline. Cache the last successful API response for tasks and boards locally, and queue mutations for replay when connectivity returns."
            ),
            (
                "Reduce cold start time by lazy-loading non-critical modules",
                "The app takes 3.2 seconds to reach interactive on a mid-range Android device. Defer loading the analytics SDK, the chat module, and the file picker until first use."
            ),
            (
                "Fix broken deep links on Android 14",
                "The App Links handler stopped working after upgrading the target SDK to 34. Android 14 changed the verification flow; the digital asset links file needs updating and the intent filter requires the `autoVerify` flag."
            ),
            (
                "Implement push notification permission prompt flow",
                "We are not requesting push permission at the right point in the user journey and our opt-in rate is below 30%. Move the permission prompt to after the user completes their first task, and add an in-app pre-prompt explaining the value."
            ),
            (
                "Add dark mode support to the onboarding screens",
                "The onboarding flow hardcodes light mode colours and looks broken on devices with system dark mode enabled. Update all onboarding views to respect the system appearance setting."
            ),
            (
                "Resolve memory leak in the image carousel component",
                "A memory leak in the image carousel causes the app to be killed after browsing large project attachments. The profiler shows view references are retained after the carousel is dismissed."
            ),
        ],
        ["DSH"] =
        [
            (
                "Add CSV and Excel export to all report views",
                "Finance and operations teams regularly copy data out of the UI manually. Add an export button to each report that generates a CSV or xlsx file server-side and streams it to the client."
            ),
            (
                "Fix stacked bar chart overflow on small viewports",
                "The stacked bar chart on the project overview page overflows its container on viewports below 768px. The tooltip also clips out of the visible area. Fix with responsive scaling and tooltip boundary detection."
            ),
            (
                "Implement real-time metric updates via WebSocket",
                "Dashboard values go stale within minutes and require a manual refresh. Add a WebSocket subscription that pushes incremental updates to the active panels without a full page reload."
            ),
            (
                "Add date range picker to the revenue overview panel",
                "The revenue panel only shows the last 30 days and has no way to change the window. Add a date range picker with presets (7d, 30d, 90d, custom) and persist the selection per user."
            ),
            (
                "Cache slow aggregation queries with a 5-minute TTL",
                "Three aggregation queries on the analytics page consistently take over 2 seconds. Add a Redis-backed query cache with a 5-minute TTL and a manual invalidation hook for when underlying data changes."
            ),
            (
                "Build cohort retention chart for the user analytics page",
                "Product needs to track weekly retention by signup cohort to measure the impact of onboarding changes. Build a heat map chart with cohort rows and week columns populated from the events table."
            ),
            (
                "Fix incorrect percentage calculations in the funnel report",
                "The conversion percentages in the funnel are calculated against total users instead of the previous step. Fix the denominator in the calculation and add a tooltip explaining how the percentage is computed."
            ),
            (
                "Add row-level drill-down to the transactions table",
                "Account managers want to inspect the line items behind each transaction without leaving the dashboard. Add an expandable row that loads the sub-items on demand from a new API endpoint."
            ),
        ],
        ["MKT"] =
        [
            (
                "Implement A/B testing framework for landing page hero copy",
                "We want to test two versions of the hero headline and CTA button copy. Integrate with our existing analytics pipeline to track impressions and conversions per variant. Use edge middleware to split traffic consistently per visitor."
            ),
            (
                "Fix broken canonical link in blog post template",
                "Blog posts are generating duplicate content warnings in Google Search Console because the canonical link tag is missing from the post template. Add the correct canonical URL to the head of each post."
            ),
            (
                "Add structured data markup to pricing and FAQ pages",
                "The pricing and FAQ pages are missing JSON-LD structured data, reducing their eligibility for rich results in search. Add Product and FAQ schema to the respective page templates."
            ),
            (
                "Improve Lighthouse performance score above 90 on mobile",
                "The mobile performance score sits at 62 due to render-blocking resources and unoptimised images. Address the largest contentful paint by deferring non-critical scripts and converting hero images to WebP with explicit size attributes."
            ),
            (
                "Integrate HubSpot form on the contact page",
                "The current contact form sends emails directly and has no lead tracking. Replace it with the HubSpot form embed and configure the portal to route submissions to the sales team pipeline."
            ),
            (
                "Build an auto-generated sitemap for all blog content",
                "The sitemap is a manually maintained XML file that has not been updated in six months. Generate it automatically at build time from the CMS content API and submit the URL to Google Search Console."
            ),
            (
                "Add cookie consent banner compliant with GDPR and CCPA",
                "Legal has flagged that the site loads analytics scripts before obtaining consent. Implement a consent banner that blocks third-party scripts until the user accepts, and store their preference in a first-party cookie."
            ),
            (
                "Fix broken navigation links introduced in the last deploy",
                "Several top-level navigation links 404 after the last production deploy restructured the URL scheme. Audit all internal links against the new routes and add 301 redirects for the old paths."
            ),
        ],
        ["CLI"] =
        [
            (
                "Add shell completion scripts for bash, zsh, and fish",
                "Users have to type full command and flag names manually, which slows adoption. Generate tab-completion scripts from the command registry and install them automatically via `tool install --completions`."
            ),
            (
                "Improve error messages for invalid flag combinations",
                "The CLI currently exits with a generic usage error when flags conflict. Detect specific invalid combinations and print a focused message explaining which flags conflict and why."
            ),
            (
                "Implement a --dry-run flag for all destructive commands",
                "Users are nervous about running `delete` and `reset` commands in production environments. Add a `--dry-run` flag that prints the operations that would be performed without executing them."
            ),
            (
                "Add progress indicators to long-running operations",
                "Commands like `generate` and `sync` can run for 30+ seconds with no output, leading users to assume they have hung. Add an animated spinner with an elapsed time counter for operations over 2 seconds."
            ),
            (
                "Support reading config from environment variables",
                "CI pipelines cannot use config files interactively. Add support for `TOOL_API_KEY`, `TOOL_WORKSPACE`, and `TOOL_ENDPOINT` environment variables that override config file values."
            ),
            (
                "Write end-to-end tests for the init and generate commands",
                "The core commands have no automated test coverage and have regressed twice in recent releases. Add tests that spin up a temporary directory, run the command, and assert on the generated file structure."
            ),
            (
                "Publish the CLI to Homebrew and Winget",
                "Users on macOS and Windows have no standard install path beyond downloading the binary manually. Create a Homebrew formula and a Winget manifest and automate publishing them as part of the release workflow."
            ),
            (
                "Fix output truncation on narrow terminal widths",
                "Long project and task names are truncated mid-character on terminal widths below 80 columns, producing garbled output. Detect the terminal width and wrap output to fit, truncating with an ellipsis only when necessary."
            ),
        ],
        ["CMP"] =
        [
            (
                "Extract DatePicker into a standalone publishable package",
                "Several external teams want to use the DatePicker without taking a dependency on the full component library. Extract it into `@company/date-picker` with its own package.json, build config, and changelog."
            ),
            (
                "Fix focus trap regression in the modal component",
                "Version 2.4.0 broke the focus trap for modals opened with a keyboard shortcut. Focus escapes the modal dialog and reaches the background content, violating WCAG 2.1 criterion 2.1.2."
            ),
            (
                "Add keyboard navigation support to the dropdown menu",
                "The dropdown menu does not respond to arrow keys, Home, End, or type-ahead character navigation. Implement the ARIA authoring practices guide pattern for the menu button widget."
            ),
            (
                "Write Storybook stories for all form input variants",
                "New contributors struggle to find available input variants and frequently recreate existing ones. Write stories covering default, disabled, error, and helper-text states for every input component."
            ),
            (
                "Resolve z-index conflict between tooltip and dialog",
                "Tooltips inside dialogs are rendered behind the dialog overlay because both rely on the same stacking context. Implement a portal-based rendering strategy to ensure tooltips always appear above their parent overlay."
            ),
            (
                "Add ARIA live region to the toast notification component",
                "Screen readers do not announce toast notifications because the container lacks a live region role. Add `role=status` to non-critical toasts and `role=alert` to error toasts."
            ),
            (
                "Support custom trigger slots in the popover component",
                "The popover only accepts a Button as its trigger, but teams need to attach it to icon buttons and text links. Add a default slot for the trigger and document the required ARIA relationship attributes."
            ),
            (
                "Upgrade Storybook to v8 and resolve breaking changes",
                "Storybook 7 is approaching end of life and the current version does not support Vite 5 or the new compiler transforms. Upgrade to v8, migrate the main configuration, and fix the stories that fail to render after the upgrade."
            ),
        ],
        ["INF"] =
        [
            (
                "Migrate staging environment to Kubernetes",
                "Staging runs on a single VM with manual deployment scripts that diverge from production. Migrate to the same Kubernetes setup as production, with separate namespaces and a dedicated Argo CD application."
            ),
            (
                "Configure HPA based on request queue depth metrics",
                "The API pods scale on CPU, which lags behind actual load. Add a custom metric from the request queue depth exposed via the Prometheus adapter and configure the HPA to target a queue depth below 50."
            ),
            (
                "Implement blue/green deployment strategy for the API",
                "Rolling updates occasionally cause request errors during the switchover window. Configure a blue/green deployment using the traffic splitting feature in the Nginx ingress controller."
            ),
            (
                "Automate TLS certificate renewal with cert-manager",
                "TLS certificates are renewed manually and have expired twice in the past year. Install cert-manager, configure a ClusterIssuer backed by Let's Encrypt, and annotate all Ingress resources."
            ),
            (
                "Set resource limits and requests on all production pods",
                "Several pods have no resource limits and have caused node memory pressure twice this quarter. Audit all Deployments and StatefulSets, and set CPU requests, CPU limits, and memory limits based on observed P99 usage."
            ),
            (
                "Write Terraform module for the shared RDS instance",
                "The RDS instance was created manually and is not tracked in version control. Import it into Terraform, extract a reusable module, and document the input variables and outputs."
            ),
            (
                "Rotate all long-lived IAM access keys",
                "The security audit identified seven IAM access keys older than 90 days. Rotate each key, update the secrets in AWS Secrets Manager, and configure automated rotation going forward."
            ),
            (
                "Enable pod disruption budgets for zero-downtime rolling updates",
                "Node drain operations during cluster upgrades occasionally take down all replicas of a deployment simultaneously. Add PodDisruptionBudget resources requiring at least one replica available for all critical workloads."
            ),
        ],
        ["MON"] =
        [
            (
                "Add SLO burn rate alert for the API error budget",
                "We have defined a 99.9% availability SLO but have no alerting when the error budget is burning faster than sustainable. Add a multi-window burn rate alert at 2% per hour (page) and 5% per day (ticket)."
            ),
            (
                "Fix missing spans in distributed trace for the checkout flow",
                "The trace for the checkout flow has a gap between the API gateway and the order service, making it impossible to diagnose latency spikes. Verify that the `traceparent` header is propagated through the message broker."
            ),
            (
                "Build a Grafana dashboard for database query latency",
                "There is no visibility into which queries are slow without tailing logs. Build a Grafana dashboard using the pg_stat_statements metrics to show P50, P95, and P99 latency per query fingerprint."
            ),
            (
                "Configure alertmanager to route critical pages to PagerDuty",
                "High-severity alerts currently go to Slack, where they get lost in noise. Configure an alertmanager route that sends `severity=critical` alerts to PagerDuty with a 5-minute repeat interval."
            ),
            (
                "Add exemplars to histogram metrics for trace correlation",
                "Histograms in Prometheus show latency distributions but give no way to jump to a specific trace that contributed to a slow bucket. Enable exemplar support in the Prometheus client and link them to Tempo."
            ),
            (
                "Set up synthetic monitoring for the public status page",
                "The status page reports status based on internal health checks that do not reflect real user experience. Add an external synthetic monitor from three regions that checks the login and API key endpoints every minute."
            ),
            (
                "Reduce log ingestion costs by sampling debug-level traces",
                "Log ingestion costs have grown 40% month-over-month due to verbose debug logging in the hot path. Configure tail-based sampling in the collector to retain 100% of error traces but only 1% of successful debug traces."
            ),
            (
                "Instrument the background job queue with OpenTelemetry",
                "Background jobs have no tracing, making it impossible to correlate a failed job with the API request that triggered it. Propagate the trace context through the job payload and create child spans for each processing step."
            ),
        ],
    };

    private static readonly ProjectTaskStatus[] Statuses = Enum.GetValues<ProjectTaskStatus>();

    public async Task SeedAsync(DataContext dbContext, SeedContext context, CancellationToken ct)
    {
        context.Tasks.AddRange(context.Projects.SelectMany((project, pi) =>
            ProjectTasks[project.Key].Select((task, i) => new ProjectTask
            {
                Name = task.Name,
                Description = task.Description,
                Status = Statuses[(pi * 8 + i) % Statuses.Length],
                Owner = context.Users[(pi + i) % context.Users.Count],
                Project = project,
                ProjectScopeId = i,
                Workspace = project.Workspace,
            })
        ));

        await dbContext.ProjectTasks.AddRangeAsync(context.Tasks, ct);
    }
}

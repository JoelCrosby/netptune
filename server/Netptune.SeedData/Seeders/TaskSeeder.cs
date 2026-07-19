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
            (
                "Add rate limiting per API key with configurable thresholds",
                "API keys shared between many clients can exhaust quotas and affect other users. Add per-key rate limits configurable through the admin panel, enforced at the gateway with a sliding window algorithm backed by Redis."
            ),
            (
                "Implement webhook delivery retry with exponential backoff",
                "Failed webhook deliveries are currently abandoned after the first attempt. Implement a retry queue with exponential backoff up to seven attempts, surfacing the failure in the dashboard after the final attempt."
            ),
            (
                "Add CORS policy configuration per workspace",
                "Workspaces with custom domains need tailored CORS policies, but the current configuration is global. Allow workspace admins to define allowed origins through the API settings, validated against a safelist of registered domains."
            ),
            (
                "Enforce role-based access control on all admin endpoints",
                "Several admin endpoints lack granular permission checks and rely on a single IsAdmin flag. Replace them with policy-based authorization using resource-level claims aligned to the existing role model."
            ),
            (
                "Add request validation using FluentValidation",
                "Command objects are validated inconsistently with a mix of data annotations and manual checks. Standardise on FluentValidation with a pipeline behaviour that returns RFC 9457 problem details on failure."
            ),
            (
                "Implement soft-delete for all entity types",
                "Hard deletes break referential integrity in the audit trail and cause cascading issues in downstream reports. Add an IsDeleted column and a global query filter to all entity types, and expose an undelete endpoint for admins."
            ),
            (
                "Add audit trail logging for destructive operations",
                "Security compliance requires a tamper-evident log of who deleted or modified sensitive records. Write an EF Core interceptor that captures the before-state of any delete or update and persists it to the audit_log table."
            ),
            (
                "Set up health check endpoints for Kubernetes probes",
                "The deployment uses a generic TCP probe that does not verify database or cache connectivity. Add /health/live and /health/ready endpoints using the ASP.NET Core health check middleware, including checks for PostgreSQL and Redis."
            ),
            (
                "Add caching layer to workspace membership queries",
                "Workspace membership is resolved on every authenticated request, causing hundreds of redundant database reads per second. Cache the resolved set per user per workspace with a 30-second TTL, invalidated on membership changes."
            ),
            (
                "Replace synchronous file upload with pre-signed S3 URLs",
                "Large file uploads are proxied through the API server, consuming connection pool resources for the full upload duration. Issue pre-signed S3 PUT URLs directly to clients and confirm completion via a webhook from S3."
            ),
            (
                "Migrate from Newtonsoft.Json to System.Text.Json",
                "The project still depends on Newtonsoft.Json despite the newer built-in serialiser being available. Migrate all serialisation to System.Text.Json, update any custom converters, and verify round-trip fidelity with existing payload snapshots."
            ),
            (
                "Add API versioning using URL segment strategy",
                "Breaking API changes are deployed without a deprecation path, leaving integration partners with broken clients. Introduce URL segment versioning (/v1/, /v2/) and keep the previous version available for a 90-day deprecation window."
            ),
            (
                "Implement circuit breaker for third-party API calls",
                "Calls to the email and payment providers have no failure isolation and can cascade into full request queue saturation. Wrap all outbound HTTP clients with Polly circuit breakers configured to open after five consecutive failures."
            ),
            (
                "Write load tests for the task bulk-update endpoint",
                "The bulk-update endpoint has never been tested under load and is expected to handle up to 500 items per request. Write k6 load tests targeting 50 concurrent users and set a baseline P95 latency target below 500 ms."
            ),
            (
                "Add ETag support to all GET endpoints",
                "Clients poll several endpoints on a short interval and consume unnecessary bandwidth re-downloading unchanged payloads. Add ETag headers based on entity hash and return 304 Not Modified when the ETag matches the If-None-Match header."
            ),
            (
                "Implement event sourcing for the task status change history",
                "Users want to see the full status change history for a task but the current model only stores the current state. Append a TaskStatusChanged event to the event store on every status transition and expose a history endpoint."
            ),
            (
                "Compress API responses with Brotli encoding",
                "The API serves large JSON payloads without compression, inflating bandwidth costs especially for mobile clients. Enable Brotli as the preferred content encoding with gzip as a fallback, and benchmark the throughput impact."
            ),
            (
                "Write contract tests for the mobile client integration",
                "The mobile app and the API are developed independently and have drifted out of sync twice this quarter. Write consumer-driven contract tests using Pact that run in CI and block merges when the provider breaks the contract."
            ),
            (
                "Add distributed lock to prevent double-processing of webhooks",
                "Stripe occasionally delivers the same webhook event twice within a short window, causing duplicate invoice records. Acquire a distributed lock keyed on the event ID before processing and release it after the idempotent write completes."
            ),
            (
                "Add structured error responses conforming to RFC 9457",
                "Error responses are inconsistently shaped across endpoints, making client error handling harder. Standardise all error responses on the RFC 9457 Problem Details format with machine-readable type URIs."
            ),
            (
                "Implement request deduplication for the email notification sender",
                "The email sender is triggered by multiple events and can send the same notification twice when events arrive in quick succession. Add a deduplication window of 60 seconds keyed on recipient and template to suppress duplicate sends."
            ),
            (
                "Add support for batch GET requests to reduce round trips",
                "The dashboard makes 12 individual API calls on load, adding latency on high-latency connections. Add a /batch endpoint that accepts an array of request descriptors and returns responses in a matching array, executed in parallel server-side."
            ),
            (
                "Expose a GraphQL schema for the reporting module",
                "Third-party analytics integrations need flexible query capabilities that the REST API cannot provide without many custom endpoints. Design a GraphQL schema for the reporting module using Hot Chocolate and restrict access to the reporting scope."
            ),
            (
                "Configure response caching headers for stable endpoints",
                "API responses for workspace metadata and user profiles are fetched on every navigation despite changing infrequently. Set appropriate Cache-Control and Vary headers on stable endpoints to allow CDN and browser caching."
            ),
            (
                "Enforce max payload size on file upload endpoints",
                "There is no server-side size limit on file uploads, and large uploads have caused OOM errors on the API pods. Enforce a 50 MB maximum at the ingress layer and return a clear 413 response with the limit stated in the body."
            ),
            (
                "Implement webhook signature verification",
                "Incoming webhooks from third parties are processed without verifying the request signature, creating a spoofing risk. Validate HMAC-SHA256 signatures on all inbound webhook endpoints and reject unsigned requests with a 401."
            ),
            (
                "Add background task processing using Hangfire",
                "Long-running operations like report generation block the request thread and cause timeouts for large datasets. Move these operations to background jobs using Hangfire with a PostgreSQL job store and expose a job status polling endpoint."
            ),
            (
                "Write a Postman collection covering all public API endpoints",
                "Onboarding new developers takes days because there is no runnable API collection to explore the endpoints. Create a Postman collection with environment variables for auth tokens and workspace IDs and publish it to the team workspace."
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
            (
                "Add haptic feedback to primary action buttons",
                "Users report that the app feels unresponsive compared to competitors. Add haptic feedback using the CoreHaptics framework on iOS and the Vibrator API on Android for task completion, swipe actions, and primary form submissions."
            ),
            (
                "Implement pull-to-refresh on the task list screen",
                "The task list shows stale data after returning from the task detail view and requires a full navigation cycle to refresh. Add pull-to-refresh with a network request to reload the current page of tasks."
            ),
            (
                "Fix incorrect badge count after clearing notifications",
                "The notification badge on the app icon does not clear after the user reads all notifications. Update the badge count via UNUserNotificationCenter on iOS and NotificationManagerCompat on Android after the user marks all as read."
            ),
            (
                "Add swipe-to-complete gesture on task cards",
                "Completing a task requires opening the detail view and tapping the status button, adding unnecessary steps to a common action. Add a trailing swipe action on the task list cells that marks the task complete with an undo toast."
            ),
            (
                "Reduce APK size by removing unused resource assets",
                "The Android APK includes asset variants for all screen densities and unused language resources. Enable resource shrinking in the release build, limit ABI splits to arm64-v8a, and remove unused drawable assets."
            ),
            (
                "Fix layout shift on iPad when keyboard appears",
                "The task creation form shifts the entire layout up instead of only scrolling the form content above the keyboard on iPad. Switch to a scroll view containing the form fields and set contentInsetAdjustmentBehavior to scrollableAxes."
            ),
            (
                "Add accessibility labels to all icon-only buttons",
                "VoiceOver and TalkBack cannot describe icon-only buttons in the toolbar and task actions. Add accessibilityLabel and contentDescription to every interactive element that uses an icon without accompanying text."
            ),
            (
                "Implement background data sync using WorkManager on Android",
                "The Android app only syncs when foregrounded, causing large data gaps after the app has been closed for a long period. Schedule a WorkManager periodic work request to run a lightweight sync every 15 minutes with network constraints."
            ),
            (
                "Add skeleton loading screens to replace spinner placeholders",
                "Full-screen spinners block navigation and give no indication of page structure while loading. Replace all full-screen loading states with skeleton screens that mirror the final content layout."
            ),
            (
                "Fix date picker locale mismatch on non-English devices",
                "The date picker displays month names in English regardless of the device locale setting. Use the device locale when formatting month and day labels throughout the date picker component."
            ),
            (
                "Implement in-app update prompt for critical patches",
                "Users on old versions missing security patches have no way of knowing an update is available. Use the In-App Updates API on Android and a custom version check on iOS to prompt users when a critical update is available."
            ),
            (
                "Add widget support for the task quick-add flow on iOS",
                "Competitors offer home screen widgets for task creation, which is a frequently requested feature. Build a WidgetKit widget with a deep link that opens the task creation sheet with the last used project pre-selected."
            ),
            (
                "Fix search results not updating after a filter change",
                "Changing a filter while search results are displayed does not re-run the search, showing results inconsistent with the active filters. Combine the search query and filter state into a single observable and re-trigger the search on any change."
            ),
            (
                "Cache user avatars in the disk image store to reduce bandwidth",
                "User avatars are re-downloaded on every app launch even when they have not changed. Add cache-control respecting disk caching using the platform image library and respect ETag headers for cache invalidation."
            ),
            (
                "Add VoiceOver support to the board column swiper",
                "The board view swiper has no accessible alternative for navigating between columns, making it unusable with VoiceOver. Add accessible pagination controls and expose the column name and task count as accessibility values."
            ),
            (
                "Implement session expiry handling with an auto-logout dialog",
                "When the refresh token expires the app silently fails API calls without informing the user. Intercept 401 responses and display a modal explaining the session has expired before redirecting to the login screen."
            ),
            (
                "Fix duplicate task entries appearing after a sync conflict",
                "When the device comes back online after an offline period, conflict resolution occasionally inserts duplicate tasks. Add a client-side deduplication pass keyed on server-assigned ID after applying the sync response."
            ),
            (
                "Add landscape orientation support to the task detail screen",
                "The task detail screen is locked to portrait and rotates awkwardly on iPads. Enable landscape layout on the task detail screen with a two-column layout that shows the description and properties side by side."
            ),
            (
                "Optimize RecyclerView adapter diffing for large task lists",
                "Scrolling through task lists with more than 200 items causes dropped frames due to full adapter rebinds. Replace notifyDataSetChanged with DiffUtil and ItemAnimator to compute and apply minimal diff updates."
            ),
            (
                "Add share extension for creating tasks from other apps",
                "Users want to save content from Safari or Mail as tasks without switching apps manually. Build a Share Extension that presents a pre-filled task creation sheet with the shared URL or text in the description field."
            ),
            (
                "Fix keyboard toolbar not dismissing on form submission",
                "Submitting the task creation form while the keyboard is visible leaves the keyboard visible on the newly created task detail view. Call resignFirstResponder on the active field before triggering the submission."
            ),
            (
                "Implement crash-free rate alerting via Firebase Performance",
                "The team has no automated alert when the app crash rate rises above baseline after a release. Set up a Firebase Performance custom alert that pages the on-call engineer when the crash-free session rate drops below 99.5%."
            ),
            (
                "Add app shortcuts for quick-creating tasks from the home screen",
                "iOS and Android support shortcuts from the home screen icon, but the app does not expose any. Register a Quick Action and App Shortcut for Create Task, defaulting to the most recently active project."
            ),
            (
                "Fix progress indicator sticking at 100% after upload completes",
                "The file upload progress indicator stays at 100% instead of dismissing after the upload response is received. Dismiss the indicator only after the server confirms the file has been saved, not on URLSession completion."
            ),
            (
                "Implement data export to JSON from the profile settings screen",
                "GDPR requires the ability for users to download all data associated with their account. Add an Export My Data button in the profile settings that requests a data export from the API and opens the share sheet when the file is ready."
            ),
            (
                "Add dynamic font size support for accessibility settings",
                "The app uses fixed font sizes that do not scale with the iOS Dynamic Type or Android font scale settings. Replace all hardcoded font sizes with scaled equivalents using UIFontMetrics on iOS and scaledDensity on Android."
            ),
            (
                "Fix timezone handling for due dates on DST changeover days",
                "Due dates set on the day before a DST transition are displayed one hour off after the clocks change. Store and compare all due dates in UTC and convert to the device timezone only for display."
            ),
            (
                "Write UI automation tests for the login and task creation flows",
                "The two most critical user flows have no automated UI test coverage and have regressed twice. Write XCUITest and Espresso tests covering login, onboarding completion, and creating and completing a task from the board view."
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
            (
                "Implement saved dashboard layouts per user",
                "Users reconfigure the same panels on every visit because there is no persistence for layout preferences. Save the dashboard layout to the user's profile on the server after each drag-and-drop reorder and restore it on the next load."
            ),
            (
                "Add print stylesheet for report pages",
                "Printing or exporting to PDF from the browser produces garbled layouts because the page has no print-specific styles. Add a print stylesheet that hides navigation and sidebars and expands all collapsed sections."
            ),
            (
                "Fix tooltip z-index clipping in the area chart component",
                "Chart tooltips inside the area chart render behind the sticky table header when the user hovers near the top of the chart. Assign the tooltip a higher z-index and use a portal to render it outside the overflow container."
            ),
            (
                "Add chart colour theming to respect system dark mode",
                "Chart colours are hardcoded as hex values and look washed out against the dark background when system dark mode is active. Read colours from CSS custom properties that switch with the colour scheme and update the chart renderer accordingly."
            ),
            (
                "Build a task completion velocity chart for sprint planning",
                "The sprint planning view has no velocity data, forcing teams to estimate capacity from memory. Build a grouped bar chart showing tasks completed per sprint for the last ten sprints broken down by project."
            ),
            (
                "Fix pagination resetting when a filter is changed",
                "Changing a filter on a paginated report sends the user back to page one but keeps the page number in state, causing a skip on the next navigation. Reset the page cursor to null whenever any filter value changes."
            ),
            (
                "Add column visibility toggle to all data tables",
                "Wide tables overflow on smaller monitors and users cannot hide the columns they do not need. Add a column settings popover that persists the visibility state per table per user using localStorage."
            ),
            (
                "Implement shareable dashboard links with filter state in the URL",
                "Users copy dashboard URLs to share with colleagues but the filters are not preserved in the URL, making shared links useless. Serialise all active filters into URL search params and restore them on page load."
            ),
            (
                "Add comparative view showing current versus previous period",
                "The KPI cards show absolute values but give no context about whether performance has improved. Add a delta indicator and a sparkline showing the previous period comparison below each KPI value."
            ),
            (
                "Fix overlapping axis labels on the weekly burn-down chart",
                "The x-axis labels on the burn-down chart overlap when the sprint contains more than two weeks of data. Rotate the labels 45 degrees and reduce the font size on viewports below 1024px."
            ),
            (
                "Implement lazy loading for off-screen chart panels",
                "All chart panels fetch their data on page load simultaneously, creating a surge of concurrent requests that slows the initial load time. Defer data fetching until the panel enters the viewport using IntersectionObserver."
            ),
            (
                "Add keyboard shortcuts for toggling between dashboard tabs",
                "Switching between dashboard tabs requires mouse interaction, which slows power users who prefer keyboard navigation. Add keyboard shortcuts (1-9) to activate the corresponding tab and display the binding in a tooltip on each tab."
            ),
            (
                "Build a geographic heat map for the regional performance view",
                "The regional performance data is shown as a table but product wants a map visualisation to identify geographic patterns. Build an SVG-based world map heat map using the regional breakdown data from the reporting API."
            ),
            (
                "Fix empty state illustration not displaying on first load",
                "When a new workspace has no data, the empty state illustration flashes visible for a moment then disappears as the loading state overwrites it. Unify the loading and empty states into a single state machine to prevent the flash."
            ),
            (
                "Add annotation markers to charts for deployment events",
                "Spikes in error rates and latency are hard to correlate with deployments without switching to the monitoring dashboard. Overlay vertical marker lines on time-series charts at the timestamps of production deployments fetched from the CI API."
            ),
            (
                "Implement server-side rendering for the initial dashboard load",
                "The dashboard renders entirely client-side, causing a blank screen for one to two seconds on slower connections. Server-render the above-the-fold panels with their initial data and hydrate the client-side chart interactivity after load."
            ),
            (
                "Fix chart legend truncating project names longer than 20 characters",
                "Project names longer than 20 characters are cut off in chart legends with no tooltip to reveal the full name. Truncate with an ellipsis and add a title attribute to show the full name on hover."
            ),
            (
                "Add sparkline summary tiles to the top of the overview page",
                "The overview page starts with a large data table and gives no at-a-glance summary of current performance. Add a row of sparkline tiles above the table, each showing a key metric over the last 30 days."
            ),
            (
                "Write Cypress tests for the filter and export flows",
                "The filter panel and CSV export have no automated test coverage and have caused silent regressions twice. Write Cypress tests that apply each filter type and assert on the resulting row count, and verify that export downloads a non-empty file."
            ),
            (
                "Implement configurable alert thresholds on the KPI cards",
                "KPI cards have no way to indicate when a metric has crossed a meaningful threshold. Allow workspace admins to set warning and critical thresholds per KPI, highlighted with colour coding on the card."
            ),
            (
                "Fix stale data displayed when switching between workspace tabs",
                "Switching workspace tabs shows the previous workspace data for a moment before the new workspace data loads. Clear the data store for the active workspace panels immediately on tab switch before the new data arrives."
            ),
            (
                "Add goal tracking lines to the revenue and user growth charts",
                "The growth charts show actuals but product wants to overlay monthly goal targets for comparison. Allow workspace admins to set monthly goals per metric and render them as a dashed reference line on the time-series charts."
            ),
            (
                "Implement drag-and-drop panel reordering on the custom dashboard",
                "The custom dashboard panels can only be reordered through a settings dialog. Add drag-and-drop reordering using the HTML5 drag API with keyboard fallback using arrow keys and spacebar."
            ),
            (
                "Fix report email attachments not rendering inline charts",
                "Scheduled report emails include chart images that render as broken image tags in some email clients. Pre-render charts to PNG on the server using a headless browser and embed them as base64 inline images in the email body."
            ),
            (
                "Add role-based panel visibility to hide sensitive financial data",
                "All workspace members can see financial and revenue panels regardless of their role. Add a panel access configuration that restricts visibility of designated panels to users with the Admin or Finance role."
            ),
            (
                "Implement an activity feed panel showing recent team actions",
                "Team leads want a live feed of recent task state changes and comments without having to check individual tasks. Build an activity feed panel that polls the activity API every 30 seconds and renders the last 50 events in reverse chronological order."
            ),
            (
                "Fix date grouping returning incorrect weekly totals near month boundaries",
                "Weekly aggregations that span a month boundary return incorrect totals because the SQL query splits the week at the month boundary. Fix the grouping to use ISO week numbers and verify with test data straddling January and March boundaries."
            ),
            (
                "Add full-text search across all dashboard panel titles",
                "Workspaces with many custom panels are hard to navigate without being able to search. Add a search input to the dashboard settings that filters panels by title and description using case-insensitive substring matching."
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
            (
                "Add Open Graph meta tags to all blog posts",
                "Blog posts shared on Slack and LinkedIn show no preview image or description because the Open Graph tags are missing. Add og:title, og:description, og:image, and og:url to the head of every blog post using the CMS metadata fields."
            ),
            (
                "Implement a changelog page fed from the releases API",
                "Customers frequently ask about recent product changes and the team manually copies release notes into a Notion page. Build a /changelog page that fetches and renders releases from the GitHub Releases API at build time."
            ),
            (
                "Fix mobile menu not closing after a navigation item is tapped",
                "The hamburger menu on mobile stays open after a navigation link is tapped, requiring a second tap to close. Close the menu in the navigation link click handler and remove focus from the trigger button."
            ),
            (
                "Add affiliate referral tracking parameter to all CTA links",
                "Affiliate partners send traffic to the site but conversions cannot be attributed because the referral parameter is stripped from CTA redirects. Persist the ref query parameter through the sign-up flow and attach it to the created account."
            ),
            (
                "Build a customer logo carousel for the homepage social proof section",
                "The homepage has a static grid of logos that does not update without a deploy. Replace it with a configurable carousel driven by a CMS content type so marketing can add logos without engineering involvement."
            ),
            (
                "Improve CLS score by reserving space for lazy-loaded images",
                "Images loaded below the fold cause layout shift as they load because no explicit dimensions are set. Add width and height attributes to all img elements and update the CSS to maintain aspect ratios without reserving full image height."
            ),
            (
                "Add hreflang tags to support French and German locale variants",
                "The French and German locale pages exist but are not discoverable by Google because the hreflang relationship tags are missing. Add hreflang tags to the head of every page pointing to all available locale variants."
            ),
            (
                "Fix video autoplay not working on iOS Safari",
                "The product demo video on the homepage uses autoplay with sound, which iOS Safari blocks by default. Add the muted and playsinline attributes and move sound control to a user-initiated toggle."
            ),
            (
                "Implement blog post series navigation with previous and next links",
                "Multi-part blog series have no navigation between posts, causing readers to search for the next entry. Add a series navigation component that displays the series title, total part count, and links to the adjacent posts."
            ),
            (
                "Add an RSS feed for the blog",
                "Several users have requested an RSS feed for following new blog posts without checking the site. Generate an RSS 2.0 feed at /feed.xml at build time from all published blog posts sorted by date descending."
            ),
            (
                "Fix pricing toggle not updating the displayed price",
                "Toggling between monthly and annual billing on the pricing page does not update the displayed price in Firefox due to a CSS transition blocking a repaint. Replace the CSS-only toggle with a JavaScript-driven value swap."
            ),
            (
                "Implement a dark mode stylesheet for the marketing site",
                "The marketing site has no dark mode support, causing a jarring brightness flash for users with system dark mode enabled. Add a dark mode stylesheet triggered by the prefers-color-scheme media query using CSS custom properties for all colour values."
            ),
            (
                "Add an email capture form to the bottom of each blog post",
                "Blog posts drive significant organic traffic but have no lead capture mechanism. Add an email capture form at the bottom of each post that submits to the marketing email list via the Mailchimp API."
            ),
            (
                "Write a redirect rule for the legacy /docs path",
                "The documentation site moved to a subdomain six months ago but the old /docs paths still return 404 instead of redirecting. Add a 301 redirect from /docs/* to docs.example.com/* in the CDN edge configuration."
            ),
            (
                "Fix font subsetting causing missing glyphs on the Czech locale page",
                "The Czech locale page displays boxes instead of special characters because the custom font subset does not include the extended Latin characters needed. Regenerate the font subset with the required Unicode ranges."
            ),
            (
                "Implement lazy loading for all below-the-fold images",
                "All images on the homepage are loaded on the initial page load even if they are far below the fold, increasing the initial payload by 800 KB. Add loading='lazy' to all images below the hero section."
            ),
            (
                "Add breadcrumb navigation to blog category pages",
                "Blog category pages have no breadcrumb, making it unclear to users and search engines how the page fits into the site hierarchy. Add a breadcrumb component with structured data markup to all category and tag listing pages."
            ),
            (
                "Fix header CTA button colour not meeting WCAG AA contrast",
                "The primary CTA button in the site header uses a colour combination that fails the WCAG 2.1 AA contrast ratio requirement on the gradient background. Update the button background or text colour to meet the 4.5:1 minimum ratio."
            ),
            (
                "Implement a job board page fed from the Lever API",
                "Open roles are listed in a static HTML page that requires a developer to update. Replace it with a dynamically generated page that fetches active job postings from the Lever Postings API and renders them with department grouping."
            ),
            (
                "Add WebP format fallback for the hero image slider",
                "The hero slider uses JPEG images that are significantly larger than the equivalent WebP. Add picture elements with WebP source and JPEG fallback for all slider images."
            ),
            (
                "Fix missing alt text on team member photos",
                "The about page team member photos have empty alt attributes, which fails accessibility audits and provides no context to screen reader users. Add descriptive alt text with the team member name and role to each photo."
            ),
            (
                "Implement exit-intent popup for returning visitors without an account",
                "Returning visitors who have not signed up leave the site without any re-engagement prompt. Show an exit-intent popup with a free trial offer when the mouse leaves the viewport for visitors without an auth cookie."
            ),
            (
                "Add tracking pixels for LinkedIn and Google Ads retargeting",
                "The LinkedIn and Google Ads retargeting campaigns are running but the conversion tracking pixels have not been installed. Add the LinkedIn Insight Tag and the Google Ads Global Site Tag to all pages via the tag manager."
            ),
            (
                "Fix terms of service anchor links not scrolling correctly",
                "The table of contents on the terms of service page uses anchor links that scroll to the wrong position because the sticky header covers the target heading. Offset the scroll destination by the height of the sticky header."
            ),
            (
                "Build a competitor comparison table page",
                "The sales team uses a manually maintained spreadsheet to compare features against competitors. Build a dynamically rendered comparison table page configurable from the CMS that the marketing team can keep up to date."
            ),
            (
                "Add a pricing calculator widget to the enterprise plan page",
                "Enterprise prospects want to estimate costs before speaking to sales but the pricing page only shows a contact us call to action. Add an interactive calculator that estimates annual cost based on seat count and storage inputs."
            ),
            (
                "Implement CSP headers to restrict script sources to approved origins",
                "The site has no Content Security Policy headers, leaving it vulnerable to XSS injection through third-party scripts. Add a CSP header via the CDN configuration that restricts script-src to a known safelist and sets report-uri to the violations endpoint."
            ),
            (
                "Fix og:image dimensions not matching the recommended ratio",
                "The Open Graph images are 800x400 pixels instead of the 1200x630 ratio recommended by most platforms. Regenerate the og:image assets at the correct dimensions and update the meta tag content paths."
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
            (
                "Add a --json flag to all list and show commands for scripting",
                "Users piping CLI output into jq report that the human-readable table format cannot be parsed. Add a global --json flag that outputs machine-readable JSON arrays for all list commands and objects for show commands."
            ),
            (
                "Implement a self-update command that pulls the latest release from GitHub",
                "Users on older CLI versions miss bug fixes until they manually reinstall. Add an `update` subcommand that checks the GitHub Releases API, downloads the latest binary for the current OS and architecture, and replaces the running executable."
            ),
            (
                "Fix ANSI escape codes appearing in piped output",
                "ANSI colour codes bleed into piped output when the user redirects CLI output to a file or another command. Detect when stdout is not a TTY and disable colour output automatically."
            ),
            (
                "Add a login command that opens the browser for OAuth device flow",
                "The CLI currently requires manually pasting an API token into the config, which is error-prone. Add a `login` command that initiates an OAuth device flow, prints the code and URL, and polls for the token."
            ),
            (
                "Implement command aliasing in the config file",
                "Power users want to define short aliases for frequently used command sequences. Add an aliases section to the config file and resolve aliases before command parsing, with a warning if an alias shadows a built-in command."
            ),
            (
                "Add a --no-color flag for environments without colour support",
                "Some CI environments and terminal emulators do not support ANSI colours but do not set the NO_COLOR or TERM environment variables. Add an explicit --no-color flag that disables all formatting regardless of terminal detection."
            ),
            (
                "Fix watch mode not detecting changes in symlinked directories",
                "Watch mode uses the filesystem event API which does not follow symlinks, causing changes in linked config directories to be missed. Resolve symlinks before registering watchers and add the real paths to the watch set."
            ),
            (
                "Write a man page for the CLI tool",
                "Developers on Unix systems expect CLI tools to have a man page accessible via `man tool-name`. Generate man pages from the command registry using the cobra-man library and distribute them in the release archive."
            ),
            (
                "Implement a plugin system for third-party command extensions",
                "Teams want to extend the CLI with domain-specific commands without maintaining a fork. Design a plugin discovery mechanism that scans PATH for executables named `tool-<name>` and exposes them as subcommands."
            ),
            (
                "Add a --quiet flag to suppress informational output",
                "CI pipelines that capture CLI output are polluted by informational log lines that make parsing harder. Add a --quiet flag that suppresses all output except errors and the final result."
            ),
            (
                "Fix config init command overwriting an existing config without confirmation",
                "Running `init` in a directory with an existing config silently overwrites it, causing users to lose custom settings. Detect the existing file, print a warning, and prompt for confirmation before overwriting."
            ),
            (
                "Add a diff subcommand to compare two workspace snapshots",
                "Teams want to compare workspace state before and after a migration. Add a `diff` subcommand that accepts two snapshot file paths and prints a coloured diff of added, modified, and removed entities."
            ),
            (
                "Implement structured output for CI pipeline integration",
                "CI systems that consume CLI output have no reliable way to parse results because the format changes with the terminal width. Add a structured output mode that prints one JSON object per line for easy streaming parse in pipelines."
            ),
            (
                "Add a --timeout flag for commands that call the network",
                "Commands calling the API have a hardcoded 30-second timeout that is too short for large workspace syncs. Add a --timeout flag that overrides the default and document the trade-offs between responsiveness and reliability."
            ),
            (
                "Fix table output alignment when values contain Unicode characters",
                "Table column alignment breaks when cell values contain multibyte Unicode characters because the code uses string length instead of visual width. Replace string length with a Unicode-aware rune width function."
            ),
            (
                "Write a migration guide for v1 to v2 breaking changes",
                "The v2 release renamed several commands and changed the config file format, but there is no documentation or tooling to help users migrate. Write a migration guide and add an automated migration command that rewrites the old config format."
            ),
            (
                "Add a --workspace flag as a global override",
                "Users who manage multiple workspaces must edit the config file to switch context between them. Add a global --workspace flag that overrides the configured workspace for a single command invocation."
            ),
            (
                "Implement tab-delimited output mode for spreadsheet import",
                "Some users want to paste CLI output directly into spreadsheet applications. Add a --tsv output format option alongside the existing --json and table formats."
            ),
            (
                "Fix the generate command not respecting .gitignore",
                "The generate command writes output files that match patterns in the .gitignore, causing them to be excluded from version control. Read the .gitignore before writing and warn when an output path would be ignored."
            ),
            (
                "Add a global --profile flag to switch between named config files",
                "Users who work across multiple environments need to maintain separate configs. Add a --profile flag that selects a named config file from ~/.config/tool/profiles/ as an alternative to the default."
            ),
            (
                "Write a semantic versioning workflow using conventional commits",
                "Releases are versioned manually and inconsistently. Set up a CI workflow using semantic-release that derives the version bump from conventional commit messages and generates the changelog automatically."
            ),
            (
                "Fix regression where sync omits soft-deleted records",
                "The v2.1.0 sync command introduced a filter that accidentally excludes soft-deleted records, causing them to reappear after a restore. Remove the erroneous filter and add a test case covering soft-deleted record round-trips."
            ),
            (
                "Add a whoami command showing the authenticated user",
                "Users in CI environments are not sure which credentials the CLI is using for a given invocation. Add a `whoami` command that prints the authenticated user email and the active workspace slug."
            ),
            (
                "Implement pager support for long list output",
                "Long list output fills the terminal buffer and requires scrolling back to see all entries. Pipe output through the system pager (less or more) when stdout is a TTY and the output exceeds the terminal height."
            ),
            (
                "Fix exit code not being set to 1 on partial command failures",
                "Commands that partially succeed exit with code 0 even when some items failed to process, making it impossible for CI to detect failures. Exit with code 1 when any item in a batch operation fails and print a summary of failures."
            ),
            (
                "Add a doctor command that diagnoses common setup problems",
                "Users experiencing authentication or connectivity issues have no self-service diagnostic tool. Add a `doctor` command that checks for a valid config file, a reachable API endpoint, a valid auth token, and the correct CLI version."
            ),
            (
                "Write benchmarks for the large workspace sync code path",
                "The sync command has no performance baseline and optimisation attempts have occasionally introduced regressions. Write benchmarks using the standard library testing.B for syncing workspaces of 100, 1000, and 10000 tasks."
            ),
            (
                "Implement shell integration that sets the workspace based on the project directory",
                "Users switch between multiple project directories and manually update the active workspace each time. Add a shell hook that reads a .workspace file in the current directory and sets TOOL_WORKSPACE automatically on directory change."
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
            (
                "Add a ComboBox component with async option loading",
                "Teams need a searchable select that fetches options from an API as the user types, but the current Select only supports static option lists. Build a ComboBox component following the ARIA combobox pattern with debounced async loading and keyboard navigation."
            ),
            (
                "Fix the Select component losing its value on parent re-render",
                "The Select component resets to its initial value when the parent re-renders due to uncontrolled state not being preserved. Lift the selected value into a controlled state model and document the controlled and uncontrolled usage patterns."
            ),
            (
                "Add size variants to the Button component",
                "Teams are overriding Button styles with custom CSS to get smaller or larger button sizes, creating visual inconsistency. Add sm, md, and lg size props to the Button component and document the appropriate size for each use case."
            ),
            (
                "Implement a colour picker component with hex and HSL input",
                "The design system requires a colour picker for workspace theming but no accessible one exists in the library. Build a colour picker component with a saturation/lightness canvas picker, a hue slider, and hex and HSL text inputs."
            ),
            (
                "Fix the Checkbox indeterminate state not reflecting in the DOM",
                "Setting the indeterminate prop does not update the underlying input element indeterminate property because it is not a DOM attribute. Set the property imperatively via an element reference and document the distinction for consumers."
            ),
            (
                "Add right-to-left layout support to all form components",
                "Teams building Arabic and Hebrew locale interfaces report that form components do not respond to the dir='rtl' attribute. Audit all components for hardcoded left/right values and replace them with logical CSS properties."
            ),
            (
                "Write visual regression tests using Chromatic",
                "Component updates occasionally introduce subtle visual regressions that are not caught by unit tests. Set up Chromatic in the CI pipeline to capture screenshots of all Storybook stories and block merges when pixel diffs exceed the threshold."
            ),
            (
                "Fix the Drawer component not restoring focus on close",
                "Closing the Drawer with the Escape key or the close button does not return focus to the element that triggered it, violating WCAG 2.1 success criterion 2.1.2. Store the trigger element reference on open and restore focus on unmount."
            ),
            (
                "Add a Stepper component for multi-step form flows",
                "Teams building multi-step forms have no standard component and implement ad hoc step indicators with inconsistent accessibility. Build a Stepper component with configurable steps, a current step indicator, and ARIA progress attributes."
            ),
            (
                "Implement compound component pattern for the Tabs component",
                "The Tabs component requires all tab content to be defined as a flat array prop, which limits the ability to conditionally render tabs. Refactor to use a compound component pattern with Tabs.List, Tabs.Tab, Tabs.Panels, and Tabs.Panel sub-components."
            ),
            (
                "Fix the DataTable column resizing handle not working in Firefox",
                "The column resize handle uses pointer capture events that do not fire correctly in Firefox due to a browser quirk. Add a mousedown fallback for Firefox-detected user agents and write a cross-browser test."
            ),
            (
                "Add a virtualised list variant to the Select component",
                "The Select component renders all options into the DOM regardless of list size, causing significant layout cost for option lists over 200 items. Add a VirtualSelect variant that renders only the visible options using a windowing library."
            ),
            (
                "Implement a LazyImage component with IntersectionObserver",
                "Teams are implementing lazy loading inconsistently across products. Build a LazyImage component that uses IntersectionObserver to defer the src load until the image is about to enter the viewport, with a blurred placeholder during loading."
            ),
            (
                "Fix the RadioGroup not updating when options prop changes",
                "Changing the options array passed to RadioGroup does not update the rendered options because the component uses a stale closure. Ensure the options array is read from the current render context on each update."
            ),
            (
                "Add a copy-to-clipboard button to all code examples in Storybook",
                "Developers frequently copy code from Storybook docs but have to select text manually. Add a copy button to all Source blocks in Storybook that copies the raw code to the clipboard and shows a brief confirmation state."
            ),
            (
                "Implement an unstyled base layer for custom design tokens",
                "Teams with their own design systems want the component behaviour without the default styles. Extract an unstyled base layer that ships without any CSS and can be composed with custom styles via className or CSS custom properties."
            ),
            (
                "Fix the FileInput not resetting after form reset",
                "The FileInput component does not clear its displayed filename when the parent form receives a reset event. Listen for the reset event on the closest form element and clear the internal file state."
            ),
            (
                "Add a Skeleton loading component with configurable presets",
                "Teams are building skeleton loaders manually, producing inconsistent animations and shapes. Build a Skeleton component with line, circle, and rectangle shape presets and a configurable animation (pulse or wave)."
            ),
            (
                "Write migration codemods for breaking API changes between major versions",
                "Consumers upgrading between major versions must manually update every usage site of changed APIs. Write jscodeshift codemods for each breaking change and document how to run them in the migration guide."
            ),
            (
                "Fix the Tooltip not repositioning on scroll in overflow containers",
                "Tooltips inside scrollable containers become detached from their anchor elements when the user scrolls. Use a floating-ui update strategy that recalculates position on scroll events from all ancestor elements."
            ),
            (
                "Add a CommandPalette component built on the combobox pattern",
                "Multiple product teams are building custom command palettes independently. Build a CommandPalette component with a fuzzy search input, grouped results, keyboard navigation, and a configurable data source."
            ),
            (
                "Implement token aliasing in the design token system",
                "Teams want to create semantic tokens such as --color-primary that reference base tokens rather than hardcoding values. Add token aliasing support to the token transform pipeline and document the alias syntax."
            ),
            (
                "Fix the Alert close button not visible in high-contrast mode",
                "The Alert component close button uses an SVG icon with a fill that matches the Windows High Contrast theme background. Use currentColor for the icon fill and add a forced-colors media query override."
            ),
            (
                "Add an InputOTP component for one-time password entry",
                "Authentication flows require a one-time password entry field that auto-advances between individual digit inputs. Build an InputOTP component with configurable length, paste support, and auto-focus management between cells."
            ),
            (
                "Implement theme switching via CSS custom properties",
                "Teams want to support light and dark mode with a theme toggle without duplicating component classes. Implement a ThemeProvider that swaps a set of CSS custom property values and persists the selected theme to localStorage."
            ),
            (
                "Fix the Accordion not animating height correctly when content changes",
                "Accordion panels that load async content after opening have the wrong height transition because the height was captured before content loaded. Use ResizeObserver to track panel height changes and apply the transition dynamically."
            ),
            (
                "Add an empty state illustration system with configurable messaging",
                "Teams are using different empty state patterns across products. Build an EmptyState component with slot-based title, description, and action areas, and ship a set of ten SVG illustrations for common empty contexts."
            ),
            (
                "Write accessibility audit documentation for all interactive components",
                "New contributors are adding components without knowing the required ARIA attributes or keyboard interaction patterns. Write an accessibility documentation page for each interactive component specifying the ARIA role, required attributes, and keyboard contract."
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
            (
                "Set up a GitOps workflow using Argo CD for all environments",
                "Deployments to staging and production are triggered manually via CI scripts that have no audit trail. Implement GitOps with Argo CD using a separate config repository per environment with automated sync for staging and manual gate for production."
            ),
            (
                "Implement network policies to restrict inter-pod communication",
                "All pods in the cluster can communicate with each other with no network-level restrictions, violating the principle of least privilege. Define Kubernetes NetworkPolicy resources that allow only the documented service-to-service communication paths."
            ),
            (
                "Add a backup and restore runbook for the PostgreSQL instance",
                "There is no written procedure for restoring the database from a backup, and the last untested backup is three months old. Write a runbook covering backup verification, point-in-time recovery, and the restore procedure for a failed primary."
            ),
            (
                "Configure Velero for scheduled Kubernetes cluster backups",
                "Persistent volume data is not included in the current backup strategy. Install Velero, configure an S3-backed backup schedule every six hours, and write a restore test procedure to validate backup integrity monthly."
            ),
            (
                "Migrate secrets from Kubernetes Secrets to Vault",
                "Kubernetes Secrets are base64-encoded and accessible to any pod with sufficient RBAC permissions. Migrate all application secrets to HashiCorp Vault using the Vault Agent Injector sidecar pattern with short-lived dynamic secrets."
            ),
            (
                "Set up a private container registry with image vulnerability scanning",
                "The cluster pulls images from public Docker Hub without any vulnerability scan, and rate limiting has caused failed pulls in CI. Set up a private registry backed by S3 with Trivy scanning on push and a policy blocking critical CVEs."
            ),
            (
                "Write a disaster recovery playbook covering the three most likely failure modes",
                "The team has no documented procedure for the most likely failure scenarios. Write a disaster recovery playbook covering primary database failure, full cluster loss, and CDN provider outage, with RTO and RPO targets for each."
            ),
            (
                "Implement node auto-provisioning with Karpenter",
                "Scaling the cluster for traffic spikes requires manual intervention to add node groups. Enable Karpenter for node auto-provisioning with provisioner configurations for the API, worker, and database tiers."
            ),
            (
                "Add Kyverno policies to enforce image tag pinning",
                "Several Deployments reference mutable image tags like latest and stable, making rollback unreliable. Add Kyverno admission policies that block any Deployment using a non-digest image reference and generate a report for existing violations."
            ),
            (
                "Configure cluster autoscaler with appropriate scale-in thresholds",
                "The cluster autoscaler uses default scale-in thresholds that are too aggressive, causing unnecessary node churn during workday traffic. Tune the scale-down delay to 10 minutes and set utilisation thresholds based on the actual P95 load profile."
            ),
            (
                "Set up a cost allocation dashboard using Kubecost",
                "Cloud spend is growing but there is no visibility into which workloads or teams are driving the cost. Deploy Kubecost and configure namespace-level cost allocation reports distributed weekly to team leads."
            ),
            (
                "Migrate the CI runner from self-hosted to GitHub-managed runners",
                "Self-hosted CI runners require manual patching and have caused security incidents twice due to stale dependencies. Migrate all workflows to GitHub-managed runners using the ubuntu-latest image and store build artefacts in S3."
            ),
            (
                "Add pre-commit hooks for Terraform formatting and validation",
                "Terraform plan failures in CI are frequently caused by formatting errors that could be caught locally. Add pre-commit hooks for terraform fmt and terraform validate and document the setup in the contributing guide."
            ),
            (
                "Implement a staging data refresh pipeline from anonymised production snapshots",
                "Staging data is months out of date and tests against it are not representative of production conditions. Write a weekly pipeline that dumps production, anonymises PII, and restores the result to the staging database."
            ),
            (
                "Configure log-based alerting for kernel OOM kills on worker nodes",
                "Kernel OOM kills on worker nodes cause silent pod failures that are difficult to diagnose. Configure a log-based alert in Cloud Monitoring that triggers when the kernel OOM killer log pattern appears on any worker node."
            ),
            (
                "Write runbooks for all Alertmanager alerts that currently have none",
                "Eighteen active alerts have no associated runbook, leaving on-call engineers without a response guide. Write a runbook for each alert covering the likely cause, investigation steps, and remediation actions."
            ),
            (
                "Add tagging standards and enforce them via a Terraform policy check",
                "Cloud resources lack consistent tagging, making cost allocation and incident attribution difficult. Define mandatory tags for environment, team, and service and add a Terraform Sentinel policy that blocks plans with untagged resources."
            ),
            (
                "Implement a multi-region failover procedure for the primary RDS instance",
                "The primary RDS instance has no tested failover procedure to the read replica in the secondary region. Document and test the failover steps, automate the DNS switch, and set an RTO target of 10 minutes."
            ),
            (
                "Fix drift between Terraform state and manually created security groups",
                "Several security groups were created manually during an incident and are not in Terraform state, causing plan drift. Import the resources into Terraform and add a drift detection check to the weekly CI run."
            ),
            (
                "Set up development environments using ephemeral Kubernetes namespaces",
                "Developers share a staging namespace for integration testing, causing conflicts between simultaneous deployments. Implement on-demand ephemeral namespaces provisioned by a GitHub Action that tear down after four hours of inactivity."
            ),
            (
                "Add a pre-merge infrastructure plan check to pull requests",
                "Infrastructure pull requests are merged without reviewing the Terraform plan output, leading to unintended resource changes. Add a GitHub Actions workflow that runs terraform plan and posts the output as a PR comment."
            ),
            (
                "Implement image promotion between environments using an OCI tag policy",
                "Images are rebuilt from source for each environment deployment instead of promoting the same image from staging to production. Implement a promotion workflow that retags the staging image digest with the production tag after sign-off."
            ),
            (
                "Configure egress gateway to route external traffic through a static IP",
                "Outbound traffic from the cluster uses dynamic IP addresses, causing problems with third-party IP allowlists. Deploy an egress gateway that routes all external traffic through a NAT gateway with a static Elastic IP."
            ),
            (
                "Add topology spread constraints to distribute pods across availability zones",
                "Critical service pods are occasionally scheduled into a single availability zone, creating a single point of failure. Add topology spread constraints to all critical Deployments targeting zone spread with a maxSkew of one."
            ),
            (
                "Write a capacity planning document based on the last six months of usage metrics",
                "The cluster is sized based on guesswork and has experienced two resource-related incidents this year. Analyse six months of CPU, memory, and network metrics to produce a capacity plan with growth projections and recommended node counts."
            ),
            (
                "Implement a secrets rotation pipeline using Vault dynamic secrets",
                "Database credentials are static and shared, making rotation disruptive. Configure Vault database secrets engine to issue short-lived credentials per service and implement a rolling restart strategy for zero-downtime rotation."
            ),
            (
                "Fix the Helm chart not rendering correctly when values are overridden in CI",
                "The Helm chart produces invalid manifests when the CI value overrides file conflicts with a required value in the base chart. Add validation to the chart NOTES.txt and add a helm template CI check covering the override scenario."
            ),
            (
                "Set up service mesh with mutual TLS for internal service-to-service communication",
                "Internal service-to-service traffic is unencrypted and unauthenticated, leaving it vulnerable to lateral movement after a pod compromise. Deploy Istio in ambient mode with mutual TLS enforcement and document the mTLS policy for new service onboarding."
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
            (
                "Add a runbook link annotation to all active alerting rules",
                "On-call engineers receiving pages have no direct link to the relevant runbook, slowing response time. Add a runbook_url annotation to every Alertmanager rule that links to the corresponding runbook page."
            ),
            (
                "Fix false-positive alerts firing during scheduled maintenance windows",
                "Alerts fire during planned maintenance windows and create unnecessary noise. Configure Alertmanager silence templates that are applied automatically during the scheduled weekly maintenance window."
            ),
            (
                "Implement log parsing for the new structured JSON log format",
                "Services migrated to structured JSON logging in the last quarter but the log pipeline still uses regex patterns written for the old text format. Update the Fluentd parser configurations to use the json parser for all affected services."
            ),
            (
                "Build a Grafana dashboard for Kubernetes node resource saturation",
                "There is no dashboard showing CPU, memory, and disk saturation across cluster nodes during an incident. Build a Grafana dashboard using kube-state-metrics and node-exporter data with red/amber/green threshold annotations."
            ),
            (
                "Add a dead man's snitch alert to catch failures in the nightly job",
                "The nightly report generation job occasionally fails silently and nobody is notified until a customer reports missing data. Configure a dead man's snitch that fires if the job does not emit a heartbeat metric within the expected window."
            ),
            (
                "Fix the Prometheus scrape config missing the new payment service pods",
                "The payment service was deployed without updating the Prometheus scrape config, so its metrics are not being collected. Add the service port annotation to the pod spec and verify the target appears in the Prometheus targets page."
            ),
            (
                "Set up log forwarding from all pods to the centralised Loki instance",
                "Several services write logs only to stdout and they are not forwarded to Loki, making them unavailable after pod deletion. Configure the Promtail DaemonSet to discover and forward logs from all pod stdout streams."
            ),
            (
                "Add metric cardinality guards to prevent label explosion in production",
                "A developer accidentally added a user_id label to a high-frequency metric, causing the time-series database to grow by 200 GB overnight. Add a recording rule and an alert that triggers when any single metric exceeds 10,000 active time series."
            ),
            (
                "Write a runbook for the high memory usage alert on worker nodes",
                "The worker node memory alert has no runbook. Engineers responding to it are unsure whether to evict pods, add nodes, or investigate application-level leaks. Write a runbook covering the investigation steps and escalation criteria."
            ),
            (
                "Implement on-call rotation sync between PagerDuty and the team calendar",
                "The on-call schedule in PagerDuty drifts from the team calendar because engineers update one without updating the other. Write a sync script that reads the PagerDuty schedule API and creates corresponding events in the shared Google Calendar."
            ),
            (
                "Fix the latency histogram bucket boundaries not covering the P99 range",
                "The latency histograms have a maximum bucket of 500ms, but P99 latency on the payment endpoint exceeds 800ms, making the P99 quantile calculation inaccurate. Extend the bucket boundaries to cover up to 5 seconds for all latency histograms."
            ),
            (
                "Add Tempo trace search to the existing Grafana data source",
                "The Grafana instance has Loki and Prometheus data sources but no Tempo connection, preventing trace correlation from log lines. Add the Tempo data source, configure the trace-to-logs correlation, and update the derived field in the Loki data source."
            ),
            (
                "Build an alert fatigue report comparing alert volume to incident count",
                "The team suspects many alerts are noise but there is no data to support pruning decisions. Build a weekly report that joins Alertmanager firing history with PagerDuty incident data to calculate the actionable rate per alert rule."
            ),
            (
                "Set up a staging Prometheus instance to validate recording rules before promoting",
                "Recording rule changes are deployed directly to production Prometheus, and a misconfigured rule caused metric loss for 48 hours. Add a staging Prometheus instance that evaluates new rules against a data replica before promotion."
            ),
            (
                "Implement trace-based testing for the critical checkout flow",
                "The checkout flow is tested with unit and integration tests but there is no trace-level assertion that all expected spans are present. Write trace-based tests using the OpenTelemetry SDK in test mode that assert on the span tree structure."
            ),
            (
                "Fix the log pipeline dropping events when Loki receiver is under load",
                "During traffic spikes the Fluentd forwarder drops log events when the Loki receiver is slow, causing log gaps. Configure retry and buffering in the Fluentd output plugin and add a metric for dropped event counts."
            ),
            (
                "Add span metrics derived from traces as an alternative to manual instrumentation",
                "Several services have traces but no corresponding Prometheus metrics for alerting. Configure the OpenTelemetry Collector span metrics connector to derive RED metrics from trace spans for all instrumented services."
            ),
            (
                "Write a capacity planning alert for disk usage on the metrics storage volume",
                "The Prometheus storage volume has reached 80% capacity twice without any alert. Add an alert that fires at 80% with a ticket-level severity and at 90% with a page-level severity, with a forecast of days remaining based on the fill rate."
            ),
            (
                "Implement continuous profiling via Pyroscope for the API and worker services",
                "Performance regressions in the API and worker services are identified reactively from latency alerts. Deploy Pyroscope and configure the Go and .NET profiling agents to submit CPU and heap profiles for the API and worker services."
            ),
            (
                "Fix grafana-agent not reloading its scrape config after a ConfigMap update",
                "Updating the grafana-agent ConfigMap does not trigger a config reload, requiring a pod restart. Configure the grafana-agent to watch the mounted ConfigMap and reload automatically using the /-/reload HTTP endpoint."
            ),
            (
                "Add a dashboard for third-party API dependency availability",
                "There is no visibility into the availability and latency of the email, payment, and identity third-party dependencies. Build a Grafana dashboard sourced from the circuit breaker and HTTP client metrics showing availability and P95 latency per dependency."
            ),
            (
                "Implement automated log retention policies to delete logs older than 90 days",
                "Log storage costs are growing because old logs are never deleted. Configure Loki retention policies using the compactor to delete logs older than 90 days and verify the policy runs on the expected schedule."
            ),
            (
                "Fix alert notification emails not rendering correctly in Outlook",
                "Alertmanager email notifications use CSS that Outlook does not render, producing plain unstyled text. Update the email template to use table-based HTML layout compatible with Outlook and test it with Litmus."
            ),
            (
                "Build a Grafana dashboard for client-side web vitals from the RUM agent",
                "There is no visibility into the real-user experience of the web application, only server-side metrics. Deploy the Grafana Faro RUM agent on the web app and build a dashboard showing LCP, FID, and CLS percentiles by page."
            ),
            (
                "Add monitors for certificate expiry across all TLS endpoints",
                "TLS certificates have expired twice due to missed renewal reminders. Configure an external certificate expiry monitor for all public endpoints that pages the on-call engineer when a certificate is less than 14 days from expiry."
            ),
            (
                "Implement distributed context propagation for message queue consumers",
                "Traces for async background jobs have no parent context, making it impossible to correlate them with the originating API request. Inject the W3C Trace Context headers into the message payload and extract them in the consumer to create child spans."
            ),
            (
                "Fix the recording rule evaluation interval causing stale metric values",
                "Several recording rules use a one-minute evaluation interval but the underlying metrics are scraped every 30 seconds, causing stale values in dashboards. Align the recording rule evaluation interval with the scrape interval for all derived metrics."
            ),
            (
                "Write an incident severity classification guide",
                "On-call engineers classify incidents inconsistently, leading to SEV1s being treated as SEV3s and vice versa. Write a severity classification guide with examples of each level and link it from the Alertmanager route annotations and the incident response runbook."
            ),
        ],
    };

    public async Task SeedAsync(DataContext dbContext, SeedContext context, CancellationToken ct)
    {
        foreach (var project in context.Projects)
        {
            project.NextTaskScopeId = ProjectTasks[project.Key].Count();
        }

        context.Tasks.AddRange(context.Projects.SelectMany((project, pi) =>
            ProjectTasks[project.Key].Select((task, i) => new ProjectTask
            {
                Name = task.Name,
                Description = task.Description,
                Status = context.Statuses
                    .Where(status => status.Workspace == project.Workspace && status.EntityType == EntityType.Task)
                    .OrderBy(status => status.SortOrder)
                    .ThenBy(status => status.Id)
                    .ElementAt((pi * 8 + i) % context.Statuses.Count(status => status.Workspace == project.Workspace && status.EntityType == EntityType.Task)),
                Owner = context.Users[(pi + i) % context.Users.Count],
                Project = project,
                ProjectScopeId = i,
                Workspace = project.Workspace,
            })
        ));

        await dbContext.ProjectTasks.AddRangeAsync(context.Tasks, ct);
    }
}

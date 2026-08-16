using Xunit;

// Almost every test here drives the same seeded "netptune" workspace through a shared host, so
// classes running concurrently race on workspace-level state: aggregate assertions read counts
// another class is busy changing, and per-workspace toggles get flipped underneath each other.
// Run in parallel the suite failed anywhere between 0 and 53 tests depending on machine load,
// with a different set each time. Serialising costs about 45 seconds and makes it deterministic.
// Per-class workspaces would let this come back out.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

using Xunit;

// These tests mutate process-wide environment variables (e.g. TRACEABILITY_SERVICENAME).
// Running test collections in parallel can cause flaky failures due to cross-test interference.
[assembly: CollectionBehavior(DisableTestParallelization = true)]


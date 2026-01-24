# Migration Guide

## Breaking change: remove internal OpenTelemetry and span creation

Traceability **no longer creates spans** (no `Activity`/`ActivitySource` creation) and no longer includes internal OpenTelemetry auto-instrumentation.

### What changed
- Traceability now focuses on **correlation-id + logging**.
- **Tracing/spans must be configured externally** (e.g., OpenTelemetry SDK/instrumentation in the host application).
- Traceability may still:
  - read `Activity.Current` to enrich logs (`TraceId`/`SpanId`) when external tracing is enabled
  - propagate `traceparent` **best-effort** on outbound `HttpClient` requests when `Activity.Current` exists

### What you need to do
- If you relied on Traceability to create spans automatically, configure tracing in your application (OpenTelemetry) instead.
- Keep using correlation-id middleware/handlers as before.


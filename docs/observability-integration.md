# Observability Tools Integration

## Summary

Traceability is focused on **correlation-id** and **logging enrichment**. It **does not create spans**.\n\nTo use observability tools (Grafana Tempo, Jaeger, Zipkin, Application Insights, etc.), configure **OpenTelemetry in your application**.

## What Traceability Provides

- **Correlation-id propagation** via `X-Correlation-Id`\n- **Log enrichment**:\n  - `CorrelationId` (from `CorrelationContext`)\n  - `TraceId/SpanId/ParentSpanId` when `Activity.Current` exists (external OTel instrumentation)\n  - `RouteName` when `Activity.Current.DisplayName` is available\n- **Outbound trace context propagation**:\n  - `traceparent` is added to outgoing HTTP requests when a valid W3C value is available from `Activity.Current` (best-effort)

## Recommended Setup

1. Configure OpenTelemetry SDK and exporters in your app (tracing/metrics/logs as needed).\n2. Use Traceability for `X-Correlation-Id` propagation and for consistent log fields.\n3. In your log templates/formatters, include both `CorrelationId` and `TraceId` to make troubleshooting easier.


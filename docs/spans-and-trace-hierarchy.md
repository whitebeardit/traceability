# Spans and Trace Hierarchy

## Summary

Traceability **does not create spans** and therefore does not manage span hierarchy.\n\nIf you need spans and trace hierarchy, configure **OpenTelemetry instrumentation in your application**. When OpenTelemetry is configured externally:\n- span hierarchy is handled by OpenTelemetry SDK/instrumentation\n- Traceability can still enrich logs via `Activity.Current` (e.g., `TraceId/SpanId`), and propagate `X-Correlation-Id`


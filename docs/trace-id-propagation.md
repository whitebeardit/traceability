# Trace Context Propagation (traceparent)

## Summary

Traceability **propagates** the `X-Correlation-Id` header and can **propagate** the W3C `traceparent` header on outgoing HTTP calls **when** `Activity.Current` exists (i.e., when OpenTelemetry is configured externally).\n\nTraceability does not create spans and does not generate trace ids.

## Outbound behavior

- If `X-Correlation-Id` exists in `CorrelationContext`, it is sent on outgoing HTTP calls.\n- If `traceparent` is not already present and `Activity.Current.Id` is a valid W3C `traceparent`, Traceability adds it.\n- `tracestate` is not emitted by Traceability. Rely on OpenTelemetry SDK/instrumentation if you need it.


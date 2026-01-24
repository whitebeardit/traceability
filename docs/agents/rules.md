# Rules and Constraints for LLMs

## Mandatory Rules

1. **Always manage correlation-ID via `AsyncLocal<string>` (independent from OpenTelemetry)**
   - ✅ Correlation-ID is **independent** from OpenTelemetry `Activity.TraceId`
   - ✅ Use `AsyncLocal<string>` for correlation-ID isolation across async/await
   - ❌ Never use `ThreadLocal`
   - ❌ Never use simple static variables
   - ❌ Never synchronize correlation-ID with `Activity.TraceId`

2. **Always conditionally compile framework-specific code**
   - ❌ Never mix .NET 8 and .NET Framework code without `#if`
   - ✅ Use `#if NET8_0` for ASP.NET Core code
   - ✅ Use `#if NET48` for .NET Framework code
   - ✅ Always close with `#endif`

3. **Default header: `X-Correlation-Id`**
   - ✅ Use constant `"X-Correlation-Id"` (future via `TraceabilityOptions`)
   - ❌ Never use other header names without configuration

4. **GUID formatted without hyphens (32 characters)**
   - ✅ Use `Guid.NewGuid().ToString("N")`
   - ❌ Never use `ToString()` without parameter (36 characters)

5. **Never modify existing correlation-id**
   - ✅ If header exists in request, use the value
   - ✅ If header doesn't exist, generate new one
   - ❌ Never overwrite existing correlation-id in context

6. **Mandatory asynchronous isolation**
   - ✅ Each isolated asynchronous context maintains its own correlation-id
   - ✅ `Task.Run()` creates new isolated context
   - ✅ `await` preserves context

## Design Constraints

1. **No circular dependencies**: Components should not depend on each other circularly
2. **Thread-safe**: All operations must be thread-safe
3. **Async-safe**: All operations must work correctly with async/await
4. **Zero configuration by default**: Works without configuration, but allows customization

## Mandatory Validations

When adding/modifying code, verify:
- [ ] Correct conditional compilation (`#if NET8_0` / `#if NET48`)
- [ ] Correlation-ID is managed via `AsyncLocal<string>` (independent from `Activity.TraceId`)
- [ ] Traceability does not create spans; tracing is configured externally
- [ ] W3C Trace Context propagated correctly (propagate `traceparent` when `Activity.Current` is available)
- [ ] Header `X-Correlation-Id` used consistently (for backward compatibility)
- [ ] GUID generated without hyphens (`ToString("N")`) when correlation-ID needs to be created
- [ ] Doesn't modify existing correlation-id
- [ ] Traceability does not create spans; log correlation with trace ids happens via `Activity.Current` when OpenTelemetry is configured externally
- [ ] Thread-safe and async-safe
- [ ] XML comments added/updated

## Validation Checklist for Modifications

### Code
- [ ] Correct conditional compilation (`#if NET8_0` / `#if NET48`)
- [ ] Correlation-ID is managed via `AsyncLocal<string>` (independent from `Activity.TraceId`)
- [ ] Traceability does not create spans; tracing is configured externally
- [ ] W3C Trace Context propagated correctly (propagate `traceparent` when `Activity.Current` is available)
- [ ] Header `X-Correlation-Id` used consistently (for backward compatibility)
- [ ] GUID generated without hyphens (`ToString("N")`) when correlation-ID needs to be created
- [ ] Doesn't modify existing correlation-id
- [ ] Traceability does not create spans; ensure OpenTelemetry is configured in the app if tracing is required
- [ ] Thread-safe and async-safe
- [ ] XML comments added/updated

### Tests
- [ ] Unit tests added/updated
- [ ] Tests pass for both frameworks
- [ ] Edge case coverage

### Documentation
- [ ] README.md updated if necessary
- [ ] AGENTS.md updated if necessary
- [ ] Usage examples updated

### Compatibility
- [ ] Works in .NET 8.0
- [ ] Works in .NET Framework 4.8
- [ ] No breaking changes (or documented)

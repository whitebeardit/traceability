# Examples - HTTP Requests

Examples of how correlation-id is managed in HTTP requests.

## Request without Correlation-ID (Generates New)

When a request is made without the `X-Correlation-Id` header, the middleware/handler automatically generates a new correlation-id.

**Request:**
```bash
curl -X GET http://localhost:5000/api/values/test
```

Or via HTTP:
```http
GET /api/values/test HTTP/1.1
Host: localhost:5000
```

**Response (.NET 8 - ASP.NET Core):**
```http
HTTP/1.1 200 OK
Content-Type: application/json
X-Correlation-Id: a1b2c3d4e5f6789012345678901234ab

{
  "correlationId": "a1b2c3d4e5f6789012345678901234ab",
  "message": "Request processed successfully"
}
```

**Response (.NET Framework 4.8 - Web API):**
```http
HTTP/1.1 200 OK
Content-Type: application/json
X-Correlation-Id: f1e2d3c4b5a6978012345678901234cd

{
  "correlationId": "f1e2d3c4b5a6978012345678901234cd",
  "message": "Request processed successfully"
}
```

## Request with Correlation-ID (Reuses Existing)

When a request is made with the `X-Correlation-Id` header, the middleware/handler reuses the provided value.

**Request:**
```bash
curl -X GET http://localhost:5000/api/values/test \
  -H "X-Correlation-Id: 12345678901234567890123456789012"
```

Or via HTTP:
```http
GET /api/values/test HTTP/1.1
Host: localhost:5000
X-Correlation-Id: 12345678901234567890123456789012
```

**Response:**
```http
HTTP/1.1 200 OK
Content-Type: application/json
X-Correlation-Id: 12345678901234567890123456789012

{
  "correlationId": "12345678901234567890123456789012",
  "message": "Request processed successfully"
}
```

**Note:** The same correlation-id is returned in the response, ensuring traceability throughout the call chain.

## Propagation in Call Chains

The correlation-id is automatically propagated in chained HTTP calls.

**Scenario:** Service A → Service B → Service C

**1. Client calls Service A (without correlation-id):**
```http
GET /api/service-a/process HTTP/1.1
Host: service-a.example.com
```

**Service A Response:**
```http
HTTP/1.1 200 OK
X-Correlation-Id: a1b2c3d4e5f6789012345678901234ab
```

**2. Service A calls Service B (correlation-id and trace context automatically propagated):**

Service A's HttpClient automatically adds headers:
```http
GET /api/service-b/data HTTP/1.1
Host: service-b.example.com
X-Correlation-Id: a1b2c3d4e5f6789012345678901234ab
traceparent: 00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01
```

**3. Service B calls Service C (correlation-id and trace context automatically propagated):**
```http
GET /api/service-c/process HTTP/1.1
Host: service-c.example.com
X-Correlation-Id: a1b2c3d4e5f6789012345678901234ab
traceparent: 00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01
```

**Result:** 
- All services in the chain use the same correlation-id (`a1b2c3d4e5f6789012345678901234ab`)
- W3C Trace Context headers (`traceparent`) enable distributed tracing
- Hierarchical spans are maintained across services
- Compatible with OpenTelemetry-compatible observability tools (Jaeger, Zipkin, Application Insights)

## Example with Postman

**Postman Configuration:**

1. Create a new request
2. In the "Headers" tab, add:
   - Key: `X-Correlation-Id`
   - Value: `12345678901234567890123456789012` (optional - if not provided, will be generated)

**Request:**
```
GET http://localhost:5000/api/values/test
Headers:
  X-Correlation-Id: 12345678901234567890123456789012
```

**Response:**
```json
{
  "correlationId": "12345678901234567890123456789012",
  "message": "Request processed successfully"
}
```

And in the response header:
```
X-Correlation-Id: 12345678901234567890123456789012
```

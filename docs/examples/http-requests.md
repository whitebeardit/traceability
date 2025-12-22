# Exemplos - Requisições HTTP

Exemplos de como o correlation-id é gerenciado em requisições HTTP.

## Requisição sem Correlation-ID (Gera Novo)

Quando uma requisição é feita sem o header `X-Correlation-Id`, o middleware/handler gera automaticamente um novo correlation-id.

**Requisição:**
```bash
curl -X GET http://localhost:5000/api/values/test
```

Ou via HTTP:
```http
GET /api/values/test HTTP/1.1
Host: localhost:5000
```

**Resposta (.NET 8 - ASP.NET Core):**
```http
HTTP/1.1 200 OK
Content-Type: application/json
X-Correlation-Id: a1b2c3d4e5f6789012345678901234ab

{
  "correlationId": "a1b2c3d4e5f6789012345678901234ab",
  "message": "Requisição processada com sucesso"
}
```

**Resposta (.NET Framework 4.8 - Web API):**
```http
HTTP/1.1 200 OK
Content-Type: application/json
X-Correlation-Id: f1e2d3c4b5a6978012345678901234cd

{
  "correlationId": "f1e2d3c4b5a6978012345678901234cd",
  "message": "Requisição processada com sucesso"
}
```

## Requisição com Correlation-ID (Reutiliza Existente)

Quando uma requisição é feita com o header `X-Correlation-Id`, o middleware/handler reutiliza o valor fornecido.

**Requisição:**
```bash
curl -X GET http://localhost:5000/api/values/test \
  -H "X-Correlation-Id: 12345678901234567890123456789012"
```

Ou via HTTP:
```http
GET /api/values/test HTTP/1.1
Host: localhost:5000
X-Correlation-Id: 12345678901234567890123456789012
```

**Resposta:**
```http
HTTP/1.1 200 OK
Content-Type: application/json
X-Correlation-Id: 12345678901234567890123456789012

{
  "correlationId": "12345678901234567890123456789012",
  "message": "Requisição processada com sucesso"
}
```

**Observação:** O mesmo correlation-id é retornado na resposta, garantindo rastreabilidade em toda a cadeia de chamadas.

## Propagação em Cadeia de Chamadas

O correlation-id é automaticamente propagado em chamadas HTTP encadeadas.

**Cenário:** Serviço A → Serviço B → Serviço C

**1. Cliente chama Serviço A (sem correlation-id):**
```http
GET /api/service-a/process HTTP/1.1
Host: service-a.example.com
```

**Resposta do Serviço A:**
```http
HTTP/1.1 200 OK
X-Correlation-Id: a1b2c3d4e5f6789012345678901234ab
```

**2. Serviço A chama Serviço B (correlation-id propagado automaticamente):**

O HttpClient do Serviço A automaticamente adiciona o correlation-id:
```http
GET /api/service-b/data HTTP/1.1
Host: service-b.example.com
X-Correlation-Id: a1b2c3d4e5f6789012345678901234ab
```

**3. Serviço B chama Serviço C (correlation-id propagado automaticamente):**
```http
GET /api/service-c/process HTTP/1.1
Host: service-c.example.com
X-Correlation-Id: a1b2c3d4e5f6789012345678901234ab
```

**Resultado:** Todos os serviços na cadeia usam o mesmo correlation-id (`a1b2c3d4e5f6789012345678901234ab`), permitindo rastrear toda a requisição através dos logs de todos os serviços.

## Exemplo com Postman

**Configuração no Postman:**

1. Crie uma nova requisição
2. Na aba "Headers", adicione:
   - Key: `X-Correlation-Id`
   - Value: `12345678901234567890123456789012` (opcional - se não fornecer, será gerado)

**Requisição:**
```
GET http://localhost:5000/api/values/test
Headers:
  X-Correlation-Id: 12345678901234567890123456789012
```

**Resposta:**
```json
{
  "correlationId": "12345678901234567890123456789012",
  "message": "Requisição processada com sucesso"
}
```

E no header da resposta:
```
X-Correlation-Id: 12345678901234567890123456789012
```



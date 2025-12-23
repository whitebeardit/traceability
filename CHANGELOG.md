# 1.0.0 (2025-12-23)


### Bug Fixes

* Adicionar limites de profundidade para prevenir stack overflow ([0a5e362](https://github.com/whitebeardit/traceability/commit/0a5e3622ae31e22cbe3a92b2b604aeb6fa61cd91))
* corrige warning de referÃªncia nula e garante que todos os testes passem ([5d75f50](https://github.com/whitebeardit/traceability/commit/5d75f50840a5495116fd1e6a5c6148461e342594))
* corrige warning de referÃªncia nula em TraceabilityUtilities ([d9a7e30](https://github.com/whitebeardit/traceability/commit/d9a7e30094eddb1174fdcf8ddbf8c6f9c7732d37))
* corrigir bugs crÃ­ticos de thread-safety e tratamento de exceÃ§Ãµes ([0ffd40b](https://github.com/whitebeardit/traceability/commit/0ffd40b0946bc7d705ec11f464d1b2d758d40c86))
* corrigir componentes de leitura para usar TryGetValue() ([639ac26](https://github.com/whitebeardit/traceability/commit/639ac26640463c02fd69fcd3912e39e6d5202880))
* corrigir memory leak no CorrelationIdScopeProvider ([c00e4ee](https://github.com/whitebeardit/traceability/commit/c00e4eec4b4e587175da3d9f845719b2621531c2))
* corrigir warning de nullable reference em GetServiceName ([d04efdd](https://github.com/whitebeardit/traceability/commit/d04efddabb6b74caf53358e3dade1fab7f0d9478))
* **logging:** decorate external scope provider without DI recursion ([2728f8c](https://github.com/whitebeardit/traceability/commit/2728f8c701c2cab0443332cce0ae7b46a9cfb1e3))
* proteger adiÃ§Ã£o de headers contra exceÃ§Ãµes ([db53dc0](https://github.com/whitebeardit/traceability/commit/db53dc02c8472dee6536119328d02e59b30b5db4))
* Validar HeaderName null/vazio em todos os middlewares e handlers ([224e91d](https://github.com/whitebeardit/traceability/commit/224e91d6e47baabf341c0b67e435ef44b1575e09))


### Features

* adicionar DataEnricher e JsonFormatter para logs estruturados ([03de91f](https://github.com/whitebeardit/traceability/commit/03de91ff3f4f6b588dc01479e8954bf22d589ce6))
* adicionar mÃ©todo TryGetValue() ao CorrelationContext ([4958ad6](https://github.com/whitebeardit/traceability/commit/4958ad632954f7d2d4ef9601d7c94410f0cad670))
* adicionar mÃ©todos WithTraceabilityJson para configuraÃ§Ã£o JSON ([a47d679](https://github.com/whitebeardit/traceability/commit/a47d67914f7b7c32c8618303b8093864b358cd82))
* adicionar propriedades de auto-configuraÃ§Ã£o e CorrelationIdStartupFilter ([81da1c5](https://github.com/whitebeardit/traceability/commit/81da1c5e4bce251fb0fbf43d1f454138bc310149))
* adicionar propriedades de configuraÃ§Ã£o para template JSON de logs ([20e4331](https://github.com/whitebeardit/traceability/commit/20e43318392663143d9ff04ddb242641ce2a5616))
* Adicionar sanitizaÃ§Ã£o de Source para seguranÃ§a ([89a5b42](https://github.com/whitebeardit/traceability/commit/89a5b42698a818d49f0c7cec26bec1fd6ab2c260))
* Adicionar suporte a variÃ¡vel de ambiente TRACEABILITY_SERVICENAME ([1e0279c](https://github.com/whitebeardit/traceability/commit/1e0279c58df08aa4968a5e24a759683ccf021fb6))
* **ci:** adiciona semantic-release para versionamento e publicaÃ§Ã£o automÃ¡tica ([6da91c7](https://github.com/whitebeardit/traceability/commit/6da91c73b2cadc0eeaa5bb7f9ce89ac3c901845c))
* implementar auto-configuraÃ§Ã£o completa do Traceability ([62bb8a7](https://github.com/whitebeardit/traceability/commit/62bb8a79647b0ba08c385bc301aeab4092a5fff3))
* integrar TraceabilityOptions em todos os componentes ([c1f7697](https://github.com/whitebeardit/traceability/commit/c1f7697de7c0a6580b671673fd32302caefb3bb5))
* prevenir socket exhaustion com IHttpClientFactory ([4781a28](https://github.com/whitebeardit/traceability/commit/4781a283e93d332bca0fc83e41fb92d8abfb17f6))
* **traceability:** simplify defaults (AddTraceability) + Serilog WithTraceability ([e0c3281](https://github.com/whitebeardit/traceability/commit/e0c328148cd2ceb08ac93f86c696484d25c366dc))


### Performance Improvements

* otimizar alocaÃ§Ãµes em CorrelationIdScopeProvider e CorrelationIdEnricher ([76a0ec3](https://github.com/whitebeardit/traceability/commit/76a0ec3b178ceadb27315cd06baac81531d96f47))

# 1.0.0 (2025-12-23)


### Bug Fixes

* Adicionar limites de profundidade para prevenir stack overflow ([0a5e362](https://github.com/whitebeardit/traceability/commit/0a5e3622ae31e22cbe3a92b2b604aeb6fa61cd91))
* corrige warning de referÃªncia nula e garante que todos os testes passem ([5d75f50](https://github.com/whitebeardit/traceability/commit/5d75f50840a5495116fd1e6a5c6148461e342594))
* corrige warning de referÃªncia nula em TraceabilityUtilities ([d9a7e30](https://github.com/whitebeardit/traceability/commit/d9a7e30094eddb1174fdcf8ddbf8c6f9c7732d37))
* corrigir bugs crÃ­ticos de thread-safety e tratamento de exceÃ§Ãµes ([0ffd40b](https://github.com/whitebeardit/traceability/commit/0ffd40b0946bc7d705ec11f464d1b2d758d40c86))
* corrigir componentes de leitura para usar TryGetValue() ([639ac26](https://github.com/whitebeardit/traceability/commit/639ac26640463c02fd69fcd3912e39e6d5202880))
* corrigir memory leak no CorrelationIdScopeProvider ([c00e4ee](https://github.com/whitebeardit/traceability/commit/c00e4eec4b4e587175da3d9f845719b2621531c2))
* corrigir warning de nullable reference em GetServiceName ([d04efdd](https://github.com/whitebeardit/traceability/commit/d04efddabb6b74caf53358e3dade1fab7f0d9478))
* **logging:** decorate external scope provider without DI recursion ([2728f8c](https://github.com/whitebeardit/traceability/commit/2728f8c701c2cab0443332cce0ae7b46a9cfb1e3))
* proteger adiÃ§Ã£o de headers contra exceÃ§Ãµes ([db53dc0](https://github.com/whitebeardit/traceability/commit/db53dc02c8472dee6536119328d02e59b30b5db4))
* Validar HeaderName null/vazio em todos os middlewares e handlers ([224e91d](https://github.com/whitebeardit/traceability/commit/224e91d6e47baabf341c0b67e435ef44b1575e09))


### Features

* adicionar DataEnricher e JsonFormatter para logs estruturados ([03de91f](https://github.com/whitebeardit/traceability/commit/03de91ff3f4f6b588dc01479e8954bf22d589ce6))
* adicionar mÃ©todo TryGetValue() ao CorrelationContext ([4958ad6](https://github.com/whitebeardit/traceability/commit/4958ad632954f7d2d4ef9601d7c94410f0cad670))
* adicionar mÃ©todos WithTraceabilityJson para configuraÃ§Ã£o JSON ([a47d679](https://github.com/whitebeardit/traceability/commit/a47d67914f7b7c32c8618303b8093864b358cd82))
* adicionar propriedades de auto-configuraÃ§Ã£o e CorrelationIdStartupFilter ([81da1c5](https://github.com/whitebeardit/traceability/commit/81da1c5e4bce251fb0fbf43d1f454138bc310149))
* adicionar propriedades de configuraÃ§Ã£o para template JSON de logs ([20e4331](https://github.com/whitebeardit/traceability/commit/20e43318392663143d9ff04ddb242641ce2a5616))
* Adicionar sanitizaÃ§Ã£o de Source para seguranÃ§a ([89a5b42](https://github.com/whitebeardit/traceability/commit/89a5b42698a818d49f0c7cec26bec1fd6ab2c260))
* Adicionar suporte a variÃ¡vel de ambiente TRACEABILITY_SERVICENAME ([1e0279c](https://github.com/whitebeardit/traceability/commit/1e0279c58df08aa4968a5e24a759683ccf021fb6))
* **ci:** adiciona semantic-release para versionamento e publicaÃ§Ã£o automÃ¡tica ([6da91c7](https://github.com/whitebeardit/traceability/commit/6da91c73b2cadc0eeaa5bb7f9ce89ac3c901845c))
* implementar auto-configuraÃ§Ã£o completa do Traceability ([62bb8a7](https://github.com/whitebeardit/traceability/commit/62bb8a79647b0ba08c385bc301aeab4092a5fff3))
* integrar TraceabilityOptions em todos os componentes ([c1f7697](https://github.com/whitebeardit/traceability/commit/c1f7697de7c0a6580b671673fd32302caefb3bb5))
* prevenir socket exhaustion com IHttpClientFactory ([4781a28](https://github.com/whitebeardit/traceability/commit/4781a283e93d332bca0fc83e41fb92d8abfb17f6))
* **traceability:** simplify defaults (AddTraceability) + Serilog WithTraceability ([e0c3281](https://github.com/whitebeardit/traceability/commit/e0c328148cd2ceb08ac93f86c696484d25c366dc))


### Performance Improvements

* otimizar alocaÃ§Ãµes em CorrelationIdScopeProvider e CorrelationIdEnricher ([76a0ec3](https://github.com/whitebeardit/traceability/commit/76a0ec3b178ceadb27315cd06baac81531d96f47))

## 1.0.0 (2025-12-22)


### Features

* adicionar DataEnricher e JsonFormatter para logs estruturados ([03de91f](https://github.com/whitebeardit/traceability/commit/03de91ff3f4f6b588dc01479e8954bf22d589ce6))
* adicionar mÃ©todo TryGetValue() ao CorrelationContext ([4958ad6](https://github.com/whitebeardit/traceability/commit/4958ad632954f7d2d4ef9601d7c94410f0cad670))
* adicionar mÃ©todos WithTraceabilityJson para configuraÃ§Ã£o JSON ([a47d679](https://github.com/whitebeardit/traceability/commit/a47d67914f7b7c32c8618303b8093864b358cd82))
* adicionar propriedades de auto-configuraÃ§Ã£o e CorrelationIdStartupFilter ([81da1c5](https://github.com/whitebeardit/traceability/commit/81da1c5e4bce251fb0fbf43d1f454138bc310149))
* adicionar propriedades de configuraÃ§Ã£o para template JSON de logs ([20e4331](https://github.com/whitebeardit/traceability/commit/20e43318392663143d9ff04ddb242641ce2a5616))
* Adicionar sanitizaÃ§Ã£o de Source para seguranÃ§a ([89a5b42](https://github.com/whitebeardit/traceability/commit/89a5b42698a818d49f0c7cec26bec1fd6ab2c260))
* Adicionar suporte a variÃ¡vel de ambiente TRACEABILITY_SERVICENAME ([1e0279c](https://github.com/whitebeardit/traceability/commit/1e0279c58df08aa4968a5e24a759683ccf021fb6))
* implementar auto-configuraÃ§Ã£o completa do Traceability ([62bb8a7](https://github.com/whitebeardit/traceability/commit/62bb8a79647b0ba08c385bc301aeab4092a5fff3))
* integrar TraceabilityOptions em todos os componentes ([c1f7697](https://github.com/whitebeardit/traceability/commit/c1f7697de7c0a6580b671673fd32302caefb3bb5))
* prevenir socket exhaustion com IHttpClientFactory ([4781a28](https://github.com/whitebeardit/traceability/commit/4781a283e93d332bca0fc83e41fb92d8abfb17f6))
* **traceability:** simplify defaults (AddTraceability) + Serilog WithTraceability ([e0c3281](https://github.com/whitebeardit/traceability/commit/e0c328148cd2ceb08ac93f86c696484d25c366dc))


### Bug Fixes

* Adicionar limites de profundidade para prevenir stack overflow ([0a5e362](https://github.com/whitebeardit/traceability/commit/0a5e3622ae31e22cbe3a92b2b604aeb6fa61cd91))
* corrigir bugs crÃ­ticos de thread-safety e tratamento de exceÃ§Ãµes ([0ffd40b](https://github.com/whitebeardit/traceability/commit/0ffd40b0946bc7d705ec11f464d1b2d758d40c86))
* corrigir componentes de leitura para usar TryGetValue() ([639ac26](https://github.com/whitebeardit/traceability/commit/639ac26640463c02fd69fcd3912e39e6d5202880))
* corrigir memory leak no CorrelationIdScopeProvider ([c00e4ee](https://github.com/whitebeardit/traceability/commit/c00e4eec4b4e587175da3d9f845719b2621531c2))
* corrigir warning de nullable reference em GetServiceName ([d04efdd](https://github.com/whitebeardit/traceability/commit/d04efddabb6b74caf53358e3dade1fab7f0d9478))
* **logging:** decorate external scope provider without DI recursion ([2728f8c](https://github.com/whitebeardit/traceability/commit/2728f8c701c2cab0443332cce0ae7b46a9cfb1e3))
* proteger adiÃ§Ã£o de headers contra exceÃ§Ãµes ([db53dc0](https://github.com/whitebeardit/traceability/commit/db53dc02c8472dee6536119328d02e59b30b5db4))
* Validar HeaderName null/vazio em todos os middlewares e handlers ([224e91d](https://github.com/whitebeardit/traceability/commit/224e91d6e47baabf341c0b67e435ef44b1575e09))


### Performance Improvements

* otimizar alocaÃ§Ãµes em CorrelationIdScopeProvider e CorrelationIdEnricher ([76a0ec3](https://github.com/whitebeardit/traceability/commit/76a0ec3b178ceadb27315cd06baac81531d96f47))

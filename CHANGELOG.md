## [3.0.1](https://github.com/whitebeardit/traceability/compare/v3.0.0...v3.0.1) (2026-01-25)


### Bug Fixes

* licence file format to be recognized by GiyHub ([16510fa](https://github.com/whitebeardit/traceability/commit/16510faeec4fad173ef84559a5b72fca6a065235))

# [3.0.0](https://github.com/whitebeardit/traceability/compare/v2.4.0...v3.0.0) (2026-01-24)


* refactor(traceability)!: remove internal OpenTelemetry and span creation (#35) ([02caaff](https://github.com/whitebeardit/traceability/commit/02caaff71f07919cb62b9f345c0d685a619d4f32)), closes [#35](https://github.com/whitebeardit/traceability/issues/35)


### BREAKING CHANGES

* Traceability no longer provides internal OpenTelemetry auto-instrumentation or automatic span (Activity) creation; tracing must be configured externally.

# [2.4.0](https://github.com/whitebeardit/traceability/compare/v2.3.1...v2.4.0) (2026-01-24)


### Bug Fixes

* Merge branch 'main' of github.com:whitebeardit/traceability ([34a74b0](https://github.com/whitebeardit/traceability/commit/34a74b01b1b290c0d9678c380fb0378284cc8832))
* samples fixes ([dab92e5](https://github.com/whitebeardit/traceability/commit/dab92e58d4b4bb3cddca6a86ba6276a54b1dca79))


### Features

* **build:** add netstandard2.0 target framework support ([#34](https://github.com/whitebeardit/traceability/issues/34)) ([4ad1c73](https://github.com/whitebeardit/traceability/commit/4ad1c735e3002b866959c13a4684d28224f64c48))

## [2.3.1](https://github.com/whitebeardit/traceability/compare/v2.3.0...v2.3.1) (2026-01-22)


### Bug Fixes

* docs - adding deepweek AI and buy-me-a-coffee ([b6f6788](https://github.com/whitebeardit/traceability/commit/b6f678890ea325e96820a2c9631d82c0ef56de38))

# [2.3.0](https://github.com/whitebeardit/traceability/compare/v2.2.0...v2.3.0) (2026-01-21)


### Bug Fixes

* documentation ([6cd5cf9](https://github.com/whitebeardit/traceability/commit/6cd5cf929c79bed2089826d4cc41021674d99fda))
* **http:** harden traceparent propagation ([#32](https://github.com/whitebeardit/traceability/issues/32)) ([1c86da1](https://github.com/whitebeardit/traceability/commit/1c86da1f2f2cbeefe3e1e8f46b481835779f4d59))


### Features

* **core:** refactor correlation-id independence from trace ID ([#33](https://github.com/whitebeardit/traceability/issues/33)) ([29024fc](https://github.com/whitebeardit/traceability/commit/29024fcdd48ea0a843ee74c9f82337395c5bdad6))

## [2.2.2](https://github.com/whitebeardit/traceability/compare/v2.2.1...v2.2.2) (2025-12-29)


### Bug Fixes

* **http:** harden traceparent propagation ([#32](https://github.com/whitebeardit/traceability/issues/32)) ([ff4f9b8](https://github.com/whitebeardit/traceability/commit/ff4f9b8a2063fca7827d9d64309d387228ce1884))

## [2.2.1](https://github.com/whitebeardit/traceability/compare/v2.2.0...v2.2.1) (2025-12-29)


### Bug Fixes

* documentation ([d8ff8ec](https://github.com/whitebeardit/traceability/commit/d8ff8eca531745ed5461dcf788458afb0120d741))

# [2.2.0](https://github.com/whitebeardit/traceability/compare/v2.1.2...v2.2.0) (2025-12-28)


### Bug Fixes

* ensure Activity is available in PreSendRequestHeaders for debug mode ([4f6aaea](https://github.com/whitebeardit/traceability/commit/4f6aaeaaca2f454a94353121fde551cdf5075dd1))
* normalize Index action route name to 'Controller/' format ([f2f16d8](https://github.com/whitebeardit/traceability/commit/f2f16d8f9ac976f48cd8d5b309ed051dbd110640))


### Features

* Add MVC 5 Attribute Routing support ([a9391db](https://github.com/whitebeardit/traceability/commit/a9391db58aee617ed0dde291d50a3c19911f8a2b))
* Add RouteNameEnricher to include route name in structured logs ([6b0916a](https://github.com/whitebeardit/traceability/commit/6b0916a04b5b84d43b2f0f0a4568daa869ac8cea))
* **logging:** enrich trace context + promote fields in json ([5fa0b32](https://github.com/whitebeardit/traceability/commit/5fa0b32766c9f536d0a74527da780f30a95059a8))

## [2.1.2](https://github.com/whitebeardit/traceability/compare/v2.1.1...v2.1.2) (2025-12-28)


### Bug Fixes

* resolve compiler warnings + add Cursor AI rules ([#30](https://github.com/whitebeardit/traceability/issues/30)) ([9715738](https://github.com/whitebeardit/traceability/commit/971573846b870c4d565e1858f3ed94fcc2bd0900))

## [2.1.1](https://github.com/whitebeardit/traceability/compare/v2.1.0...v2.1.1) (2025-12-27)


### Bug Fixes

* trigger CI ([a641a3b](https://github.com/whitebeardit/traceability/commit/a641a3b3ccc0cdcb1ca095f56174cfb6fd28f294))

# [2.1.0](https://github.com/whitebeardit/traceability/compare/v2.0.0...v2.1.0) (2025-12-26)


### Features

* Auto-instrumentação zero-code e naming consistente de spans para .NET Framework 4.8 ([#26](https://github.com/whitebeardit/traceability/issues/26)) ([e20c770](https://github.com/whitebeardit/traceability/commit/e20c770ad29008f23272512f30284a511b947d9d))

# [2.0.0](https://github.com/whitebeardit/traceability/compare/v1.1.0...v2.0.0) (2025-12-24)


### Bug Fixes

* fixes tests ([3ecc57e](https://github.com/whitebeardit/traceability/commit/3ecc57e59a8f244be52f0a6465fadf69aee79ff6))


* feat!: add OpenTelemetry support ([65f25be](https://github.com/whitebeardit/traceability/commit/65f25be12a40b901146358450b348da7866b1d6b))


### BREAKING CHANGES

* Adds OpenTelemetry ActivitySource support, which may require additional configuration for users not using OpenTelemetry.

# [1.1.0](https://github.com/whitebeardit/traceability/compare/v1.0.1...v1.1.0) (2025-12-23)


### Features

* incluir README e documentaÃ§Ã£o completa no pacote NuGet ([e7e5d82](https://github.com/whitebeardit/traceability/commit/e7e5d8203412e9550bd3a67a08fcb86db770baff))

## [1.0.1](https://github.com/whitebeardit/traceability/compare/v1.0.0...v1.0.1) (2025-12-23)


### Bug Fixes

* **ci:** configurar Git remote com token antes do semantic-release ([cd38411](https://github.com/whitebeardit/traceability/commit/cd38411e20ae28813d1c79c7c6c2dda4fa761d04))
* **ci:** remover config manual de Git remote - checkout jÃ¡ faz isso ([07d25b6](https://github.com/whitebeardit/traceability/commit/07d25b68caa0149ca3065375392a2abccad5f030))

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

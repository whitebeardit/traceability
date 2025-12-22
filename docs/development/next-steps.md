# Próximos Passos - Implementação do CI/CD

Este guia descreve os passos necessários para finalizar a implementação do CI/CD com semantic-release.

## Checklist de Implementação

### ✅ Concluído

- [x] Node.js e npm instalados localmente
- [x] Dependências do semantic-release instaladas (`npm install`)
- [x] Configuração do semantic-release (`.releaserc.json`)
- [x] Workflow do GitHub Actions atualizado (`.github/workflows/ci.yml`)
- [x] Scripts de suporte criados (`scripts/`)
- [x] Documentação criada (`docs/development/ci-cd.md`)
- [x] Branch `feat/add-ci-cd-nugrt-publish` criada
- [x] Branch `staging` criada

### 🔲 Pendente

- [ ] Fazer commit das mudanças relacionadas ao CI/CD
- [ ] Fazer push da branch `feat/add-ci-cd-nugrt-publish`
- [ ] Configurar `NUGET_API_KEY` no GitHub Secrets
- [ ] Criar Pull Request para `main`
- [ ] Testar o pipeline (após merge)

## Passo 1: Fazer Commit das Mudanças

Adicione apenas os arquivos relacionados ao CI/CD:

```powershell
# Adicionar arquivos novos do CI/CD
git add .releaserc.json
git add package.json
git add package-lock.json
git add .github/workflows/ci.yml
git add docs/development/
git add docs/setup-nodejs.md
git add scripts/

# Fazer commit seguindo Conventional Commits
git commit -m "feat(ci): adiciona semantic-release para versionamento e publicação automática

- Configura semantic-release com plugins para changelog, git e github
- Adiciona workflow do GitHub Actions para CI/CD
- Suporta prerelease na branch staging e release na branch main
- Adiciona documentação do processo de CI/CD
- Adiciona scripts de suporte e verificação"
```

**Nota**: Se houver outros arquivos modificados que não são relacionados ao CI/CD, você pode fazer commits separados ou descartá-los com `git restore <arquivo>`.

## Passo 2: Fazer Push da Branch

```powershell
# Fazer push da branch de feature
git push origin feat/add-ci-cd-nugrt-publish

# Fazer push da branch staging (se ainda não foi feito)
git push origin staging
```

## Passo 3: Configurar GitHub Secrets

Antes de fazer merge, configure o secret necessário:

1. Acesse o repositório no GitHub
2. Vá em **Settings** → **Secrets and variables** → **Actions**
3. Clique em **New repository secret**
4. Configure:
   - **Name**: `NUGET_API_KEY`
   - **Value**: Sua API key do NuGet.org
     - Obtenha em: https://www.nuget.org/account/apikeys
     - Crie uma nova key se necessário
     - Escolha "Automatically use from GitHub Actions" se disponível

## Passo 4: Criar Pull Request

1. No GitHub, crie uma Pull Request de `feat/add-ci-cd-nugrt-publish` para `main`
2. Adicione uma descrição explicando as mudanças
3. Aguarde revisão e aprovação

## Passo 5: Testar o Pipeline

Após o merge na `main`, teste o pipeline:

### Teste 1: Prerelease na branch staging

```powershell
git checkout staging
git pull origin staging

# Fazer uma mudança pequena (ex: atualizar README)
# Fazer commit seguindo Conventional Commits
git commit -m "docs: atualiza documentação do CI/CD"
git push origin staging
```

**Resultado esperado**:
- Pipeline executa builds e testes
- Se passar, cria versão prerelease (ex: `1.0.1-alpha.1`)
- Publica no NuGet.org como prerelease
- Cria release no GitHub

### Teste 2: Release estável na branch main

```powershell
git checkout main
git pull origin main

# Fazer uma mudança pequena
git commit -m "docs: melhora documentação"
git push origin main
```

**Resultado esperado**:
- Pipeline executa builds e testes
- Se passar, cria versão estável (ex: `1.0.1`)
- Publica no NuGet.org como release estável
- Cria tag Git e release no GitHub
- Commita `CHANGELOG.md` e `.csproj` atualizado

## Verificações Importantes

### Antes de fazer merge:

- [ ] `NUGET_API_KEY` configurado no GitHub Secrets
- [ ] Branch `staging` existe no remoto
- [ ] Todos os arquivos do CI/CD foram commitados
- [ ] Commits seguem o padrão Conventional Commits

### Após o merge:

- [ ] Verificar que o workflow aparece em Actions
- [ ] Verificar que builds e testes passam
- [ ] Testar publicação fazendo push para `staging` ou `main`

## Troubleshooting

### Pipeline não executa

- Verifique se o workflow está no caminho correto: `.github/workflows/ci.yml`
- Verifique se está fazendo push para `main` ou `staging`
- Verifique os logs em **Actions** no GitHub

### Erro ao publicar no NuGet

- Verifique se `NUGET_API_KEY` está configurado corretamente
- Verifique se a API key tem permissão para publicar
- Verifique os logs do job `release` no GitHub Actions

### Versão não é incrementada

- Verifique se os commits seguem Conventional Commits
- Commits do tipo `chore`, `docs` (sem `BREAKING CHANGE`) não incrementam versão
- Verifique a mensagem do commit no formato: `tipo(escopo): descrição`

## Recursos

- [Documentação do CI/CD](ci-cd.md)
- [Setup do Node.js](../setup-nodejs.md)
- [Conventional Commits](https://www.conventionalcommits.org/)
- [semantic-release Documentation](https://semantic-release.gitbook.io/)


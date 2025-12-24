# Next Steps - CI/CD Implementation

This guide describes the steps needed to finalize the CI/CD implementation with semantic-release.

## Implementation Checklist

### ✅ Completed

- [x] Node.js and npm installed locally
- [x] semantic-release dependencies installed (`npm install`)
- [x] semantic-release configuration (`.releaserc.json`)
- [x] GitHub Actions workflow updated (`.github/workflows/ci.yml`)
- [x] Support scripts created (`scripts/`)
- [x] Documentation created (`docs/development/ci-cd.md`)
- [x] Branch `feat/add-ci-cd-nugrt-publish` created
- [x] Branch `staging` created

### 🔲 Pending

- [ ] Commit CI/CD related changes
- [ ] Push branch `feat/add-ci-cd-nugrt-publish`
- [ ] Configure `NUGET_API_KEY` in GitHub Secrets
- [ ] Create Pull Request to `main`
- [ ] Test the pipeline (after merge)

## Step 1: Commit Changes

Add only CI/CD related files:

```powershell
# Add new CI/CD files
git add .releaserc.json
git add package.json
git add package-lock.json
git add .github/workflows/ci.yml
git add docs/development/
git add docs/setup-nodejs.md
git add scripts/

# Commit following Conventional Commits
git commit -m "feat(ci): add semantic-release for automatic versioning and publishing

- Configure semantic-release with plugins for changelog, git and github
- Add GitHub Actions workflow for CI/CD
- Support prerelease on staging branch and release on main branch
- Add CI/CD process documentation
- Add support and verification scripts"
```

**Note**: If there are other modified files that are not related to CI/CD, you can make separate commits or discard them with `git restore <file>`.

## Step 2: Push Branch

```powershell
# Push feature branch
git push origin feat/add-ci-cd-nugrt-publish

# Push staging branch (if not done yet)
git push origin staging
```

## Step 3: Configure GitHub Secrets

Before merging, configure the required secret:

1. Go to the repository on GitHub
2. Go to **Settings** → **Secrets and variables** → **Actions**
3. Click **New repository secret**
4. Configure:
   - **Name**: `NUGET_API_KEY`
   - **Value**: Your NuGet.org API key
     - Get it at: https://www.nuget.org/account/apikeys
     - Create a new key if necessary
     - Choose "Automatically use from GitHub Actions" if available

## Step 4: Create Pull Request

1. On GitHub, create a Pull Request from `feat/add-ci-cd-nugrt-publish` to `main`
2. Add a description explaining the changes
3. Wait for review and approval

## Step 5: Test the Pipeline

After merging to `main`, test the pipeline:

### Test 1: Prerelease on staging branch

```powershell
git checkout staging
git pull origin staging

# Make a small change (e.g., update README)
# Commit following Conventional Commits
git commit -m "docs: update CI/CD documentation"
git push origin staging
```

**Expected result**:
- Pipeline runs builds and tests
- If it passes, creates prerelease version (e.g., `1.0.1-alpha.1`)
- Publishes to NuGet.org as prerelease
- Creates GitHub release

### Test 2: Stable release on main branch

```powershell
git checkout main
git pull origin main

# Make a small change
git commit -m "docs: improve documentation"
git push origin main
```

**Expected result**:
- Pipeline runs builds and tests
- If it passes, creates stable version (e.g., `1.0.1`)
- Publishes to NuGet.org as stable release
- Creates Git tag and GitHub release
- Commits updated `CHANGELOG.md` and `.csproj`

## Important Checks

### Before merging:

- [ ] `NUGET_API_KEY` configured in GitHub Secrets
- [ ] `staging` branch exists on remote
- [ ] All CI/CD files have been committed
- [ ] Commits follow Conventional Commits pattern

### After merge:

- [ ] Verify workflow appears in Actions
- [ ] Verify builds and tests pass
- [ ] Test publishing by pushing to `staging` or `main`

## Troubleshooting

### Pipeline does not run

- Check if workflow is in correct path: `.github/workflows/ci.yml`
- Check if you're pushing to `main` or `staging`
- Check logs in **Actions** on GitHub

### Error publishing to NuGet

- Check if `NUGET_API_KEY` is configured correctly
- Check if API key has permission to publish
- Check logs from `release` job in GitHub Actions

### Version is not incremented

- Check if commits follow Conventional Commits
- Commits of type `chore`, `docs` (without `BREAKING CHANGE`) don't increment version
- Check commit message in format: `type(scope): description`

## Resources

- [CI/CD Documentation](ci-cd.md)
- [Node.js Setup](../setup-nodejs.md)
- [Conventional Commits](https://www.conventionalcommits.org/)
- [semantic-release Documentation](https://semantic-release.gitbook.io/)

# Cursor Rules

This directory contains rules and guidelines for Cursor AI to follow when working on this project.

## Rule Files

### `conventional-commits.md`
Complete guide on the Conventional Commits pattern used in the project. Contains:
- Required commit format
- Allowed commit types
- Examples of valid and invalid commits
- Pull Request rules
- Validation checklist

### `ai-commit-guidelines.md`
Specific guidelines for Cursor AI:
- How to suggest commits and PRs
- Validation before suggesting
- Reminders for the user
- Special cases

## Why These Rules Exist

This project uses **semantic-release** for automatic versioning and publishing. Semantic-release analyzes commits following the [Conventional Commits](https://www.conventionalcommits.org/) pattern to determine:

- Whether to generate a new version
- What type of version (MAJOR, MINOR, PATCH)
- What to include in the changelog

**Commits that don't follow the pattern are ignored** by semantic-release, resulting in:
- ❌ Versions not automatically generated
- ❌ Outdated changelog
- ❌ Publication not performed

## How to Use

Cursor AI automatically reads these rules and applies them when:
- You request commit creation
- You request Pull Request creation
- You make code changes

If you create a commit or PR that doesn't follow the pattern, Cursor AI will:
1. Alert about the problem
2. Suggest the correct format
3. Explain why it's important

## References

- [Conventional Commits Specification](https://www.conventionalcommits.org/)
- [Semantic Release Documentation](https://semantic-release.gitbook.io/)
- [Angular Commit Message Guidelines](https://github.com/angular/angular/blob/main/CONTRIBUTING.md#commit)

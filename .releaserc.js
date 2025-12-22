module.exports = {
  branches: [
    'main',
    {
      name: 'staging',
      channel: 'staging',
      prerelease: 'staging',
    },
  ],
  tagFormat: 'v${version}',
  plugins: [
    '@semantic-release/commit-analyzer',
    [
      '@semantic-release/release-notes-generator',
      {
        preset: 'conventionalcommits',
      },
    ],
    [
      '@semantic-release/changelog',
      {
        changelogFile: 'CHANGELOG.md',
      },
    ],
    [
      '@droidsolutions-oss/semantic-release-nuget',
      {
        projectPath: 'src/Traceability/Traceability.csproj',
        nugetServer: 'https://api.nuget.org/v3/index.json',
        usePackageVersion: true,
        includeSymbols: false,
      },
    ],
    [
      '@semantic-release/git',
      {
        assets: ['CHANGELOG.md'],
        message: 'chore(release): ${nextRelease.version} [skip ci]\\n\\n${nextRelease.notes}',
      },
    ],
    '@semantic-release/github',
  ],
};

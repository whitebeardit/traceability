const branchName = process.env.RELEASE_BRANCH_NAME || process.env.GITHUB_REF_NAME || '';
const isStagingBranch = branchName === 'staging';
const packageId = process.env.NUGET_PACKAGE_ID || (isStagingBranch ? 'Traceability.Staging' : 'Traceability');
const nugetApiKeyEnvVar = process.env.NUGET_TOKEN ? 'NUGET_TOKEN' : 'NUGET_API_KEY';

const packArguments = [`/p:PackageId=${packageId}`];

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
        project: 'src/Traceability/Traceability.csproj',
        packageId,
        packArguments,
        publish: true,
        source: 'https://api.nuget.org/v3/index.json',
        apiKeyEnvironmentVariable: nugetApiKeyEnvVar,
        includeSymbols: false,
        packageOutputPath: 'artifacts',
        buildConfiguration: 'Release',
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

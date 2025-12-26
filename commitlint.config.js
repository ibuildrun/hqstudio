// Conventional Commits configuration
// https://www.conventionalcommits.org/

module.exports = {
  extends: ['@commitlint/config-conventional'],
  rules: {
    'type-enum': [
      2,
      'always',
      [
        'feat',     // New feature
        'fix',      // Bug fix
        'docs',     // Documentation
        'style',    // Formatting (no code change)
        'refactor', // Code refactoring
        'perf',     // Performance improvement
        'test',     // Tests
        'build',    // Build/dependencies
        'ci',       // CI/CD
        'chore',    // Miscellaneous
        'revert',   // Revert changes
      ],
    ],
    'scope-enum': [
      1,
      'always',
      [
        'api',      // HQStudio.API
        'web',      // HQStudio.Web
        'desktop',  // HQStudio.Desktop
        'tests',    // Tests
        'docker',   // Docker configuration
        'ci',       // CI/CD
        'deps',     // Dependencies
        'release',  // Automatic releases
      ],
    ],
    'subject-case': [0],
    'body-max-line-length': [0],
  },
};

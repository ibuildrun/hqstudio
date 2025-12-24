// Conventional Commits configuration
// https://www.conventionalcommits.org/

module.exports = {
  extends: ['@commitlint/config-conventional'],
  rules: {
    'type-enum': [
      2,
      'always',
      [
        'feat',     // Новая функциональность
        'fix',      // Исправление бага
        'docs',     // Документация
        'style',    // Форматирование (не влияет на код)
        'refactor', // Рефакторинг
        'perf',     // Улучшение производительности
        'test',     // Тесты
        'build',    // Сборка/зависимости
        'ci',       // CI/CD
        'chore',    // Прочее
        'revert',   // Откат изменений
      ],
    ],
    'scope-enum': [
      1,
      'always',
      [
        'api',      // HQStudio.API
        'web',      // HQStudio.Web
        'desktop',  // HQStudio.Desktop
        'tests',    // Тесты
        'docker',   // Docker конфигурация
        'ci',       // CI/CD
        'deps',     // Зависимости
        'release',  // Автоматические релизы
      ],
    ],
    'subject-case': [0],
    'body-max-line-length': [0],
  },
};

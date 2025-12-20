/** @type {import('next').NextConfig} */
const nextConfig = {
  // Для GitHub Pages используем static export
  output: process.env.GITHUB_ACTIONS ? 'export' : 'standalone',
  
  // Base path для GitHub Pages (имя репозитория)
  basePath: process.env.GITHUB_ACTIONS ? '/hqstudio' : '',
  assetPrefix: process.env.GITHUB_ACTIONS ? '/hqstudio/' : '',
  
  images: {
    unoptimized: process.env.GITHUB_ACTIONS ? true : false,
    remotePatterns: [
      {
        protocol: 'https',
        hostname: 'images.unsplash.com',
      },
      {
        protocol: 'https',
        hostname: 'picsum.photos',
      },
    ],
  },
  experimental: {
    serverActions: {
      bodySizeLimit: '2mb',
    },
  },
  
  // Trailing slash для статических файлов
  trailingSlash: process.env.GITHUB_ACTIONS ? true : false,
}

module.exports = nextConfig

import type { Metadata } from 'next'
import './globals.css'
import { APP_NAME, OWNER_NAME, ADDRESS, PHONE } from '@/lib/constants'

export const metadata: Metadata = {
  metadataBase: new URL('https://hqstudio.ru'),
  title: {
    default: `${APP_NAME} | Тюнинг и шумоизоляция автомобилей в Сургуте`,
    template: `%s | ${APP_NAME}`,
  },
  description: 'Профессиональная шумоизоляция, установка автодоводчиков, контурная подсветка Ambient Light и антихром в Сургуте. Премиальный тюнинг вашего автомобиля.',
  keywords: [
    'шумоизоляция авто Сургут',
    'тюнинг Сургут',
    'автодоводчики',
    'антихром',
    'контурная подсветка',
    'ambient light',
    'HQ Studio',
    'автосервис Сургут',
  ],
  authors: [{ name: OWNER_NAME }],
  creator: APP_NAME,
  publisher: APP_NAME,
  formatDetection: {
    email: false,
    address: false,
    telephone: false,
  },
  openGraph: {
    type: 'website',
    locale: 'ru_RU',
    url: 'https://hqstudio.ru',
    siteName: APP_NAME,
    title: `${APP_NAME} | Премиальный тюнинг в Сургуте`,
    description: 'Шумоизоляция, автодоводчики, Ambient Light и антихром. Создаём комфорт и стиль вашего автомобиля.',
    images: [
      {
        url: '/og-image.jpg',
        width: 1200,
        height: 630,
        alt: `${APP_NAME} - Тюнинг студия`,
      },
    ],
  },
  twitter: {
    card: 'summary_large_image',
    title: `${APP_NAME} | Тюнинг Сургут`,
    description: 'Профессиональная шумоизоляция и тюнинг автомобилей',
    images: ['/og-image.jpg'],
  },
  robots: {
    index: true,
    follow: true,
    googleBot: {
      index: true,
      follow: true,
      'max-video-preview': -1,
      'max-image-preview': 'large',
      'max-snippet': -1,
    },
  },
  verification: {
    yandex: 'your-yandex-verification-code',
  },
  alternates: {
    canonical: 'https://hqstudio.ru',
  },
}

// JSON-LD structured data for SEO
const jsonLd = {
  '@context': 'https://schema.org',
  '@type': 'AutoRepair',
  name: APP_NAME,
  description: 'Профессиональная шумоизоляция, установка автодоводчиков и тюнинг автомобилей',
  url: 'https://hqstudio.ru',
  telephone: PHONE,
  address: {
    '@type': 'PostalAddress',
    streetAddress: ADDRESS,
    addressLocality: 'Сургут',
    addressRegion: 'Ханты-Мансийский автономный округ — Югра',
    postalCode: '628415',
    addressCountry: 'RU',
  },
  geo: {
    '@type': 'GeoCoordinates',
    latitude: 61.2500,
    longitude: 73.4167,
  },
  openingHoursSpecification: {
    '@type': 'OpeningHoursSpecification',
    dayOfWeek: ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'],
    opens: '09:00',
    closes: '21:00',
  },
  priceRange: '₽₽',
  image: 'https://hqstudio.ru/og-image.jpg',
  sameAs: [],
}

export default function RootLayout({
  children,
}: {
  children: React.ReactNode
}) {
  return (
    <html lang="ru" className="bg-black text-white antialiased" style={{ scrollBehavior: 'smooth' }}>
      <head>
        <link rel="preconnect" href="https://fonts.googleapis.com" />
        <link rel="preconnect" href="https://fonts.gstatic.com" crossOrigin="anonymous" />
        <link href="https://fonts.googleapis.com/css2?family=Manrope:wght@200;300;400;500;600;700;800&display=swap" rel="stylesheet" />
        <link rel="icon" href="/favicon.svg" type="image/svg+xml" />
        <link rel="icon" href="/favicon.ico" sizes="any" />
        <link rel="apple-touch-icon" href="/icon-192.svg" />
        <script
          type="application/ld+json"
          dangerouslySetInnerHTML={{ __html: JSON.stringify(jsonLd) }}
        />
      </head>
      <body>
        {children}
        <div className="noise-bg" />
      </body>
    </html>
  )
}

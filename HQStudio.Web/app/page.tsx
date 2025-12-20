import { Suspense } from 'react'
import { APP_NAME, OWNER_NAME, SERVICES, DEFAULT_SITE_DATA } from '@/lib/constants'
import ClientPage from './ClientPage'

// Server-side rendered metadata for SEO
export default function Home() {
  return (
    <>
      {/* SEO-critical content rendered server-side */}
      <noscript>
        <div className="p-8 bg-black text-white">
          <h1 className="text-4xl font-bold mb-4">{APP_NAME} — Тюнинг и шумоизоляция в Сургуте</h1>
          <p className="mb-4">{DEFAULT_SITE_DATA.heroSubtitle}</p>
          <h2 className="text-2xl font-bold mb-4">Наши услуги:</h2>
          <ul className="list-disc pl-6 mb-4">
            {SERVICES.map(service => (
              <li key={service.id} className="mb-2">
                <strong>{service.title}</strong> — {service.description} ({service.price})
              </li>
            ))}
          </ul>
          <p>Телефон: {DEFAULT_SITE_DATA.phone}</p>
          <p>Адрес: {DEFAULT_SITE_DATA.address}</p>
          <p>© {new Date().getFullYear()} {APP_NAME}. {OWNER_NAME}</p>
        </div>
      </noscript>
      
      <Suspense fallback={<LoadingScreen />}>
        <ClientPage />
      </Suspense>
    </>
  )
}

function LoadingScreen() {
  return (
    <div className="fixed inset-0 bg-black flex items-center justify-center z-50">
      <div className="text-center">
        <div className="text-4xl font-black uppercase tracking-tighter mb-4">{APP_NAME}</div>
        <div className="w-48 h-1 bg-neutral-800 rounded-full overflow-hidden">
          <div className="h-full bg-white animate-pulse" style={{ width: '60%' }} />
        </div>
      </div>
    </div>
  )
}

'use client'

export default function GlobalError({
  error,
  reset,
}: {
  error: Error & { digest?: string }
  reset: () => void
}) {
  return (
    <html>
      <body className="bg-black">
        <div className="min-h-screen flex items-center justify-center p-4">
          <div className="text-center">
            <h2 className="text-2xl font-bold text-white mb-4">Критическая ошибка</h2>
            <p className="text-neutral-500 mb-8">{error.message}</p>
            <button
              onClick={() => reset()}
              className="px-6 py-3 bg-white text-black rounded-full font-bold uppercase tracking-widest text-sm hover:bg-neutral-200 transition-all"
            >
              Перезагрузить
            </button>
          </div>
        </div>
      </body>
    </html>
  )
}

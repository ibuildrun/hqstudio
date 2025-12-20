'use client'

import { useEffect } from 'react'

export default function Error({
  error,
  reset,
}: {
  error: Error & { digest?: string }
  reset: () => void
}) {
  useEffect(() => {
    console.error(error)
  }, [error])

  return (
    <div className="min-h-screen bg-black flex items-center justify-center p-4">
      <div className="text-center">
        <h2 className="text-2xl font-bold text-white mb-4">Что-то пошло не так</h2>
        <p className="text-neutral-500 mb-8">{error.message}</p>
        <button
          onClick={() => reset()}
          className="px-6 py-3 bg-white text-black rounded-full font-bold uppercase tracking-widest text-sm hover:bg-neutral-200 transition-all"
        >
          Попробовать снова
        </button>
      </div>
    </div>
  )
}

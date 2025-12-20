'use client'

import { useState, useRef } from 'react'

export default function ComparisonSlider() {
  const [position, setPosition] = useState(50)
  const containerRef = useRef<HTMLDivElement>(null)

  const handleMove = (e: React.MouseEvent | React.TouchEvent | any) => {
    if (!containerRef.current) return
    const rect = containerRef.current.getBoundingClientRect()
    
    let x = 0
    if (e.touches && e.touches.length > 0) {
      x = e.touches[0].clientX
    } else if (e.clientX !== undefined) {
      x = e.clientX
    } else {
      return
    }

    const newPos = ((x - rect.left) / rect.width) * 100
    setPosition(Math.min(Math.max(newPos, 0), 100))
  }

  return (
    <section className="py-24 bg-black">
      <div className="container mx-auto px-4">
        <div className="text-center mb-16">
          <h2 className="text-4xl md:text-6xl font-bold uppercase tracking-tighter">Магия преображения</h2>
          <p className="text-neutral-500 mt-4 uppercase text-xs tracking-widest">Тяните за слайдер, чтобы увидеть эффект Антихрома</p>
        </div>

        <div 
          ref={containerRef}
          className="relative aspect-video max-w-5xl mx-auto overflow-hidden border border-white/10 cursor-ew-resize select-none touch-none"
          onMouseMove={(e) => e.buttons === 1 && handleMove(e)}
          onTouchMove={handleMove}
        >
          <div className="absolute inset-0">
            <img 
              src="https://images.unsplash.com/photo-1492144534655-ae79c964c9d7?auto=format&fit=crop&q=80&w=2000&grayscale=false" 
              alt="After" 
              className="w-full h-full object-cover"
            />
            <div className="absolute bottom-6 right-6 bg-white text-black px-4 py-1 text-xs font-bold uppercase tracking-widest">
              Результат
            </div>
          </div>

          <div 
            className="absolute inset-0 overflow-hidden"
            style={{ width: `${position}%` }}
          >
            <img 
              src="https://images.unsplash.com/photo-1492144534655-ae79c964c9d7?auto=format&fit=crop&q=80&w=2000&grayscale=true" 
              alt="Before" 
              className="absolute inset-0 w-[100vw] max-w-none h-full object-cover"
              style={{ width: containerRef.current?.offsetWidth || '100vw' }}
            />
            <div className="absolute bottom-6 left-6 bg-black text-white px-4 py-1 text-xs font-bold uppercase tracking-widest border border-white/20">
              До тюнинга
            </div>
          </div>

          <div 
            className="absolute top-0 bottom-0 w-px bg-white z-20"
            style={{ left: `${position}%` }}
          >
            <div className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 w-10 h-10 border border-white bg-black rounded-full flex items-center justify-center">
              <div className="flex gap-1">
                <div className="w-1 h-1 bg-white rounded-full" />
                <div className="w-1 h-1 bg-white rounded-full" />
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>
  )
}

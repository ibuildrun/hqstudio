'use client'

import { useRef } from 'react'
import { motion, useScroll, useTransform } from 'framer-motion'
import { useAdmin } from '@/lib/store'

export default function StudioGallery() {
  const { data } = useAdmin()
  const containerRef = useRef<HTMLDivElement>(null)
  const { scrollYProgress } = useScroll({
    target: containerRef,
    offset: ["start end", "end start"]
  })

  const scale = useTransform(scrollYProgress, [0, 0.5], [0.8, 1])
  const opacity = useTransform(scrollYProgress, [0, 0.3], [0, 1])
  const clipPath = useTransform(
    scrollYProgress,
    [0, 0.5],
    ["inset(10% 20% 10% 20%)", "inset(0% 0% 0% 0%)"]
  )

  const features = data.galleryFeatures || [
    "Профессиональный свет для контроля качества",
    "Премиальные инструменты из Германии",
    "Зона отдыха для клиентов"
  ]

  return (
    <section ref={containerRef} className="py-24 bg-black overflow-hidden">
      <div className="container mx-auto px-4 mb-16 text-center">
        <span className="text-xs font-bold uppercase tracking-[0.5em] text-neutral-500 mb-4 block">Наше пространство</span>
        <h2 className="text-4xl md:text-7xl font-black uppercase tracking-tighter">{data.galleryTitle}</h2>
      </div>

      <motion.div 
        style={{ scale, opacity, clipPath }}
        className="relative w-full aspect-[21/9] max-w-7xl mx-auto overflow-hidden bg-neutral-900 border border-white/5"
      >
        <img 
          src={data.galleryImage} 
          alt="Studio interior" 
          className="w-full h-full object-cover opacity-60"
        />
        <div className="absolute inset-0 bg-gradient-to-b from-black/60 via-transparent to-black/60" />
        
        <div className="absolute inset-0 flex items-center justify-center">
          <div className="text-center px-4">
             <p className="text-white text-xl md:text-3xl font-light italic tracking-widest uppercase">
               {data.gallerySubtitle}
             </p>
          </div>
        </div>
      </motion.div>
      
      <div className="container mx-auto px-4 mt-12 grid grid-cols-1 md:grid-cols-3 gap-8">
        {features.map((text: string, i: number) => (
          <div key={i} className="border-l border-white/10 pl-6">
            <p className="text-neutral-500 text-xs uppercase tracking-widest leading-relaxed">{text}</p>
          </div>
        ))}
      </div>
    </section>
  )
}

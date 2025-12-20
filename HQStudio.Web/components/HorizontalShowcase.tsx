'use client'

import { useRef } from 'react'
import { motion, useScroll, useTransform } from 'framer-motion'
import { useAdmin } from '@/lib/store'

export default function HorizontalShowcase() {
  const { data } = useAdmin()
  const targetRef = useRef<HTMLDivElement>(null)

  const { scrollYProgress } = useScroll({
    target: targetRef,
    offset: ["start start", "end end"]
  })

  const x = useTransform(scrollYProgress, [0, 1], ["0%", "-75%"])

  return (
    <section ref={targetRef} className="relative h-[300vh] bg-black">
      <div className="sticky top-0 h-screen flex items-center overflow-hidden">
        <motion.div style={{ x }} className="flex gap-4 px-4">
          <div className="flex-shrink-0 w-screen h-[80vh] flex flex-col justify-center px-12">
            <span className="text-white/30 text-xs font-bold uppercase tracking-[0.5em] mb-4">
              {data.showcaseSubtitle || 'Наше портфолио'}
            </span>
            <h2 className="text-6xl md:text-9xl font-black uppercase tracking-tighter text-white whitespace-pre-line">
              {(data.showcaseTitle || 'LOOK\nBOOK').replace(/\\n/g, '\n')}
            </h2>
            <p className="text-neutral-500 mt-8 max-w-sm uppercase text-xs tracking-widest leading-relaxed">
              {data.showcaseDescription || 'Листайте горизонтально, чтобы увидеть результаты нашей работы в деталях.'}
            </p>
          </div>
          
          {data.showcaseItems.map((project, i) => (
            <div key={i} className="flex-shrink-0 w-[85vw] h-[80vh] relative group overflow-hidden border border-white/10">
              <img 
                src={project.img} 
                alt={project.title}
                className="w-full h-full object-cover grayscale transition-all duration-700 group-hover:scale-105 group-hover:grayscale-0"
              />
              <div className="absolute inset-0 bg-gradient-to-t from-black via-transparent to-transparent opacity-80" />
              <div className="absolute bottom-12 left-12">
                <h3 className="text-4xl font-bold uppercase tracking-tight text-white mb-2">{project.title}</h3>
                <p className="text-neutral-400 uppercase text-xs tracking-[0.2em]">{project.desc}</p>
              </div>
            </div>
          ))}
        </motion.div>
      </div>
    </section>
  )
}

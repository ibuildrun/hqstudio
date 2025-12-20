'use client'

import { motion, useScroll, useTransform } from 'framer-motion'
import { Star, ChevronDown } from 'lucide-react'
import { STATS } from '@/lib/constants'
import { useAdmin } from '@/lib/store'

export default function Hero() {
  const { data } = useAdmin()
  const { scrollY } = useScroll()
  const y1 = useTransform(scrollY, [0, 500], [0, 200])
  const y2 = useTransform(scrollY, [0, 500], [0, -150])
  const opacity = useTransform(scrollY, [0, 300], [1, 0])

  const scrollToAbout = (e: React.MouseEvent) => {
    e.preventDefault()
    const element = document.getElementById('about')
    if (element) {
      const offset = 80
      const elementPosition = element.getBoundingClientRect().top + window.pageYOffset
      window.scrollTo({
        top: elementPosition - offset,
        behavior: 'smooth'
      })
      window.history.pushState(null, '', '#about')
    }
  }

  return (
    <section id="home" className="relative h-screen w-full flex items-center justify-center overflow-hidden bg-black">
      {/* Abstract Background Elements */}
      <div className="absolute inset-0 z-0">
        <div className="absolute top-0 left-0 w-full h-full bg-[radial-gradient(circle_at_center,_var(--tw-gradient-stops))] from-neutral-800 via-black to-black opacity-30"></div>
        <motion.div 
          className="absolute inset-0 bg-cover bg-center opacity-50 mix-blend-overlay"
        >
          <img src={data.heroImage} className="w-full h-full object-cover grayscale" alt="Hero background" />
        </motion.div>
      </div>

      <div className="relative z-10 container mx-auto px-4 flex flex-col items-center text-center">
        
        <motion.div
          initial={{ opacity: 0, y: 30 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.2, duration: 0.8 }}
          className="mb-6 flex items-center gap-2 px-4 py-2 border border-white/20 rounded-full bg-black/50 backdrop-blur-sm"
        >
          <div className="flex text-white">
            {[...Array(5)].map((_, i) => (
              <Star key={i} size={14} fill="white" className="text-white" />
            ))}
          </div>
          <span className="text-[10px] font-bold uppercase tracking-wider ml-2">
            {STATS.rating} / {STATS.count} Оценок
          </span>
        </motion.div>

        <motion.h1 
          initial={{ opacity: 0, scale: 0.9 }}
          animate={{ opacity: 1, scale: 1 }}
          transition={{ duration: 1, ease: "easeOut" }}
          className="text-6xl md:text-8xl lg:text-9xl font-extrabold uppercase tracking-tighter leading-none mb-6 mix-blend-difference text-white"
        >
          {data.heroTitle}
        </motion.h1>
        
        <motion.p 
          initial={{ opacity: 0 }}
          animate={{ opacity: 1 }}
          transition={{ delay: 0.5, duration: 1 }}
          className="text-lg md:text-xl text-gray-400 max-w-2xl font-light tracking-wide px-4"
        >
          {data.heroSubtitle}
          <br/>
          <span className="text-white font-medium mt-2 block">Premium Quality. No Compromise.</span>
        </motion.p>

        <motion.button 
          style={{ opacity }}
          onClick={scrollToAbout}
          className="absolute bottom-12 left-1/2 -translate-x-1/2 animate-bounce hover:text-white transition-colors p-4 z-20"
          aria-label="Scroll to About"
        >
           <ChevronDown className="text-white/30 hover:text-white transition-colors" size={40} />
        </motion.button>
      </div>

      <motion.div 
        style={{ y: y2 }}
        className="absolute -bottom-20 -right-20 text-[20rem] font-bold text-white/5 whitespace-nowrap pointer-events-none select-none"
      >
        TUNING
      </motion.div>
    </section>
  )
}

'use client'

import { useState, useEffect } from 'react'
import { motion, AnimatePresence, useScroll, useSpring } from 'framer-motion'
import { Menu, X, Phone } from 'lucide-react'
import { useAdmin } from '@/lib/store'

export default function Navigation() {
  const { data } = useAdmin()
  const [isScrolled, setIsScrolled] = useState(false)
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false)
  const [activeSection, setActiveSection] = useState('home')
  
  const { scrollYProgress } = useScroll()
  const scaleX = useSpring(scrollYProgress, {
    stiffness: 100,
    damping: 30,
    restDelta: 0.001
  })

  useEffect(() => {
    const handleScroll = () => {
      setIsScrolled(window.scrollY > 50)

      const sections = ['home', 'services', 'about', 'contact']
      let current = 'home'

      for (const section of sections) {
        const element = document.getElementById(section)
        if (element) {
          const rect = element.getBoundingClientRect()
          if (rect.top <= 150) {
            current = section
          }
        }
      }
      setActiveSection(current)
    }

    window.addEventListener('scroll', handleScroll)
    handleScroll()
    return () => window.removeEventListener('scroll', handleScroll)
  }, [])

  const navLinks = [
    { name: 'Услуги', href: '#services', id: 'services' },
    { name: 'О нас', href: '#about', id: 'about' },
    { name: 'Контакты', href: '#contact', id: 'contact' },
  ]

  const handleLinkClick = (e: React.MouseEvent<HTMLAnchorElement>, href: string) => {
    e.preventDefault()
    setIsMobileMenuOpen(false)
    
    const targetId = href.replace('#', '')
    const element = document.getElementById(targetId)
    
    if (element) {
      const offset = 80
      const bodyRect = document.body.getBoundingClientRect().top
      const elementRect = element.getBoundingClientRect().top
      const elementPosition = elementRect - bodyRect
      const offsetPosition = elementPosition - offset

      window.scrollTo({
        top: offsetPosition,
        behavior: 'smooth'
      })
      
      window.history.pushState(null, '', href)
    }
  }

  return (
    <>
      <motion.nav
        initial={{ y: -100 }}
        animate={{ y: 0 }}
        transition={{ duration: 0.8, ease: [0.16, 1, 0.3, 1] }}
        className={`fixed top-0 left-0 right-0 z-50 transition-all duration-500 ${
          isScrolled || isMobileMenuOpen 
            ? 'bg-black/80 backdrop-blur-2xl border-b border-white/5 py-3' 
            : 'bg-transparent py-6'
        }`}
      >
        <motion.div 
          className="absolute bottom-0 left-0 h-[2px] bg-white origin-left z-50"
          style={{ scaleX, width: '100%' }}
        />

        <div className="container mx-auto px-4 md:px-8 flex justify-between items-center">
          <a 
            href="#home" 
            onClick={(e) => handleLinkClick(e, '#home')}
            className="flex flex-col group"
          >
            <span className="text-2xl font-black tracking-tighter uppercase leading-none">
              {data.appName}
            </span>
            <span className="text-[7px] font-bold uppercase tracking-[0.5em] text-neutral-500 group-hover:text-white transition-colors">
              Premium Tuning
            </span>
          </a>

          <div className="hidden md:flex items-center gap-12">
            {navLinks.map((link) => (
              <a 
                key={link.name} 
                href={link.href}
                onClick={(e) => handleLinkClick(e, link.href)}
                className={`text-[10px] uppercase tracking-[0.4em] font-bold transition-all relative py-2 ${
                  activeSection === link.id ? 'text-white' : 'text-neutral-500 hover:text-neutral-300'
                }`}
              >
                {link.name}
                {activeSection === link.id && (
                  <motion.span 
                    layoutId="active-dot"
                    className="absolute -bottom-1 left-1/2 -translate-x-1/2 w-1 h-1 bg-white rounded-full"
                  />
                )}
              </a>
            ))}
            <a 
              href={`tel:${data.phone}`} 
              className="group flex items-center gap-3 bg-white text-black px-6 py-3 rounded-full text-[10px] font-bold uppercase tracking-widest hover:bg-neutral-200 transition-all hover:scale-105 active:scale-95"
            >
              <Phone size={12} className="group-hover:rotate-12 transition-transform" />
              <span>Записаться</span>
            </a>
          </div>

          <button 
            className="md:hidden text-white p-2 relative z-50"
            onClick={() => setIsMobileMenuOpen(!isMobileMenuOpen)}
            aria-label="Toggle Menu"
          >
            {isMobileMenuOpen ? <X size={24} /> : <Menu size={24} />}
          </button>
        </div>
      </motion.nav>

      <AnimatePresence>
        {isMobileMenuOpen && (
          <motion.div
            initial={{ opacity: 0, backdropFilter: "blur(0px)" }}
            animate={{ opacity: 1, backdropFilter: "blur(20px)" }}
            exit={{ opacity: 0, backdropFilter: "blur(0px)" }}
            className="fixed inset-0 z-40 bg-black/95 flex flex-col items-center justify-center space-y-8 md:hidden"
          >
            {navLinks.map((link, i) => (
              <motion.a
                key={link.name}
                href={link.href}
                initial={{ y: 20, opacity: 0 }}
                animate={{ y: 0, opacity: 1 }}
                transition={{ delay: i * 0.1 }}
                onClick={(e) => handleLinkClick(e, link.href)}
                className="text-4xl font-black uppercase tracking-tighter hover:text-neutral-400 transition-colors"
              >
                {link.name}
              </motion.a>
            ))}
            <motion.div 
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              transition={{ delay: 0.5 }}
              className="mt-12 pt-12 border-t border-white/10 w-64 text-center"
            >
              <p className="text-neutral-600 text-[10px] uppercase tracking-widest mb-4 italic">Сургут</p>
              <a href={`tel:${data.phone}`} className="text-2xl font-bold tracking-tighter text-white">{data.phone}</a>
            </motion.div>
          </motion.div>
        )}
      </AnimatePresence>
    </>
  )
}

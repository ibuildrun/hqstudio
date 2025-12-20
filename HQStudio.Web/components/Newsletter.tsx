'use client'

import { useState } from 'react'
import { motion, AnimatePresence } from 'framer-motion'
import { Mail, Check, Send, Sparkle } from 'lucide-react'
import { useAdmin } from '@/lib/store'
import { api } from '@/lib/api'

export default function Newsletter() {
  const { addSubscription, data } = useAdmin()
  const [email, setEmail] = useState('')
  const [isSubscribed, setIsSubscribed] = useState(false)
  const [isLoading, setIsLoading] = useState(false)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!email || !email.includes('@')) return
    
    setIsLoading(true)
    
    // Try API first, fallback to local store
    const result = await api.subscriptions.create(email)
    if (result.error) {
      addSubscription(email)
    }
    
    setIsLoading(false)
    setIsSubscribed(true)
    setTimeout(() => {
      setIsSubscribed(false)
      setEmail('')
    }, 5000)
  }

  return (
    <section id="newsletter" className="py-24 bg-black border-t border-white/5 relative overflow-hidden">
      <div className="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 w-[500px] h-[500px] bg-white/5 rounded-full blur-[120px] pointer-events-none" />
      
      <div className="container mx-auto px-4 relative z-10">
        <div className="max-w-3xl mx-auto text-center">
          <motion.div
            initial={{ opacity: 0, scale: 0.9 }}
            whileInView={{ opacity: 1, scale: 1 }}
            className="inline-flex items-center gap-2 mb-6 text-neutral-500"
          >
            <Sparkle size={14} />
            <span className="text-[10px] uppercase tracking-[0.6em] font-bold">{data.newsletterSubtitle || 'Privilege Membership'}</span>
            <Sparkle size={14} />
          </motion.div>
          
          <h2 className="text-4xl md:text-6xl font-black uppercase tracking-tighter mb-8 text-white">
            {(data.newsletterTitle || 'Будьте в курсе привилегий').split(' ').slice(0, 3).join(' ')} <br/>
            <span className="italic font-light opacity-50 lowercase">{(data.newsletterTitle || 'Будьте в курсе привилегий').split(' ').slice(3).join(' ')}</span>
          </h2>
          
          <p className="text-neutral-400 text-sm md:text-base mb-12 max-w-lg mx-auto leading-relaxed italic">
            {data.newsletterDescription || 'Оставьте вашу почту, чтобы первыми получать закрытые предложения, новости о новых услугах и сезонные акции.'}
          </p>

          <AnimatePresence mode="wait">
            {isSubscribed ? (
              <motion.div
                key="success"
                initial={{ opacity: 0, y: 10 }}
                animate={{ opacity: 1, y: 0 }}
                exit={{ opacity: 0, y: -10 }}
                className="flex flex-col items-center py-4"
              >
                <div className="w-16 h-16 bg-white text-black rounded-full flex items-center justify-center mb-4">
                  <Check size={32} />
                </div>
                <h3 className="text-xl font-bold uppercase tracking-widest text-white">Вы в списке!</h3>
                <p className="text-neutral-500 text-[10px] uppercase tracking-widest mt-2">Пришлем вам что-нибудь особенное скоро.</p>
              </motion.div>
            ) : (
              <motion.form
                key="form"
                initial={{ opacity: 0 }}
                animate={{ opacity: 1 }}
                onSubmit={handleSubmit}
                className="relative max-w-md mx-auto"
              >
                <div className="relative group">
                   <Mail className="absolute left-6 top-1/2 -translate-y-1/2 text-neutral-600 group-focus-within:text-white transition-colors" size={18} />
                   <input
                    type="email"
                    required
                    placeholder="ВАШ E-MAIL"
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                    className="w-full bg-neutral-900 border border-white/10 p-6 pl-16 pr-16 rounded-full text-white outline-none focus:border-white/30 transition-all text-[10px] font-bold tracking-[0.2em] uppercase"
                  />
                  <button
                    type="submit"
                    disabled={isLoading}
                    className="absolute right-2 top-1/2 -translate-y-1/2 bg-white text-black p-4 rounded-full hover:bg-neutral-200 transition-all active:scale-90 disabled:opacity-50"
                  >
                    <Send size={18} />
                  </button>
                </div>
                <p className="mt-4 text-[8px] text-neutral-600 uppercase tracking-widest">Никакого спама. Только самое важное.</p>
              </motion.form>
            )}
          </AnimatePresence>
        </div>
      </div>
    </section>
  )
}

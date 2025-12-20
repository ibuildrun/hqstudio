'use client'

import { useState } from 'react'
import { MapPin, Clock, Phone, Mail, CheckCircle, Send, Car, Tag, AlertCircle } from 'lucide-react'
import { WORK_HOURS } from '@/lib/constants'
import { useAdmin } from '@/lib/store'
import { motion, AnimatePresence } from 'framer-motion'
import { api } from '@/lib/api'

const ContactItem: React.FC<{ icon: React.ReactNode; label: string; value: string; href?: string }> = ({ 
  icon, label, value, href 
}) => (
  <div className="flex flex-col gap-2 group">
    <div className="flex items-center gap-2 text-neutral-500 mb-1">
      {icon}
      <span className="text-xs uppercase tracking-widest font-semibold">{label}</span>
    </div>
    {href ? (
      <a href={href} className="text-xl md:text-2xl font-light hover:text-white/70 transition-colors border-b border-transparent hover:border-white/50 w-fit">
        {value}
      </a>
    ) : (
      <p className="text-xl md:text-2xl font-light">{value}</p>
    )}
  </div>
)

interface ContactProps {
  onLogoClick?: () => void
}

export default function Contact({ onLogoClick }: ContactProps) {
  const { addRequest, data } = useAdmin()
  const [formData, setFormData] = useState({ name: '', phone: '', carModel: '', licensePlate: '' })
  const [isSent, setIsSent] = useState(false)
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!formData.name || !formData.phone) return
    
    setIsLoading(true)
    setError(null)
    
    // Try API first, fallback to local store
    const result = await api.callbacks.create({
      name: formData.name,
      phone: formData.phone,
      carModel: formData.carModel || undefined,
      message: formData.licensePlate ? `Госномер: ${formData.licensePlate}` : undefined,
    })
    
    if (result.error) {
      // Fallback to local store if API unavailable
      addRequest(formData)
    }
    
    setFormData({ name: '', phone: '', carModel: '', licensePlate: '' })
    setIsSent(true)
    setIsLoading(false)
    setTimeout(() => setIsSent(false), 3000)
  }

  return (
    <section id="contact" className="bg-neutral-900 text-white py-24 px-4 md:px-8 relative overflow-hidden">
      <div className="absolute inset-0 z-0 opacity-10" 
        style={{ backgroundImage: 'linear-gradient(#333 1px, transparent 1px), linear-gradient(90deg, #333 1px, transparent 1px)', backgroundSize: '40px 40px' }} 
      />

      <div className="container mx-auto relative z-10">
        <div className="flex flex-col lg:flex-row justify-between items-start mb-20 gap-16">
          <div className="max-w-xl">
            <h2 className="text-5xl md:text-7xl font-bold uppercase tracking-tighter mb-4">
              {data.contactTitle || 'Контакты'}
            </h2>
            <p className="text-neutral-400 mb-10 max-w-md">
              {data.contactDescription || 'Мы находимся в Северном промышленном районе. Позвоните нам или приезжайте в гости для консультации.'}
            </p>
            
            <div className="flex flex-col items-start">
              <a href={`tel:${data.phone}`} className="inline-block bg-white text-black px-12 py-5 text-sm font-bold uppercase tracking-widest hover:bg-gray-200 transition-all rounded-full mb-4 shadow-xl">
                Позвонить сейчас
              </a>
              <span className="text-green-500 text-sm flex items-center gap-2">
                <span className="w-2 h-2 rounded-full bg-green-500 animate-pulse"></span>
                Открыто сегодня до 21:00
              </span>
            </div>
          </div>
          
          <div className="w-full lg:max-w-md bg-black border border-white/5 p-8 rounded-3xl shadow-2xl relative overflow-hidden">
            <AnimatePresence mode="wait">
              {isSent ? (
                <motion.div 
                  key="success"
                  initial={{ opacity: 0, scale: 0.9 }}
                  animate={{ opacity: 1, scale: 1 }}
                  exit={{ opacity: 0 }}
                  className="py-10 text-center"
                >
                  <CheckCircle size={48} className="text-green-500 mx-auto mb-4" />
                  <h3 className="text-2xl font-bold uppercase mb-2">Заявка принята</h3>
                  <p className="text-neutral-500 text-xs uppercase tracking-widest">Менеджер свяжется с вами в ближайшее время</p>
                </motion.div>
              ) : (
                <motion.form 
                  key="form"
                  initial={{ opacity: 0 }}
                  animate={{ opacity: 1 }}
                  onSubmit={handleSubmit}
                  className="space-y-4"
                >
                  <div className="text-center mb-6">
                    <h3 className="text-lg font-bold uppercase tracking-widest">{data.contactFormTitle || 'Перезвонить вам?'}</h3>
                    <p className="text-[10px] text-neutral-500 uppercase tracking-widest mt-1 italic">{data.contactFormSubtitle || 'Оставьте данные, и мы свяжемся с вами'}</p>
                  </div>
                  <input 
                    type="text" 
                    placeholder="Ваше имя *" 
                    required
                    value={formData.name}
                    onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                    className="w-full bg-neutral-900 border border-white/10 p-4 rounded-xl text-white outline-none focus:border-white/30 transition-all"
                  />
                  <input 
                    type="tel" 
                    placeholder="Ваш телефон *" 
                    required
                    value={formData.phone}
                    onChange={(e) => setFormData({ ...formData, phone: e.target.value })}
                    className="w-full bg-neutral-900 border border-white/10 p-4 rounded-xl text-white outline-none focus:border-white/30 transition-all"
                  />
                  
                  {data.formConfig.showCarModel && (
                    <div className="relative">
                      <Car size={14} className="absolute left-4 top-1/2 -translate-y-1/2 text-neutral-600" />
                      <input 
                        type="text" 
                        placeholder="Марка и модель авто" 
                        value={formData.carModel}
                        onChange={(e) => setFormData({ ...formData, carModel: e.target.value })}
                        className="w-full bg-neutral-900 border border-white/10 p-4 pl-12 rounded-xl text-white outline-none focus:border-white/30 transition-all text-sm"
                      />
                    </div>
                  )}

                  {data.formConfig.showLicensePlate && (
                    <div className="relative">
                      <Tag size={14} className="absolute left-4 top-1/2 -translate-y-1/2 text-neutral-600" />
                      <input 
                        type="text" 
                        placeholder="Госномер" 
                        value={formData.licensePlate}
                        onChange={(e) => setFormData({ ...formData, licensePlate: e.target.value })}
                        className="w-full bg-neutral-900 border border-white/10 p-4 pl-12 rounded-xl text-white outline-none focus:border-white/30 transition-all text-sm uppercase"
                      />
                    </div>
                  )}

                  <button 
                    type="submit" 
                    disabled={isLoading}
                    className="w-full bg-white text-black py-5 rounded-xl font-bold uppercase tracking-widest text-[10px] flex items-center justify-center gap-2 hover:bg-neutral-200 transition-all mt-4 disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    {isLoading ? 'Отправка...' : 'Отправить'} {!isLoading && <Send size={14} />}
                  </button>
                  <p className="text-center text-[8px] text-neutral-600 uppercase tracking-widest">* Обязательные поля</p>
                </motion.form>
              )}
            </AnimatePresence>
          </div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-12 border-t border-white/10 pt-12">
          <ContactItem icon={<MapPin size={16} />} label="Адрес" value={data.address} href={`https://yandex.ru/maps/?text=${encodeURIComponent(data.address)}`} />
          <ContactItem icon={<Clock size={16} />} label="Режим работы" value={WORK_HOURS} />
          <ContactItem icon={<Phone size={16} />} label="Телефон" value={data.phone} href={`tel:${data.phone}`} />
          <div className="flex flex-col gap-2">
            <div className="flex items-center gap-2 text-neutral-500 mb-1">
              <Mail size={16} />
              <span className="text-xs uppercase tracking-widest font-semibold">Социальные сети</span>
            </div>
            <div className="flex gap-4 mt-2">
              <a href="#" className="w-10 h-10 border border-white/20 rounded-full flex items-center justify-center hover:bg-white hover:text-black transition-all">VK</a>
              <a href="#" className="w-10 h-10 border border-white/20 rounded-full flex items-center justify-center hover:bg-white hover:text-black transition-all">TG</a>
            </div>
          </div>
        </div>

        <div className="mt-24 flex flex-col md:flex-row justify-between items-end">
          <div 
            onClick={onLogoClick}
            className="text-[10vw] md:text-[8vw] font-black text-white/5 leading-none uppercase select-none cursor-pointer hover:text-white/10 transition-colors"
          >
            {data.appName}
          </div>
          <div className="text-neutral-600 text-sm uppercase tracking-wider mt-4 md:mt-0">
            © {new Date().getFullYear()} {data.appName}. {data.ownerName}
          </div>
        </div>
      </div>
    </section>
  )
}

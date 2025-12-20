'use client'

import React, { useRef, useState, useEffect } from 'react'
import { motion, useScroll, useTransform, MotionValue, AnimatePresence } from 'framer-motion'
import { ArrowUpRight, Check, Send, Car, Tag, User, Phone, X } from 'lucide-react'
import { useAdmin } from '@/lib/store' // Путь оставлен ваш
import { ServiceItem } from '@/lib/types'

const ServiceCard: React.FC<{
  service: ServiceItem
  index: number
  total: number
  progress: MotionValue<number>
  onSelect: (service: ServiceItem) => void
}> = ({ service, index, total, progress, onSelect }) => {
  const container = useRef<HTMLDivElement>(null)
  
  const { scrollYProgress: cardScroll } = useScroll({
    target: container,
    offset: ['start end', 'start start']
  })

  // Параметры из оригинала: масштаб 1.5 -> 1
  const imageScale = useTransform(cardScroll, [0, 1], [1.5, 1])
  
  // Расчет из оригинала
  const start = index * (1 / total)
  const targetScale = 1 - (total - index) * 0.04
  const scale = useTransform(progress, [start, 1], [1, targetScale])

  return (
    <div 
      ref={container} 
      className="h-screen flex items-center justify-center sticky top-0 px-4"
    >
      <motion.div
        style={{ 
          scale,
          // Смещение из оригинала: -5vh и шаг 25px
          top: `calc(-5vh + ${index * 25}px)`,
        }}
        className="relative flex flex-col md:flex-row w-full max-w-6xl h-[80vh] md:h-[600px] bg-neutral-900 border border-white/10 overflow-hidden shadow-2xl origin-top rounded-3xl"
      >
        {/* Контент */}
        <div className="w-full md:w-1/2 p-8 md:p-12 flex flex-col justify-between z-10 bg-neutral-900/95 backdrop-blur-sm md:bg-neutral-900">
          <div>
            <span className="text-[10px] font-bold uppercase tracking-[0.4em] text-neutral-500 mb-4 block">
              {String(index + 1).padStart(2, '0')} — {service.category}
            </span>
            <h2 className="text-3xl md:text-5xl font-black uppercase tracking-tight mb-6 leading-none">
              {service.title}
            </h2>
            <p className="text-neutral-400 text-sm md:text-base leading-relaxed mb-8 font-light">
              {service.description}
            </p>
          </div>

          <div className="flex items-end justify-between border-t border-white/5 pt-8">
            <div className="flex flex-col">
              <span className="text-[10px] text-neutral-500 uppercase tracking-widest mb-1">Ориентировочно</span>
              <span className="text-3xl font-black text-white">{service.price}</span>
            </div>
            <button
              onClick={() => onSelect(service)}
              className="group flex items-center gap-3 bg-white text-black px-6 py-4 rounded-full text-[10px] font-bold uppercase tracking-[0.2em] hover:bg-neutral-200 transition-all active:scale-95"
            >
              Записаться
              <ArrowUpRight size={16} className="group-hover:rotate-45 transition-transform duration-300" />
            </button>
          </div>
        </div>

        {/* Изображение */}
        <div className="w-full md:w-1/2 h-full relative overflow-hidden group">
          <div className="absolute inset-0 bg-black/40 z-10 group-hover:bg-transparent transition-colors duration-700" />
          <motion.div style={{ scale: imageScale }} className="w-full h-full">
            <img
              src={service.image}
              alt={service.title}
              className="w-full h-full object-cover grayscale contrast-125 hover:grayscale-0 transition-all duration-1000"
            />
          </motion.div>
        </div>
      </motion.div>
    </div>
  )
}

const Services: React.FC = () => {
  const { data, addRequest } = useAdmin()
  const container = useRef<HTMLDivElement>(null)
  
  const { scrollYProgress } = useScroll({
    target: container,
    offset: ['start start', 'end end']
  })

  const [selectedService, setSelectedService] = useState<ServiceItem | null>(null)
  const [formData, setFormData] = useState({ name: '', phone: '', carModel: '', licensePlate: '' })
  const [isSent, setIsSent] = useState(false)

  useEffect(() => {
    if (selectedService) {
      document.body.style.overflow = 'hidden'
    } else {
      document.body.style.overflow = 'unset'
    }
  }, [selectedService])

  const handleBookingSubmit = (e: React.FormEvent) => {
    e.preventDefault()
    if (!formData.name || !formData.phone || !selectedService) return

    addRequest({
      ...formData,
      service: selectedService.title
    })

    setIsSent(true)
    setTimeout(() => {
      setIsSent(false)
      setSelectedService(null)
      setFormData({ name: '', phone: '', carModel: '', licensePlate: '' })
    }, 3000)
  }

  return (
    <section 
      id="services" 
      ref={container}
      className="bg-black relative"
    >
      {/* Заголовок секции - Текстовка и отступы из оригинала */}
      <div className="pt-32 pb-12 px-4 text-center">
        <motion.span
          initial={{ opacity: 0 }}
          whileInView={{ opacity: 1 }}
          className="text-xs font-bold uppercase tracking-[0.6em] text-neutral-600 mb-4 block"
        >
          Premium Solutions
        </motion.span>
        <h2 className="text-5xl md:text-8xl font-black uppercase tracking-tighter mb-6">
          Наши Услуги
        </h2>
        <p className="text-neutral-500 max-w-xl mx-auto uppercase text-[10px] tracking-[0.3em] font-light leading-relaxed">
          Комплексный подход к эстетике и комфорту вашего автомобиля. <br /> 
          Только проверенные временем технологии.
        </p>
      </div>

      {/* Контейнер с карточками */}
      <div className="pb-32">
        {data.services.map((service, index) => (
          <ServiceCard
            key={service.id}
            service={service}
            index={index}
            total={data.services.length}
            progress={scrollYProgress}
            onSelect={(s) => setSelectedService(s)}
          />
        ))}
      </div>

      {/* Модальное окно - Стили и текстовка из оригинала */}
      <AnimatePresence>
        {selectedService && (
          <motion.div
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            className="fixed inset-0 z-[100] bg-black/95 backdrop-blur-xl flex items-center justify-center p-4 md:p-8"
          >
            <motion.div
              initial={{ scale: 0.9, y: 20 }}
              animate={{ scale: 1, y: 0 }}
              exit={{ scale: 0.9, y: 20 }}
              className="w-full max-w-2xl bg-neutral-900 border border-white/10 rounded-3xl overflow-hidden shadow-2xl"
            >
              <div className="p-6 border-b border-white/5 flex justify-between items-center bg-black/50">
                <div className="flex flex-col">
                  <h3 className="text-lg font-black uppercase tracking-tight">Запись на сервис</h3>
                  <span className="text-[10px] uppercase tracking-widest text-neutral-500">
                    Услуга: {selectedService.title}
                  </span>
                </div>
                <button
                  onClick={() => setSelectedService(null)}
                  className="p-3 bg-neutral-800 rounded-full hover:bg-white hover:text-black transition-all"
                >
                  <X size={20} />
                </button>
              </div>

              <div className="p-8 md:p-12">
                {isSent ? (
                  <div className="text-center py-10">
                    <div className="w-20 h-20 bg-green-500 text-white rounded-full flex items-center justify-center mx-auto mb-6 shadow-[0_0_40px_rgba(34,197,94,0.3)]">
                      <Check size={40} />
                    </div>
                    <h4 className="text-3xl font-black uppercase tracking-tight mb-2">Отправлено!</h4>
                    <p className="text-neutral-500 uppercase tracking-widest text-[10px]">
                      Ваша заявка на «{selectedService.title}» в очереди на обработку.
                    </p>
                  </div>
                ) : (
                  <form onSubmit={handleBookingSubmit} className="space-y-6">
                    <div className="grid md:grid-cols-2 gap-6">
                      <div className="relative border-b border-white/10 focus-within:border-white transition-colors">
                        <User size={14} className="absolute left-0 top-1/2 -translate-y-1/2 text-neutral-500" />
                        <input
                          type="text"
                          required
                          placeholder="ВАШЕ ИМЯ"
                          value={formData.name}
                          onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                          className="w-full py-4 pl-8 bg-transparent outline-none text-xs font-bold uppercase tracking-widest text-white placeholder:text-neutral-700"
                        />
                      </div>
                      <div className="relative border-b border-white/10 focus-within:border-white transition-colors">
                        <Phone size={14} className="absolute left-0 top-1/2 -translate-y-1/2 text-neutral-500" />
                        <input
                          type="tel"
                          required
                          placeholder="ТЕЛЕФОН"
                          value={formData.phone}
                          onChange={(e) => setFormData({ ...formData, phone: e.target.value })}
                          className="w-full py-4 pl-8 bg-transparent outline-none text-xs font-bold uppercase tracking-widest text-white placeholder:text-neutral-700"
                        />
                      </div>
                    </div>

                    <div className="grid md:grid-cols-2 gap-6">
                      {data.formConfig.showCarModel && (
                        <div className="relative border-b border-white/10 focus-within:border-white transition-colors">
                          <Car size={14} className="absolute left-0 top-1/2 -translate-y-1/2 text-neutral-500" />
                          <input
                            type="text"
                            placeholder="МАРКА / МОДЕЛЬ"
                            value={formData.carModel}
                            onChange={(e) => setFormData({ ...formData, carModel: e.target.value })}
                            className="w-full py-4 pl-8 bg-transparent outline-none text-xs font-bold uppercase tracking-widest text-white placeholder:text-neutral-700"
                          />
                        </div>
                      )}

                      {data.formConfig.showLicensePlate && (
                        <div className="relative border-b border-white/10 focus-within:border-white transition-colors">
                          <Tag size={14} className="absolute left-0 top-1/2 -translate-y-1/2 text-neutral-500" />
                          <input
                            type="text"
                            placeholder="ГОСНОМЕР"
                            value={formData.licensePlate}
                            onChange={(e) => setFormData({ ...formData, licensePlate: e.target.value })}
                            className="w-full py-4 pl-8 bg-transparent outline-none text-xs font-bold uppercase tracking-widest text-white placeholder:text-neutral-700"
                          />
                        </div>
                      )}
                    </div>

                    <button
                      type="submit"
                      className="w-full bg-white text-black py-6 text-[10px] font-bold uppercase tracking-[0.4em] hover:bg-neutral-200 transition-all flex items-center justify-center gap-4 mt-8 rounded-xl shadow-xl active:scale-95"
                    >
                      Подтвердить запись <Send size={14} />
                    </button>
                  </form>
                )}
              </div>
            </motion.div>
          </motion.div>
        )}
      </AnimatePresence>
    </section>
  )
}

export default Services
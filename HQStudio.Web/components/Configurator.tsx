'use client'

import { useState } from 'react'
import { motion, AnimatePresence } from 'framer-motion'
import { ChevronRight, ChevronLeft, Check, Sparkles } from 'lucide-react'

const STEPS = [
  {
    id: 'type',
    title: 'Ваш автомобиль',
    options: ['Седан / Купе', 'Кроссовер / SUV', 'Минивэн / Премиум']
  },
  {
    id: 'goal',
    title: 'Что для вас важно?',
    options: ['Абсолютная тишина', 'Эстетика и стайлинг', 'Комфорт и удобство']
  },
  {
    id: 'services',
    title: 'Выберите услуги',
    options: ['Шумоизоляция', 'Антихром', 'Доводчики', 'Подсветка Ambient']
  }
]

export default function Configurator() {
  const [currentStep, setCurrentStep] = useState(0)
  const [selections, setSelections] = useState<Record<string, string | string[]>>({})
  const [isFinished, setIsFinished] = useState(false)

  const handleNext = () => {
    if (currentStep < STEPS.length - 1) {
      setCurrentStep(currentStep + 1)
    } else {
      setIsFinished(true)
    }
  }

  const handleBack = () => {
    if (currentStep > 0) setCurrentStep(currentStep - 1)
  }

  const toggleOption = (option: string) => {
    const stepId = STEPS[currentStep].id
    if (stepId === 'services') {
      const current = (selections[stepId] as string[]) || []
      const updated = current.includes(option) 
        ? current.filter(o => o !== option) 
        : [...current, option]
      setSelections({ ...selections, [stepId]: updated })
    } else {
      setSelections({ ...selections, [stepId]: option })
      setTimeout(handleNext, 400)
    }
  }

  return (
    <section className="py-24 bg-neutral-50 text-black">
      <div className="container mx-auto px-4 max-w-4xl">
        <div className="text-center mb-16">
          <h2 className="text-4xl md:text-6xl font-black uppercase tracking-tighter mb-4">Консьерж-сервис</h2>
          <p className="text-neutral-500 uppercase text-xs tracking-[0.3em]">Создайте свой идеальный пакет комфорта</p>
        </div>

        <div className="bg-white border border-neutral-200 p-8 md:p-16 min-h-[500px] flex flex-col justify-between shadow-sm relative overflow-hidden">
          <AnimatePresence mode="wait">
            {!isFinished ? (
              <motion.div
                key={currentStep}
                initial={{ opacity: 0, x: 20 }}
                animate={{ opacity: 1, x: 0 }}
                exit={{ opacity: 0, x: -20 }}
                className="flex-1"
              >
                <div className="flex justify-between items-center mb-12">
                  <span className="text-xs font-bold text-neutral-300 uppercase tracking-widest">Шаг {currentStep + 1} / {STEPS.length}</span>
                  <div className="flex gap-1">
                    {STEPS.map((_, i) => (
                      <div key={i} className={`w-8 h-1 transition-colors ${i <= currentStep ? 'bg-black' : 'bg-neutral-100'}`} />
                    ))}
                  </div>
                </div>

                <h3 className="text-3xl font-light mb-12">{STEPS[currentStep].title}</h3>

                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  {STEPS[currentStep].options.map((option) => {
                    const isSelected = Array.isArray(selections[STEPS[currentStep].id]) 
                      ? (selections[STEPS[currentStep].id] as string[]).includes(option)
                      : selections[STEPS[currentStep].id] === option
                    
                    return (
                      <button
                        key={option}
                        onClick={() => toggleOption(option)}
                        className={`group p-6 text-left border transition-all duration-300 flex justify-between items-center ${
                          isSelected ? 'border-black bg-black text-white' : 'border-neutral-200 hover:border-black'
                        }`}
                      >
                        <span className="text-sm font-bold uppercase tracking-widest">{option}</span>
                        {isSelected && <Check size={18} />}
                      </button>
                    )
                  })}
                </div>
              </motion.div>
            ) : (
              <motion.div
                initial={{ opacity: 0, scale: 0.9 }}
                animate={{ opacity: 1, scale: 1 }}
                className="text-center py-12"
              >
                <div className="w-20 h-20 bg-black text-white rounded-full flex items-center justify-center mx-auto mb-8">
                  <Sparkles size={32} />
                </div>
                <h3 className="text-3xl font-bold uppercase mb-4">Ваш выбор готов</h3>
                <p className="text-neutral-500 mb-12 max-w-md mx-auto italic">
                  Наши специалисты подготовят персональное предложение на основе ваших предпочтений.
                </p>
                <button className="px-12 py-5 bg-black text-white font-bold uppercase tracking-[0.2em] hover:bg-neutral-800 transition-colors">
                  Отправить запрос эксперту
                </button>
              </motion.div>
            )}
          </AnimatePresence>

          {!isFinished && (
            <div className="flex justify-between items-center mt-12 pt-8 border-t border-neutral-100">
              <button 
                onClick={handleBack}
                disabled={currentStep === 0}
                className={`flex items-center gap-2 text-xs font-bold uppercase tracking-widest ${currentStep === 0 ? 'opacity-0' : 'opacity-100'}`}
              >
                <ChevronLeft size={16} /> Назад
              </button>
              {STEPS[currentStep].id === 'services' && (
                <button 
                  onClick={handleNext}
                  className="flex items-center gap-2 bg-black text-white px-8 py-3 text-xs font-bold uppercase tracking-widest"
                >
                  Далее <ChevronRight size={16} />
                </button>
              )}
            </div>
          )}
        </div>
      </div>
    </section>
  )
}

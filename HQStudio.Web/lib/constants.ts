import { ServiceItem, ReviewStats } from './types'

export const APP_NAME = "Hq_studio"
export const OWNER_NAME = "Игонин Павел Васильевич"
export const PHONE = "+7 929 293-52-22"
export const CLEAN_PHONE = "79292935222"
export const ADDRESS = "Нефтеюганское шоссе, 22 ст1, Сургут"
export const FULL_ADDRESS = "Северный промышленный район, Сургут, Ханты-Мансийский автономный округ — Югра, 628415"
export const WORK_HOURS = "Ежедневно с 09:00 до 21:00"

export const STATS: ReviewStats = {
  rating: 5.0,
  count: 45
}

export const SERVICES: ServiceItem[] = [
  {
    id: "closers",
    title: "Доводчики дверей",
    category: "Комфорт",
    description: "Осуществляем продажу и установку. Система автоматических доводчиков позволяет без дополнительных усилий закрывать двери – при неполном закрытии их автоматически дотянет механизм. Решение премиум-сегмента теперь доступно практически для любого авто. Без конструктивных изменений и потери гарантии.",
    price: "от 15 000 ₽",
    image: "https://picsum.photos/800/600?grayscale&random=1"
  },
  {
    id: "sound",
    title: "Шумоизоляция",
    category: "Тишина",
    description: "Профессиональная шумоизоляция автомобиля. Полный комплекс работ: шумоизоляция колёсных арок снаружи, дверей, крыши, пола и багажного отделения. Максимальный акустический комфорт.",
    price: "от 15 000 ₽",
    image: "https://picsum.photos/800/600?grayscale&random=2"
  },
  {
    id: "antichrome",
    title: "Антихром",
    category: "Стиль",
    description: "Предлагаем антихром на авто методом качественной обтяжки виниловой пленкой, а также профессиональный окрас с предварительным травлением хрома. Создайте строгий и агрессивный облик вашего автомобиля.",
    price: "от 4 000 ₽",
    image: "https://picsum.photos/800/600?grayscale&random=3"
  },
  {
    id: "ambient",
    title: "Контурная подсветка",
    category: "Атмосфера",
    description: "Осуществляем продажу и установку. Ambient light — это способ выделить свой автомобиль, подчеркнуть статус и улучшить внутреннюю атмосферу. Мягкое свечение по линиям интерьера добавляет ощущение уюта и элегантности.",
    price: "от 16 000 ₽",
    image: "https://picsum.photos/800/600?grayscale&random=4"
  },
  {
    id: "parts",
    title: "Комплектующие",
    category: "Продажа",
    description: "Продажа комплектов контурной подсветки Ambient light. В наличии черная и белая версии для самостоятельной установки или сервисов.",
    price: "от 6 000 ₽",
    image: "https://picsum.photos/800/600?grayscale&random=5"
  }
]

export const DEFAULT_SITE_DATA = {
  appName: APP_NAME,
  ownerName: OWNER_NAME,
  phone: PHONE,
  address: ADDRESS,
  services: SERVICES,
  heroImage: 'https://images.unsplash.com/photo-1503376780353-7e6692767b70?auto=format&fit=crop&q=80&w=2000&grayscale=true',
  heroTitle: APP_NAME,
  heroSubtitle: "Услуги профессиональной шумоизоляции, автодоводчики и стайлинг вашего автомобиля.",
  aboutTickerText: "ШУМОИЗОЛЯЦИЯ • АНТИХРОМ • АВТОСВЕТ • ДОВОДЧИКИ",
  aboutTickerDisclaimer: "Игонин Павел Васильевич. Информация на сайте не является публичной офертой. 2025 г.",
  aboutManifestSubtitle: "Искусство детализации",
  aboutManifestTitle: "Ваш комфорт — наша работа",
  aboutManifestText: "Мы не просто устанавливаем оборудование, мы создаем новую атмосферу внутри вашего автомобиля. Для аудитории, которая ценит тишину, эстетику и безупречное качество исполнения.",
  aboutQualityTitle: "Забота о ваших близких",
  aboutQualityDesc: "Мы понимаем, что автомобиль — это продолжение вашего дома. Поэтому мы используем только материалы премиум-класса, которые прошли строгий европейский контроль качества.",
  faqItems: [
    { q: "Сохранится ли дилерская гарантия?", a: "Да. Мы работаем согласно техническим регламентам, не вносим изменений в конструкцию электронных блоков." },
    { q: "Как долго длится процесс шумоизоляции?", a: "Полный комплекс занимает от 2 до 3 рабочих дней." }
  ],
  processSteps: [
    { num: "01", title: "Консультация", text: "Встреча в нашей студии за чашкой свежего кофе. Обсуждаем пожелания и бюджет." },
    { num: "02", title: "Приемка", text: "Тщательный осмотр ЛКП и интерьера при ярком студийном свете." },
    { num: "03", title: "Преображение", text: "Мастера приступают к работе, используя премиальные инструменты." },
    { num: "04", title: "Результат", text: "Финальное тестирование всех систем и демонстрация результата." }
  ],
  showcaseItems: [
    { title: "BMW M4 Shadow", desc: "Полный антихром и Ambient Light", img: "https://picsum.photos/1200/800?grayscale&random=11" },
    { title: "Mercedes S-Class", desc: "Премиальная шумоизоляция 'Elite'", img: "https://picsum.photos/1200/800?grayscale&random=12" },
    { title: "Audi RS6 Avant", desc: "Комплексная шумоизоляция и доводчики", img: "https://picsum.photos/1200/800?grayscale&random=13" },
    { title: "Porsche Cayenne", desc: "Ambient Light 64 цвета", img: "https://picsum.photos/1200/800?grayscale&random=14" }
  ],
  showcaseSubtitle: "Наше портфолио",
  showcaseTitle: "LOOK\\nBOOK",
  showcaseDescription: "Листайте горизонтально, чтобы увидеть результаты нашей работы в деталях.",
  galleryImage: "https://images.unsplash.com/photo-1619642751034-765dfdf7c58e?auto=format&fit=crop&q=80&w=2000&grayscale=true",
  galleryTitle: "Место, где рождается тишина",
  gallerySubtitle: "Стерильность. Точность. Эстетика.",
  galleryFeatures: [
    "Профессиональный свет для контроля качества",
    "Премиальные инструменты из Германии",
    "Зона отдыха для клиентов"
  ],
  soundExpNoisyTitle: "«Утомляющий гул дорожного хаоса»",
  soundExpNoisyDesc: "Заводская защита часто пропускает до 85% шумов.",
  soundExpQuietTitle: "«Тишина, в которой слышны мысли»",
  soundExpQuietDesc: "После нашей обработки уровень шума падает до минимума.",
  // Testimonials
  testimonialsTitle: "Истории наших клиентов",
  testimonialsSubtitle: "Клиентский опыт",
  testimonialsRating: "4.9",
  testimonialsCount: "45+",
  testimonials: [
    { name: "Марина", car: "Audi Q7", text: "HQ_Studio превратили мою машину в настоящий оазис. Теперь поездки с детьми — это удовольствие, в салоне идеальная тишина даже на трассе." },
    { name: "Александр", car: "Range Rover", text: "Делал антихром и доводчики. Качество исполнения на высоте — ни одного зазора, всё работает как заводское. Рекомендую тем, кто ценит детали." },
    { name: "Екатерина", car: "Porsche Macan", text: "Контурная подсветка просто преобразила интерьер! Вечером в машине невероятно уютная атмосфера. Очень деликатная и качественная работа." }
  ],
  // FAQ
  faqTitle: "Вопросы и ответы",
  faqSubtitle: "База знаний",
  faqDescription: "Мы за прозрачность в работе. Если у вас остались сомнения — мы развеем их здесь.",
  // Newsletter
  newsletterTitle: "Будьте в курсе привилегий",
  newsletterSubtitle: "Privilege Membership",
  newsletterDescription: "Оставьте вашу почту, чтобы первыми получать закрытые предложения, новости о новых услугах и сезонные акции.",
  // Contact
  contactTitle: "Контакты",
  contactDescription: "Мы находимся в Северном промышленном районе. Позвоните нам или приезжайте в гости для консультации.",
  contactFormTitle: "Перезвонить вам?",
  contactFormSubtitle: "Оставьте данные, и мы свяжемся с вами",
  // MoodLight
  moodlightTitle: "Управляйте атмосферой",
  moodlightSubtitle: "Освещение как искусство",
  moodlightModes: [
    { id: 'zen', name: 'Zen Mode', color: 'rgba(255, 140, 0, 0.4)', desc: 'Теплый уют для вечерних поездок' },
    { id: 'arctic', name: 'Arctic Blue', color: 'rgba(0, 191, 255, 0.4)', desc: 'Свежесть и концентрация за рулем' },
    { id: 'royal', name: 'Royal Violet', color: 'rgba(138, 43, 226, 0.4)', desc: 'Элегантность и статус вашего интерьера' }
  ],
  moodlightImage: "https://images.unsplash.com/photo-1542362567-b055002b91f4?auto=format&fit=crop&q=80&w=1200&grayscale=true",
  // Configurator
  configuratorTitle: "Консьерж-сервис",
  configuratorSubtitle: "Создайте свой идеальный пакет комфорта",
  configuratorSteps: [
    { id: 'type', title: 'Ваш автомобиль', options: ['Седан / Купе', 'Кроссовер / SUV', 'Минивэн / Премиум'] },
    { id: 'goal', title: 'Что для вас важно?', options: ['Абсолютная тишина', 'Эстетика и стайлинг', 'Комфорт и удобство'] },
    { id: 'services', title: 'Выберите услуги', options: ['Шумоизоляция', 'Антихром', 'Доводчики', 'Подсветка Ambient'] }
  ],
  // PromoGame
  gameTitle: "Aesthetic Rewards",
  gameSubtitle: "Privilege Program",
  gameDescription: "Персональные привилегии для тех, кто ценит безупречный стиль и премиальный комфорт.",
  gameButtonText: "Испытать удачу",
  
  // Block visibility and order
  siteBlocks: [
    { id: 'hero', name: 'Главный экран', enabled: true },
    { id: 'services', name: 'Услуги', enabled: true },
    { id: 'ticker', name: 'Бегущая строка', enabled: true },
    { id: 'manifest', name: 'Манифест', enabled: true },
    { id: 'quality', name: 'Качество материалов', enabled: true },
    { id: 'sound', name: 'Сравнение звука', enabled: true },
    { id: 'process', name: 'Процесс работы', enabled: true },
    { id: 'gallery', name: 'Галерея студии', enabled: true },
    { id: 'moodlight', name: 'Ambient Light', enabled: true },
    { id: 'configurator', name: 'Конфигуратор', enabled: true },
    { id: 'showcase', name: 'Портфолио', enabled: true },
    { id: 'testimonials', name: 'Отзывы', enabled: true },
    { id: 'game', name: 'Промо-игра', enabled: true },
    { id: 'faq', name: 'FAQ', enabled: true },
    { id: 'newsletter', name: 'Рассылка', enabled: true },
    { id: 'contact', name: 'Контакты', enabled: true },
  ],
  
  formConfig: { showCarModel: true, showLicensePlate: true }
}

export interface ServiceItem {
  id: string
  title: string
  description: string
  price: string
  category: string
  image: string
}

export interface ReviewStats {
  rating: number
  count: number
}

export interface FAQItem {
  q: string
  a: string
}

export interface ProcessStep {
  num: string
  title: string
  text: string
}

export interface ShowcaseItem {
  title: string
  desc: string
  img: string
}

export interface CallbackRequest {
  id: string
  name: string
  phone: string
  carModel?: string
  licensePlate?: string
  service?: string
  timestamp: number
  status: 'new' | 'completed'
}

export interface Subscription {
  id: string
  email: string
  timestamp: number
}

export interface Testimonial {
  name: string
  car: string
  text: string
}

export interface MoodlightMode {
  id: string
  name: string
  color: string
  desc: string
}

export interface ConfiguratorStep {
  id: string
  title: string
  options: string[]
}

export interface SiteBlock {
  id: string
  name: string
  enabled: boolean
}

export interface SiteData {
  appName: string
  ownerName: string
  phone: string
  address: string
  services: ServiceItem[]
  heroImage: string
  heroTitle: string
  heroSubtitle: string
  aboutTickerText: string
  aboutTickerDisclaimer: string
  aboutManifestSubtitle: string
  aboutManifestTitle: string
  aboutManifestText: string
  aboutQualityTitle: string
  aboutQualityDesc: string
  faqItems: FAQItem[]
  processSteps: ProcessStep[]
  showcaseItems: ShowcaseItem[]
  showcaseSubtitle: string
  showcaseTitle: string
  showcaseDescription: string
  galleryImage: string
  galleryTitle: string
  gallerySubtitle: string
  galleryFeatures: string[]
  soundExpNoisyTitle: string
  soundExpNoisyDesc: string
  soundExpQuietTitle: string
  soundExpQuietDesc: string
  // Testimonials
  testimonialsTitle: string
  testimonialsSubtitle: string
  testimonialsRating: string
  testimonialsCount: string
  testimonials: Testimonial[]
  // FAQ
  faqTitle: string
  faqSubtitle: string
  faqDescription: string
  // Newsletter
  newsletterTitle: string
  newsletterSubtitle: string
  newsletterDescription: string
  // Contact
  contactTitle: string
  contactDescription: string
  contactFormTitle: string
  contactFormSubtitle: string
  // MoodLight
  moodlightTitle: string
  moodlightSubtitle: string
  moodlightModes: MoodlightMode[]
  moodlightImage: string
  // Configurator
  configuratorTitle: string
  configuratorSubtitle: string
  configuratorSteps: ConfiguratorStep[]
  // PromoGame
  gameTitle: string
  gameSubtitle: string
  gameDescription: string
  gameButtonText: string
  // Block management
  siteBlocks: SiteBlock[]
  formConfig: {
    showCarModel: boolean
    showLicensePlate: boolean
  }
}

/**
 * Тесты для lucide-react иконок
 * 
 * Этот файл проверяет, что все используемые в проекте иконки
 * корректно импортируются из lucide-react.
 * 
 * Запустите тесты ДО и ПОСЛЕ обновления lucide-react:
 * - До: npm test -- --run (baseline)
 * - После: npm install lucide-react@0.562.0 && npm test -- --run
 */

import { describe, it, expect } from 'vitest'

// Все иконки, используемые в проекте HQStudio.Web
import {
  // ClientPage.tsx
  EyeOff,
  // Configurator.tsx
  ChevronRight, ChevronLeft, Check, Sparkles, Loader2,
  // AdminPanel.tsx
  Settings, Save, X, Eye, Trash2, Plus, Edit2, Image, Type, Palette,
  ChevronUp, ChevronDown, LayoutGrid, GripVertical, Minimize2, Maximize2,
  ChevronsLeft, ChevronsRight, Bell,
  // Contact.tsx
  MapPin, Clock, Phone, Mail, CheckCircle, Send, Car, Tag, AlertCircle,
  // CallbacksPanel.tsx
  XCircle, Search, Calendar, RefreshCw, LogIn, Inbox,
  // FAQ.tsx
  Minus,
  // NotificationSettings.tsx
  BellOff, Smartphone, Shield, Package, MessageSquare,
  // Hero.tsx
  Star,
  // SectionPreview.tsx
  Monitor, Tablet, RotateCcw,
  // MaterialQuality.tsx
  ShieldCheck, Leaf, Wind,
  // MoodLightExperience.tsx
  Moon, Zap,
  // Navigation.tsx
  Menu,
  // Newsletter.tsx
  Sparkle,
  // PromoGame.tsx
  Gift, RefreshCcw, CheckCircle2,
  // Services.tsx
  ArrowUpRight, User,
  // SoundExperience.tsx
  Volume2, VolumeX,
  // Testimonials.tsx
  Quote
} from 'lucide-react'

describe('lucide-react Icons Import', () => {
  it('all icons are valid React components', () => {
    const icons = [
      EyeOff, ChevronRight, ChevronLeft, Check, Sparkles, Loader2,
      Settings, Save, X, Eye, Trash2, Plus, Edit2, Image, Type, Palette,
      ChevronUp, ChevronDown, LayoutGrid, GripVertical, Minimize2, Maximize2,
      ChevronsLeft, ChevronsRight, Bell, MapPin, Clock, Phone, Mail,
      CheckCircle, Send, Car, Tag, AlertCircle, XCircle, Search, Calendar,
      RefreshCw, LogIn, Inbox, Minus, BellOff, Smartphone, Shield, Package,
      MessageSquare, Star, Monitor, Tablet, RotateCcw, ShieldCheck, Leaf,
      Wind, Moon, Zap, Menu, Sparkle, Gift, RefreshCcw, CheckCircle2,
      ArrowUpRight, User, Volume2, VolumeX, Quote
    ]

    icons.forEach(icon => {
      expect(icon).toBeDefined()
      expect(typeof icon).toBe('object') // React.forwardRef returns object
    })
  })

  it('icons have displayName property', () => {
    // Проверяем несколько ключевых иконок
    const sampleIcons = [Star, Check, Phone, Mail, Settings, Menu]
    
    sampleIcons.forEach(icon => {
      expect(icon).toHaveProperty('displayName')
    })
  })
})

describe('lucide-react Icons Count', () => {
  it('project uses expected number of unique icons', () => {
    const uniqueIcons = new Set([
      'EyeOff', 'ChevronRight', 'ChevronLeft', 'Check', 'Sparkles', 'Loader2',
      'Settings', 'Save', 'X', 'Eye', 'Trash2', 'Plus', 'Edit2', 'Image', 'Type', 'Palette',
      'ChevronUp', 'ChevronDown', 'LayoutGrid', 'GripVertical', 'Minimize2', 'Maximize2',
      'ChevronsLeft', 'ChevronsRight', 'Bell', 'MapPin', 'Clock', 'Phone', 'Mail',
      'CheckCircle', 'Send', 'Car', 'Tag', 'AlertCircle', 'XCircle', 'Search', 'Calendar',
      'RefreshCw', 'LogIn', 'Inbox', 'Minus', 'BellOff', 'Smartphone', 'Shield', 'Package',
      'MessageSquare', 'Star', 'Monitor', 'Tablet', 'RotateCcw', 'ShieldCheck', 'Leaf',
      'Wind', 'Moon', 'Zap', 'Menu', 'Sparkle', 'Gift', 'RefreshCcw', 'CheckCircle2',
      'ArrowUpRight', 'User', 'Volume2', 'VolumeX', 'Quote'
    ])
    
    // Проект использует ~60 уникальных иконок
    expect(uniqueIcons.size).toBeGreaterThanOrEqual(55)
    expect(uniqueIcons.size).toBeLessThanOrEqual(70)
  })
})

describe('lucide-react Version Compatibility', () => {
  it('icons are valid forwardRef components', () => {
    // lucide-react иконки — это forwardRef компоненты
    const testIcons = [Star, Check, Phone, Menu, Settings]
    
    testIcons.forEach(Icon => {
      // forwardRef компоненты имеют $$typeof Symbol
      expect(Icon).toHaveProperty('$$typeof')
      // И render функцию (используем 'in' для проверки без TS ошибки)
      expect('render' in Icon).toBe(true)
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      expect(typeof (Icon as any).render).toBe('function')
    })
  })

  it('icons have correct displayName', () => {
    // Проверяем структуру иконки Star как образец
    expect(Star).toHaveProperty('displayName')
    expect(Star.displayName).toBe('Star')
    
    expect(Check).toHaveProperty('displayName')
    expect(Check.displayName).toBe('Check')
    
    expect(Phone).toHaveProperty('displayName')
    expect(Phone.displayName).toBe('Phone')
  })
})

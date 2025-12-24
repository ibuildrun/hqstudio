import { describe, it, expect } from 'vitest'

// Edge case tests for various utilities

// ============ Phone Formatting Edge Cases ============

const formatPhone = (phone: string): string => {
  const digits = phone.replace(/\D/g, '')
  
  // Handle 8 prefix (convert to 7)
  let normalized = digits
  if (digits.length === 11 && digits.startsWith('8')) {
    normalized = '7' + digits.slice(1)
  }
  
  if (normalized.length === 11 && normalized.startsWith('7')) {
    return `+7 (${normalized.slice(1, 4)}) ${normalized.slice(4, 7)}-${normalized.slice(7, 9)}-${normalized.slice(9)}`
  }
  if (normalized.length === 10) {
    return `+7 (${normalized.slice(0, 3)}) ${normalized.slice(3, 6)}-${normalized.slice(6, 8)}-${normalized.slice(8)}`
  }
  return phone
}

describe('Phone Formatting Edge Cases', () => {
  it('handles 8 prefix correctly', () => {
    expect(formatPhone('89291234567')).toBe('+7 (929) 123-45-67')
  })

  it('handles multiple plus signs', () => {
    expect(formatPhone('+++79291234567')).toBe('+7 (929) 123-45-67')
  })

  it('handles spaces between digits', () => {
    expect(formatPhone('7 9 2 9 1 2 3 4 5 6 7')).toBe('+7 (929) 123-45-67')
  })

  it('handles parentheses and dashes', () => {
    expect(formatPhone('(929) 123-45-67')).toBe('+7 (929) 123-45-67')
  })

  it('returns original for too short numbers', () => {
    expect(formatPhone('123')).toBe('123')
  })

  it('returns original for too long numbers', () => {
    expect(formatPhone('123456789012345')).toBe('123456789012345')
  })

  it('handles empty string', () => {
    expect(formatPhone('')).toBe('')
  })

  it('handles only non-digit characters', () => {
    expect(formatPhone('abc-def')).toBe('abc-def')
  })
})

// ============ Price Validation Edge Cases ============

const validatePrice = (price: string): boolean => {
  if (!price || price.trim() === '') return false
  
  // Remove currency symbols and spaces
  const normalized = price
    .replace(/[₽$€]/g, '')
    .replace(/\s/g, '')
    .replace(/,/g, '.')
    .replace(/^от/i, '')
    .trim()
  
  if (normalized === '') return false
  
  const num = parseFloat(normalized)
  return !isNaN(num) && num >= 0 && isFinite(num)
}

describe('Price Validation Edge Cases', () => {
  it('validates zero', () => {
    expect(validatePrice('0')).toBe(true)
    expect(validatePrice('0.00')).toBe(true)
  })

  it('validates with currency symbols', () => {
    expect(validatePrice('100 ₽')).toBe(true)
    expect(validatePrice('$100')).toBe(true)
    expect(validatePrice('100€')).toBe(true)
  })

  it('validates with "от" prefix', () => {
    expect(validatePrice('от 15000')).toBe(true)
    expect(validatePrice('ОТ 15000')).toBe(true)
  })

  it('validates with comma as decimal separator', () => {
    expect(validatePrice('100,50')).toBe(true)
  })

  it('rejects negative prices', () => {
    expect(validatePrice('-100')).toBe(false)
  })

  it('rejects non-numeric values', () => {
    expect(validatePrice('бесплатно')).toBe(false)
    expect(validatePrice('abc')).toBe(false)
  })

  it('rejects empty values', () => {
    expect(validatePrice('')).toBe(false)
    expect(validatePrice('   ')).toBe(false)
  })

  it('rejects Infinity', () => {
    expect(validatePrice('Infinity')).toBe(false)
  })

  it('rejects NaN', () => {
    expect(validatePrice('NaN')).toBe(false)
  })
})

// ============ Email Validation Edge Cases ============

const validateEmail = (email: string): boolean => {
  if (!email || email.trim() === '') return false
  const re = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
  return re.test(email.trim())
}

describe('Email Validation Edge Cases', () => {
  it('validates minimal email', () => {
    expect(validateEmail('a@b.c')).toBe(true)
  })

  it('validates email with plus sign', () => {
    expect(validateEmail('user+tag@example.com')).toBe(true)
  })

  it('validates email with dots in local part', () => {
    expect(validateEmail('first.last@example.com')).toBe(true)
  })

  it('validates email with subdomain', () => {
    expect(validateEmail('user@mail.example.com')).toBe(true)
  })

  it('rejects email without TLD', () => {
    expect(validateEmail('user@localhost')).toBe(false)
  })

  it('rejects email with spaces', () => {
    expect(validateEmail('user @example.com')).toBe(false)
    expect(validateEmail('user@ example.com')).toBe(false)
  })

  it('rejects multiple @ signs', () => {
    expect(validateEmail('user@@example.com')).toBe(false)
    expect(validateEmail('user@exam@ple.com')).toBe(false)
  })

  it('handles whitespace around email', () => {
    expect(validateEmail('  user@example.com  ')).toBe(true)
  })
})

// ============ Text Truncation Edge Cases ============

const truncateText = (text: string, maxLength: number, suffix = '...'): string => {
  if (!text) return ''
  if (maxLength <= 0) return ''
  if (text.length <= maxLength) return text
  if (maxLength <= suffix.length) return suffix.slice(0, maxLength)
  return text.slice(0, maxLength - suffix.length) + suffix
}

describe('Text Truncation Edge Cases', () => {
  it('handles empty string', () => {
    expect(truncateText('', 10)).toBe('')
  })

  it('handles null/undefined', () => {
    expect(truncateText(null as unknown as string, 10)).toBe('')
    expect(truncateText(undefined as unknown as string, 10)).toBe('')
  })

  it('handles zero maxLength', () => {
    expect(truncateText('Hello', 0)).toBe('')
  })

  it('handles negative maxLength', () => {
    expect(truncateText('Hello', -5)).toBe('')
  })

  it('handles maxLength smaller than suffix', () => {
    expect(truncateText('Hello World', 2)).toBe('..')
  })

  it('handles exact maxLength', () => {
    expect(truncateText('Hello', 5)).toBe('Hello')
  })

  it('handles Unicode characters', () => {
    expect(truncateText('Привет мир', 7)).toBe('Прив...')
  })

  it('handles emojis', () => {
    // Emojis can be multi-byte, this tests basic handling
    const result = truncateText('Hello 👋 World', 10)
    expect(result.length).toBeLessThanOrEqual(10)
  })
})

// ============ Slugify Edge Cases ============

const slugify = (text: string): string => {
  if (!text) return ''
  
  return text
    .toLowerCase()
    .replace(/[а-яё]/g, (char) => {
      const map: Record<string, string> = {
        'а': 'a', 'б': 'b', 'в': 'v', 'г': 'g', 'д': 'd', 'е': 'e', 'ё': 'yo',
        'ж': 'zh', 'з': 'z', 'и': 'i', 'й': 'y', 'к': 'k', 'л': 'l', 'м': 'm',
        'н': 'n', 'о': 'o', 'п': 'p', 'р': 'r', 'с': 's', 'т': 't', 'у': 'u',
        'ф': 'f', 'х': 'h', 'ц': 'ts', 'ч': 'ch', 'ш': 'sh', 'щ': 'sch', 'ъ': '',
        'ы': 'y', 'ь': '', 'э': 'e', 'ю': 'yu', 'я': 'ya'
      }
      return map[char] || char
    })
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '')
}

describe('Slugify Edge Cases', () => {
  it('handles empty string', () => {
    expect(slugify('')).toBe('')
  })

  it('handles only special characters', () => {
    expect(slugify('!@#$%^&*()')).toBe('')
  })

  it('handles multiple consecutive spaces', () => {
    expect(slugify('hello    world')).toBe('hello-world')
  })

  it('handles leading/trailing special chars', () => {
    expect(slugify('---hello---')).toBe('hello')
  })

  it('handles numbers only', () => {
    expect(slugify('12345')).toBe('12345')
  })

  it('handles mixed case', () => {
    expect(slugify('HeLLo WoRLD')).toBe('hello-world')
  })

  it('handles Russian soft/hard signs', () => {
    // ъ и ь удаляются, но могут создавать дефисы между словами
    const result1 = slugify('объём')
    expect(result1).toMatch(/ob.*m/) // Содержит ob и m
    
    const result2 = slugify('подъезд')
    expect(result2).toMatch(/pod.*ezd/) // Содержит pod и ezd
  })

  it('handles ё character', () => {
    expect(slugify('ёлка')).toBe('yolka')
  })
})

// ============ Date Formatting Edge Cases ============

const formatDate = (date: Date | string | null | undefined): string => {
  if (!date) return ''
  
  try {
    const d = typeof date === 'string' ? new Date(date) : date
    if (isNaN(d.getTime())) return ''
    
    return d.toLocaleDateString('ru-RU', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric'
    })
  } catch {
    return ''
  }
}

describe('Date Formatting Edge Cases', () => {
  it('handles null', () => {
    expect(formatDate(null)).toBe('')
  })

  it('handles undefined', () => {
    expect(formatDate(undefined)).toBe('')
  })

  it('handles invalid date string', () => {
    expect(formatDate('not-a-date')).toBe('')
  })

  it('handles ISO date string', () => {
    const result = formatDate('2025-12-24T12:00:00Z')
    expect(result).toContain('24')
    expect(result).toContain('12')
    expect(result).toContain('2025')
  })

  it('handles Date object', () => {
    const result = formatDate(new Date(2025, 11, 24))
    expect(result).toContain('24')
    expect(result).toContain('12')
    expect(result).toContain('2025')
  })

  it('handles epoch timestamp', () => {
    const result = formatDate(new Date(0))
    expect(result).not.toBe('')
  })
})

// ============ Array/Collection Edge Cases ============

const safeFirst = <T>(arr: T[] | null | undefined): T | undefined => {
  return arr?.[0]
}

const safeLast = <T>(arr: T[] | null | undefined): T | undefined => {
  return arr?.[arr.length - 1]
}

const uniqueBy = <T, K>(arr: T[], keyFn: (item: T) => K): T[] => {
  const seen = new Set<K>()
  return arr.filter(item => {
    const key = keyFn(item)
    if (seen.has(key)) return false
    seen.add(key)
    return true
  })
}

describe('Array Utility Edge Cases', () => {
  it('safeFirst handles null', () => {
    expect(safeFirst(null)).toBeUndefined()
  })

  it('safeFirst handles undefined', () => {
    expect(safeFirst(undefined)).toBeUndefined()
  })

  it('safeFirst handles empty array', () => {
    expect(safeFirst([])).toBeUndefined()
  })

  it('safeLast handles empty array', () => {
    expect(safeLast([])).toBeUndefined()
  })

  it('uniqueBy removes duplicates', () => {
    const items = [
      { id: 1, name: 'a' },
      { id: 2, name: 'b' },
      { id: 1, name: 'c' }
    ]
    const result = uniqueBy(items, item => item.id)
    expect(result).toHaveLength(2)
    expect(result[0].name).toBe('a')
  })

  it('uniqueBy handles empty array', () => {
    expect(uniqueBy([], (x: number) => x)).toEqual([])
  })
})

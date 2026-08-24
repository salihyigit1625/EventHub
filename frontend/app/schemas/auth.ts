import { z } from 'zod'

export const passwordSchema = z.string()
  .min(8, 'En az 8 karakter')
  .max(128, 'En fazla 128 karakter')
  .regex(/[A-Z]/, 'En az bir büyük harf')
  .regex(/[a-z]/, 'En az bir küçük harf')
  .regex(/[^a-zA-Z0-9]/, 'En az bir özel karakter')

export const loginSchema = z.object({
  email: z.string().email('Geçerli bir e-posta girin'),
  password: z.string().min(1, 'Şifre gerekli')
})

export const registerAttendeeSchema = z.object({
  fullName: z.string().min(1, 'Ad soyad gerekli').max(200),
  email: z.string().email('Geçerli bir e-posta girin').max(256),
  password: passwordSchema
})

export const registerOrganizerSchema = registerAttendeeSchema.extend({
  companyName: z.string().min(1, 'Şirket adı gerekli').max(200),
  taxNumber: z.string().max(50).optional().or(z.literal(''))
})

export type LoginInput = z.infer<typeof loginSchema>
export type RegisterAttendeeInput = z.infer<typeof registerAttendeeSchema>
export type RegisterOrganizerInput = z.infer<typeof registerOrganizerSchema>

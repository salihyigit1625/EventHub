import { z } from 'zod'
import { passwordSchema } from './auth'

export const depositSchema = z.object({
  amount: z.coerce.number().gt(0, 'Tutar 0’dan büyük olmalı').lte(10000, 'En fazla 10.000')
})

export const organizerProfileSchema = z.object({
  companyName: z.string().min(1, 'Şirket adı gerekli').max(200),
  taxNumber: z.string().max(50).optional().or(z.literal(''))
})

export const checkInSchema = z.object({
  uniqueCode: z.string().min(1, 'Bilet kodu gerekli').max(64),
  deviceLocation: z.string().max(200).optional().or(z.literal(''))
})

export const createGateStaffSchema = z.object({
  email: z.string().email().max(256),
  password: passwordSchema,
  fullName: z.string().min(1).max(200),
  assignedEventId: z.string().optional()
})

export const assignGateStaffSchema = z.object({
  gateStaffUserId: z.coerce.number().int().positive('Geçerli kullanıcı id'),
  eventId: z.coerce.number().int().positive('Geçerli etkinlik id')
})

export type DepositInput = z.infer<typeof depositSchema>
export type OrganizerProfileInput = z.infer<typeof organizerProfileSchema>
export type CheckInInput = z.infer<typeof checkInSchema>
export type CreateGateStaffInput = z.infer<typeof createGateStaffSchema>
export type AssignGateStaffInput = z.infer<typeof assignGateStaffSchema>

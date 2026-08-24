import { z } from 'zod'

export const eventFormSchema = z.object({
  title: z.string().min(1, 'Başlık gerekli').max(200),
  description: z.string().max(4000).optional().or(z.literal('')),
  venue: z.string().min(1, 'Mekan gerekli').max(300),
  startDate: z.string().min(1, 'Başlangıç gerekli'),
  endDate: z.string().min(1, 'Bitiş gerekli'),
  cancellationDeadlineHours: z.coerce.number().int().min(0, '0 veya daha büyük olmalı')
}).refine(data => new Date(data.endDate) > new Date(data.startDate), {
  message: 'Bitiş, başlangıçtan sonra olmalı',
  path: ['endDate']
})

export const ticketTypeFormSchema = z.object({
  name: z.string().min(1, 'Ad gerekli').max(150),
  price: z.coerce.number().min(0, 'Fiyat 0 veya daha büyük'),
  totalQuantity: z.coerce.number().int().min(1, 'En az 1 adet'),
  maxTicketsPerUser: z.coerce.number().int().min(1, 'En az 1'),
  saleStartDate: z.string().min(1, 'Satış başlangıcı gerekli'),
  saleEndDate: z.string().min(1, 'Satış bitişi gerekli')
}).refine(data => new Date(data.saleEndDate) > new Date(data.saleStartDate), {
  message: 'Satış bitişi, başlangıçtan sonra olmalı',
  path: ['saleEndDate']
})

export type EventFormInput = z.infer<typeof eventFormSchema>
export type TicketTypeFormInput = z.infer<typeof ticketTypeFormSchema>

import { z } from 'zod'

export const organizerProfileFormSchema = z.object({
  companyName: z.string().min(1, 'Şirket adı gerekli').max(200),
  taxNumber: z.string().max(50).optional().or(z.literal(''))
})

export type OrganizerProfileFormInput = z.infer<typeof organizerProfileFormSchema>

export const AppRoles = {
  Admin: 'Admin',
  Organizer: 'Organizer',
  Attendee: 'Attendee',
  GateStaff: 'GateStaff'
} as const

export type AppRole = (typeof AppRoles)[keyof typeof AppRoles]

export const ROLE_LABEL: Record<string, string> = {
  [AppRoles.Admin]: 'Yönetici',
  [AppRoles.Organizer]: 'Organizatör',
  [AppRoles.Attendee]: 'Katılımcı',
  [AppRoles.GateStaff]: 'Kapı görevlisi'
}

export const EventStatus = {
  Draft: 1,
  Published: 2,
  Cancelled: 3,
  Completed: 4
} as const

export const TicketStatus = {
  Reserved: 1,
  Paid: 2,
  CheckedIn: 3,
  Cancelled: 4,
  Refunded: 5
} as const

export const WaitlistStatus = {
  Waiting: 1,
  Notified: 2,
  Expired: 3,
  Converted: 4
} as const

export const PaymentStatus = {
  Pending: 1,
  Completed: 2,
  Failed: 3,
  Refunded: 4
} as const

export const WalletTransactionType = {
  Purchase: 1,
  Refund: 2,
  Deposit: 3
} as const

export const EVENT_STATUS_LABEL: Record<number, string> = {
  [EventStatus.Draft]: 'Taslak',
  [EventStatus.Published]: 'Yayında',
  [EventStatus.Cancelled]: 'İptal',
  [EventStatus.Completed]: 'Tamamlandı'
}

export const TICKET_STATUS_LABEL: Record<number, string> = {
  [TicketStatus.Reserved]: 'Rezerve',
  [TicketStatus.Paid]: 'Ödendi',
  [TicketStatus.CheckedIn]: 'Giriş yapıldı',
  [TicketStatus.Cancelled]: 'İptal',
  [TicketStatus.Refunded]: 'İade'
}

export const WAITLIST_STATUS_LABEL: Record<number, string> = {
  [WaitlistStatus.Waiting]: 'Beklemede',
  [WaitlistStatus.Notified]: 'Sıra geldi',
  [WaitlistStatus.Expired]: 'Süresi doldu',
  [WaitlistStatus.Converted]: 'Bilet alındı'
}

export const PAYMENT_STATUS_LABEL: Record<number, string> = {
  [PaymentStatus.Pending]: 'Bekliyor',
  [PaymentStatus.Completed]: 'Tamamlandı',
  [PaymentStatus.Failed]: 'Başarısız',
  [PaymentStatus.Refunded]: 'İade'
}

export const WALLET_TYPE_LABEL: Record<number, string> = {
  [WalletTransactionType.Purchase]: 'Satın alma',
  [WalletTransactionType.Refund]: 'İade',
  [WalletTransactionType.Deposit]: 'Yükleme'
}

export function homePathForRoles(roles: readonly string[]) {
  if (roles.includes(AppRoles.Admin)) return '/admin'
  if (roles.includes(AppRoles.Organizer)) return '/organizer'
  if (roles.includes(AppRoles.GateStaff)) return '/check-in'
  if (roles.includes(AppRoles.Attendee)) return '/account'
  return '/'
}

export function loginWithRedirect(path: string) {
  return `/login?redirect=${encodeURIComponent(path)}`
}

import type { AuthResponseDto, CurrentUserDto } from '~/api-client'

export const useAuthStore = defineStore('auth', () => {
  const api = useApi()
  const { accessToken, refreshToken, clearAuthCookies } = useAuthCookies()

  const user = ref<CurrentUserDto | null>(null)
  const hydrated = ref(false)
  const pending = ref(false)

  const isAuthenticated = computed(() => Boolean(accessToken.value && user.value))
  const roles = computed(() => user.value?.roles ?? [])
  const fullName = computed(() => user.value?.fullName ?? '')
  const email = computed(() => user.value?.email ?? '')

  const isAdmin = computed(() => roles.value.includes(AppRoles.Admin))
  const isOrganizer = computed(() => roles.value.includes(AppRoles.Organizer))
  const isAttendee = computed(() => roles.value.includes(AppRoles.Attendee))
  const isGateStaff = computed(() => roles.value.includes(AppRoles.GateStaff))

  function applySession(dto: AuthResponseDto) {
    if (!dto.accessToken || !dto.refreshToken)
      throw new ApiError({ status: 500, title: 'Eksik oturum yanıtı' })

    accessToken.value = dto.accessToken
    refreshToken.value = dto.refreshToken
    user.value = {
      userId: dto.userId,
      email: dto.email,
      fullName: dto.fullName,
      isActive: true,
      roles: dto.roles ?? []
    }
  }

  async function login(payload: { email: string, password: string }) {
    pending.value = true
    try {
      const dto = await api.post<AuthResponseDto>('/api/auth/login', payload)
      applySession(dto)
      return dto
    }
    finally {
      pending.value = false
    }
  }

  async function registerAttendee(payload: { email: string, password: string, fullName: string }) {
    pending.value = true
    try {
      const dto = await api.post<AuthResponseDto>('/api/auth/register/attendee', payload)
      applySession(dto)
      return dto
    }
    finally {
      pending.value = false
    }
  }

  async function registerOrganizer(payload: {
    email: string
    password: string
    fullName: string
    companyName: string
    taxNumber?: string
  }) {
    pending.value = true
    try {
      const dto = await api.post<AuthResponseDto>('/api/auth/register/organizer', payload)
      applySession(dto)
      return dto
    }
    finally {
      pending.value = false
    }
  }

  async function logout() {
    try {
      if (accessToken.value)
        await api.post('/api/auth/logout')
    }
    catch {
      // Token already invalid — still drop local session.
    }
    finally {
      user.value = null
      clearAuthCookies()
    }
  }

  async function hydrate() {
    if (hydrated.value) return
    if (!accessToken.value && !refreshToken.value) {
      hydrated.value = true
      return
    }

    try {
      if (!accessToken.value)
        await api.refreshSession()
      user.value = await api.get<CurrentUserDto>('/api/auth/me')
    }
    catch {
      user.value = null
      clearAuthCookies()
    }
    finally {
      hydrated.value = true
    }
  }

  function hasRole(role: string) {
    return roles.value.includes(role)
  }

  return {
    user,
    hydrated,
    pending,
    isAuthenticated,
    roles,
    fullName,
    email,
    isAdmin,
    isOrganizer,
    isAttendee,
    isGateStaff,
    login,
    registerAttendee,
    registerOrganizer,
    logout,
    hydrate,
    hasRole
  }
})

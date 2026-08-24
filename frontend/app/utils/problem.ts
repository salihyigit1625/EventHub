export class ApiError extends Error {
  status: number
  title: string
  detail?: string
  errors?: Record<string, string[]>

  constructor(init: {
    status: number
    title: string
    detail?: string
    errors?: Record<string, string[]>
  }) {
    super(init.detail || init.title)
    this.name = 'ApiError'
    this.status = init.status
    this.title = init.title
    this.detail = init.detail
    this.errors = init.errors
  }
}

interface ProblemBody {
  title?: string
  detail?: string
  status?: number
  errors?: Record<string, string[]>
}

export function firstFieldError(errors?: Record<string, string[]>) {
  if (!errors) return undefined
  const first = Object.values(errors)[0]
  return first?.[0]
}

export function toApiError(error: unknown): ApiError {
  if (error instanceof ApiError) return error

  if (typeof error === 'object' && error !== null && 'statusCode' in error) {
    const fetchError = error as {
      statusCode?: number
      statusMessage?: string
      data?: ProblemBody | string
    }
    const body = typeof fetchError.data === 'object' ? fetchError.data : undefined
    const status = fetchError.statusCode ?? 500
    const fallbackTitle = status === 403
      ? 'Bu işlem için yetkin yok'
      : status === 401
        ? 'Oturum gerekli'
        : 'İstek başarısız'
    const fallbackDetail = status === 403
      ? 'Bu hesapla bu işlemi yapamazsın.'
      : undefined
    return new ApiError({
      status,
      title: body?.title || fetchError.statusMessage || fallbackTitle,
      detail: body?.detail
        || (typeof fetchError.data === 'string' ? fetchError.data : undefined)
        || fallbackDetail,
      errors: body?.errors
    })
  }

  return new ApiError({
    status: 500,
    title: 'İstek başarısız',
    detail: error instanceof Error ? error.message : 'Bilinmeyen hata'
  })
}

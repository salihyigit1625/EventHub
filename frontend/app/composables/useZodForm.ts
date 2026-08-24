import type { ZodType, z } from 'zod'

export function useZodForm<T extends ZodType>(schema: T) {
  const form = reactive({
    errors: {} as Record<string, string>,
    formError: null as string | null,
    parse(data: unknown): z.infer<T> | null {
      form.formError = null
      for (const key of Object.keys(form.errors))
        delete form.errors[key]

      const result = schema.safeParse(data)
      if (!result.success) {
        for (const issue of result.error.issues) {
          const key = String(issue.path[0] ?? '_')
          if (!form.errors[key])
            form.errors[key] = issue.message
        }
        return null
      }
      return result.data
    },
    fromApi(error: unknown) {
      const apiError = toApiError(error)
      form.formError = firstFieldError(apiError.errors) || apiError.detail || apiError.title
      for (const key of Object.keys(form.errors))
        delete form.errors[key]
      if (apiError.errors) {
        for (const [key, messages] of Object.entries(apiError.errors)) {
          const camel = key.charAt(0).toLowerCase() + key.slice(1)
          form.errors[camel] = messages[0] ?? ''
        }
      }
    }
  })

  return form
}

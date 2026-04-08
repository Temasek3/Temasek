import type {
  RequestConfig as BaseRequestConfig,
  ResponseConfig,
  ResponseErrorConfig,
} from '@kubb/plugin-client/clients/fetch'
import fetchClient from '@kubb/plugin-client/clients/fetch'

export type KubbAuthMode = 'auto' | 'required' | 'none'

export type RequestConfig<TData = unknown> = BaseRequestConfig<TData> & {
  auth?: KubbAuthMode
}

export type Client = <TResponseData, _TError = unknown, TRequestData = unknown>(
  config: RequestConfig<TRequestData>,
) => Promise<ResponseConfig<TResponseData>>

const ABSOLUTE_URL_PATTERN = /^https?:\/\//

function normalizeHeaders(headers?: BaseRequestConfig['headers']): Record<string, string> {
  if (!headers) {
    return {}
  }

  return Array.isArray(headers) ? Object.fromEntries(headers) : headers
}

function hasHeader(headers: Record<string, string>, name: string) {
  return Object.keys(headers).some(key => key.toLowerCase() === name.toLowerCase())
}

function resolveApiBaseUrl(baseUrl?: string) {
  const normalized = String(baseUrl ?? '').trim()

  return ABSOLUTE_URL_PATTERN.test(normalized) ? normalized : undefined
}

function resolveRequestHeaders(
  requestConfig: Omit<RequestConfig, 'auth' | 'headers'>,
  headers: Record<string, string>,
  authHeaders: Record<string, string>,
) {
  const resolvedHeaders = {
    ...headers,
    ...authHeaders,
  }

  if (
    requestConfig.data !== undefined
    && !(requestConfig.data instanceof FormData)
    && !hasHeader(resolvedHeaders, 'Content-Type')
  ) {
    resolvedHeaders['Content-Type'] = 'application/json'
  }

  return resolvedHeaders
}

async function resolveAuthHeaders(auth: KubbAuthMode): Promise<Record<string, string>> {
  if (auth === 'none') {
    return {}
  }

  try {
    const { getToken } = useAuth()
    const token = await getToken.value()

    if (!token) {
      if (auth === 'required') {
        throw new Error('Authentication is required for this request.')
      }

      return {}
    }

    return {
      Authorization: `Bearer ${token}`,
    }
  }
  catch (error) {
    if (auth === 'required') {
      throw error instanceof Error
        ? error
        : new Error('Authentication is required for this request.')
    }

    return {}
  }
}

const kubbClient: Client = async (config) => {
  const runtimeConfig = useRuntimeConfig()
  const { auth = 'auto', headers, ...requestConfig } = config
  const normalizedHeaders = normalizeHeaders(headers)
  const authHeaders = await resolveAuthHeaders(auth)

  return fetchClient({
    ...requestConfig,
    baseURL: requestConfig.baseURL ?? resolveApiBaseUrl(runtimeConfig.public.temasekWebApiHttps),
    headers: resolveRequestHeaders(requestConfig, normalizedHeaders, authHeaders),
  })
}

export const publicKubbClient: Client = async config => kubbClient({
  ...config,
  auth: 'none',
})

export const authenticatedKubbClient: Client = async config => kubbClient({
  ...config,
  auth: 'required',
})

export default kubbClient

export type { ResponseConfig, ResponseErrorConfig }

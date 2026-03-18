import type { Client } from '@kubb/plugin-client/clients/fetch'
import fetchClient from '@kubb/plugin-client/clients/fetch'

const kubbClient: Client = async (config) => {
  const { getToken } = useAuth()
  const runtimeConfig = useRuntimeConfig()

  return fetchClient({
    ...config,
    baseURL: config.baseURL ?? runtimeConfig.public.temasekWebApiHttps,
    headers: {
      Authorization: `Bearer ${await getToken.value()}`,
    },
  })
}

export default kubbClient

export type { Client, RequestConfig, ResponseErrorConfig } from '@kubb/plugin-client/clients/fetch'

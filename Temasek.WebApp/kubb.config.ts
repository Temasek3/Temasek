import { defineConfig } from '@kubb/core'
import { pluginFaker } from '@kubb/plugin-faker'
import { pluginMsw } from '@kubb/plugin-msw'
import { pluginOas } from '@kubb/plugin-oas'
import { pluginTs } from '@kubb/plugin-ts'
import { pluginVueQuery } from '@kubb/plugin-vue-query'
import { pluginZod } from '@kubb/plugin-zod'

export default defineConfig({
  root: '.',
  input: {
    path: 'http://localhost:5149/openapi/v1.json',
  },
  output: {
    path: './app/kubb',
    format: false,
  },
  plugins: [
    pluginOas(),
    pluginTs(),
    pluginVueQuery({
      client: {
        importPath: '../../utils/kubb-client',
      },
    }),
    pluginFaker(),
    pluginMsw(),
    pluginZod(),
  ],
})

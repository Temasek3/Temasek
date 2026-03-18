<script setup lang="ts">
import { useTemasekWebApiFeaturesEnabledFeaturesGetEndpoint } from '~/kubb'

const x = useTemasekWebApiFeaturesEnabledFeaturesGetEndpoint()
</script>

<template>
  <div>
    {{ x }}
  </div>
</template>

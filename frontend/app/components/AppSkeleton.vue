<script setup lang="ts">
withDefaults(
  defineProps<{
    /** Sayfanın kendi iskeletini yansıtan hazır kalıplar. */
    variant?: 'text' | 'title' | 'media' | 'block' | 'pill' | 'card' | 'row' | 'ticket' | 'kpi'
    count?: number
    width?: string
    /** false ise dış kapsayıcı sınıfını üst bileşen belirler (grid içi kullanım). */
    stack?: boolean
  }>(),
  { variant: 'text', count: 1, width: undefined, stack: true }
)
</script>

<template>
  <div :class="{ 'sk-stack': stack }" aria-hidden="true">
    <template v-for="i in count" :key="i">
      <div v-if="variant === 'card'" class="sk-card">
        <div class="sk sk--media" />
        <div class="sk sk--title" />
        <div class="sk sk--text" style="width: 70%" />
      </div>

      <div v-else-if="variant === 'row'" class="sk-card" style="display:flex; gap:0.8rem; align-items:center">
        <div class="sk" style="width:2.6rem; height:2.6rem; border-radius:50%" />
        <div class="sk-stack" style="flex:1">
          <div class="sk sk--text" style="width: 45%" />
          <div class="sk sk--text" style="width: 70%" />
        </div>
        <div class="sk sk--pill" />
      </div>

      <div v-else-if="variant === 'ticket'" class="sk" style="height: 11.5rem; border-radius: 20px" />

      <div v-else-if="variant === 'kpi'" class="sk-card">
        <div class="sk sk--text" style="width: 40%" />
        <div class="sk" style="height: 2rem; width: 60%" />
      </div>

      <div v-else class="sk" :class="`sk--${variant}`" :style="width ? { width } : undefined" />
    </template>
  </div>
</template>

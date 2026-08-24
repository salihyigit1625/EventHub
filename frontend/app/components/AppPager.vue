<script setup lang="ts">
const props = defineProps<{
  page: number
  pageSize: number
  totalCount: number
}>()

const emit = defineEmits<{ change: [page: number] }>()

const lastPage = computed(() => Math.max(1, Math.ceil(props.totalCount / Math.max(props.pageSize, 1))))
</script>

<template>
  <div v-if="totalCount > pageSize" class="pager">
    <button class="btn btn-ghost" type="button" :disabled="page <= 1" @click="emit('change', page - 1)">
      Önceki
    </button>
    <span class="event-card__meta">{{ page }} / {{ lastPage }} · {{ totalCount }} kayıt</span>
    <button class="btn btn-ghost" type="button" :disabled="page >= lastPage" @click="emit('change', page + 1)">
      Sonraki
    </button>
  </div>
</template>

<script setup lang="ts">
const props = withDefaults(
  defineProps<{
    modelValue: File | null
    /** Henüz dosya seçilmemişken gösterilecek mevcut afiş. */
    fallbackSrc?: string | null
    accept?: string
    maxMb?: number
  }>(),
  { fallbackSrc: null, accept: '.png,.jpg,.jpeg', maxMb: 5 }
)

const emit = defineEmits<{ 'update:modelValue': [File | null] }>()

const input = ref<HTMLInputElement | null>(null)
const over = ref(false)
const localError = ref('')
const previewUrl = ref('')

function setFile(file: File | null) {
  localError.value = ''
  if (file) {
    if (!/image\/(png|jpe?g)/.test(file.type)) {
      localError.value = 'Yalnızca PNG veya JPG yükleyebilirsin.'
      return
    }
    if (file.size > props.maxMb * 1024 * 1024) {
      localError.value = `Dosya ${props.maxMb} MB sınırını aşıyor.`
      return
    }
  }
  emit('update:modelValue', file)
}

function onDrop(event: DragEvent) {
  over.value = false
  setFile(event.dataTransfer?.files?.[0] ?? null)
}

function onPick(event: Event) {
  setFile((event.target as HTMLInputElement).files?.[0] ?? null)
}

function clearFile() {
  setFile(null)
  if (input.value) input.value.value = ''
}

watch(
  () => props.modelValue,
  (file) => {
    if (previewUrl.value) URL.revokeObjectURL(previewUrl.value)
    previewUrl.value = file ? URL.createObjectURL(file) : ''
  },
  { immediate: true }
)

onBeforeUnmount(() => {
  if (previewUrl.value) URL.revokeObjectURL(previewUrl.value)
})

const shown = computed(() => previewUrl.value || props.fallbackSrc || '')
</script>

<template>
  <div>
    <div
      class="dropzone"
      :class="{ 'dropzone--over': over, 'dropzone--filled': Boolean(shown) }"
      role="button"
      tabindex="0"
      @click="input?.click()"
      @keydown.enter.prevent="input?.click()"
      @keydown.space.prevent="input?.click()"
      @dragover.prevent="over = true"
      @dragleave.prevent="over = false"
      @drop.prevent="onDrop"
    >
      <img v-if="shown" class="dropzone__preview" :src="shown" alt="Afiş önizleme">
      <template v-else>
        <span class="dropzone__icon" aria-hidden="true">⇪</span>
        <strong>Afişi buraya sürükle</strong>
        <span>ya da tıklayıp seç · PNG / JPG · en fazla {{ maxMb }} MB</span>
      </template>
    </div>

    <input
      ref="input"
      type="file"
      :accept="accept"
      hidden
      @change="onPick"
    >

    <div v-if="modelValue || localError" style="display:flex; gap:0.5rem; align-items:center; margin-top:0.6rem; flex-wrap:wrap">
      <span v-if="modelValue" class="event-card__meta">
        {{ modelValue.name }} · {{ formatBytes(modelValue.size) }}
      </span>
      <button v-if="modelValue" class="btn btn-ghost" type="button" @click="clearFile">Kaldır</button>
      <small v-if="localError" class="error" style="color:#c0553f">{{ localError }}</small>
    </div>
  </div>
</template>

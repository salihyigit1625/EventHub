<script setup lang="ts">
import QRCode from 'qrcode'

const props = withDefaults(
  defineProps<{
    value?: string | null
    size?: number
    dim?: boolean
  }>(),
  { value: null, size: 320, dim: false }
)

const src = ref('')
const failed = ref(false)

async function render() {
  if (!import.meta.client || !props.value) return
  try {
    src.value = await QRCode.toDataURL(props.value, {
      margin: 0,
      width: props.size,
      errorCorrectionLevel: 'M',
      color: { dark: props.dim ? '#8b8b8b' : '#100d0c', light: '#ffffff' }
    })
    failed.value = false
  }
  catch {
    failed.value = true
  }
}

onMounted(render)
watch(() => [props.value, props.dim], render)
</script>

<template>
  <div class="ticket__qr">
    <img v-if="src && !failed" :src="src" :alt="`Bilet karekodu ${value}`">
    <div v-else class="sk" style="width:100%;height:100%;border-radius:6px" />
  </div>
</template>

<script setup lang="ts">
/**
 * Bekleme sırasında metnini değiştirip kendini kilitler.
 * Çift tıklama, pending sırasında click yayılmayarak engellenir.
 */
const props = withDefaults(
  defineProps<{
    pending?: boolean
    disabled?: boolean
    pendingLabel?: string
    type?: 'button' | 'submit'
  }>(),
  {
    pending: false,
    disabled: false,
    pendingLabel: 'İşleniyor…',
    type: 'button'
  }
)

const emit = defineEmits<{ click: [MouseEvent] }>()

function onClick(event: MouseEvent) {
  if (props.pending || props.disabled) {
    event.preventDefault()
    return
  }
  emit('click', event)
}
</script>

<template>
  <button
    class="btn"
    :class="{ 'btn--pending': pending }"
    :type="type"
    :disabled="disabled || pending"
    :aria-busy="pending"
    @click="onClick"
  >
    <span v-if="pending" class="btn__spinner" aria-hidden="true" />
    <span>{{ pending ? pendingLabel : undefined }}<slot v-if="!pending" /></span>
  </button>
</template>

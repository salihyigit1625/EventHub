<script setup lang="ts">
const props = withDefaults(
  defineProps<{
    open: boolean
    title?: string
    subtitle?: string
  }>(),
  { title: '', subtitle: '' }
)

const emit = defineEmits<{ close: [] }>()

function onKey(event: KeyboardEvent) {
  if (event.key === 'Escape' && props.open) emit('close')
}

onMounted(() => window.addEventListener('keydown', onKey))
onBeforeUnmount(() => window.removeEventListener('keydown', onKey))
</script>

<template>
  <Teleport to="body">
    <Transition name="modal">
      <div
        v-if="open"
        class="modal-backdrop"
        role="dialog"
        aria-modal="true"
        @click.self="emit('close')"
      >
        <div class="modal">
          <header class="modal__head">
            <div>
              <h2 style="font-size: 1.2rem">{{ title }}</h2>
              <p v-if="subtitle" class="event-card__meta">{{ subtitle }}</p>
            </div>
            <button class="toast__close" type="button" aria-label="Kapat" @click="emit('close')">
              ×
            </button>
          </header>
          <div class="modal__body">
            <slot />
          </div>
          <footer v-if="$slots.footer" class="modal__foot">
            <slot name="footer" />
          </footer>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

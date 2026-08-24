<script setup lang="ts">
import type { NoticeTone } from '~/stores/notice'

const notice = useNoticeStore()

const glyph: Record<NoticeTone, string> = {
  good: '✓',
  bad: '!',
  info: 'i'
}
</script>

<template>
  <Teleport to="body">
    <div class="toast-stack" role="status" aria-live="polite">
      <TransitionGroup name="toast">
        <div
          v-for="item in notice.items"
          :key="item.id"
          class="toast"
          :class="`toast--${item.tone}`"
        >
          <div class="toast__icon" aria-hidden="true">{{ glyph[item.tone] }}</div>
          <div class="toast__text">
            <strong>{{ item.title }}</strong>
            <span v-if="item.description">{{ item.description }}</span>
          </div>
          <button
            class="toast__close"
            type="button"
            aria-label="Kapat"
            @click="notice.dismiss(item.id)"
          >
            ×
          </button>
        </div>
      </TransitionGroup>
    </div>
  </Teleport>
</template>

<script setup lang="ts">
import type { TicketDto } from '~/api-client'

const props = withDefaults(
  defineProps<{
    ticket: TicketDto
    eventTitle?: string
    typeName?: string
    venue?: string
    startDate?: string | null
    /** Kart tıklanabilir olsun mu (cüzdan listesinde detaya gider). */
    to?: string
  }>(),
  {
    eventTitle: undefined,
    typeName: undefined,
    venue: undefined,
    startDate: null,
    to: undefined
  }
)

const status = computed(() => props.ticket.status ?? 0)
const isUsed = computed(() => status.value === TicketStatus.CheckedIn)
const isVoid = computed(
  () => status.value === TicketStatus.Cancelled || status.value === TicketStatus.Refunded
)
const isLive = computed(() => !isUsed.value && !isVoid.value)

const flag = computed(() => {
  if (isUsed.value) return { cls: 'ticket__flag--used', text: 'Kullanıldı' }
  if (isVoid.value) return { cls: 'ticket__flag--void', text: TICKET_STATUS_LABEL[status.value] ?? 'Geçersiz' }
  return { cls: 'ticket__flag--ok', text: 'Geçerli' }
})
</script>

<template>
  <component
    :is="to ? resolveComponent('NuxtLink') : 'div'"
    :to="to"
    class="ticket"
    :class="{ 'ticket--used': isUsed, 'ticket--void': isVoid }"
  >
    <span class="ticket__flag" :class="flag.cls">{{ flag.text }}</span>

    <div class="ticket__body">
        <p class="ticket__brand">
          <img class="ticket__brand-mark" src="/brand/eventhub-mark.png" alt="" width="18" height="18">
          <span>EventHub</span>
        <span aria-hidden="true">·</span>
        <span>{{ typeName || 'Bilet' }}</span>
      </p>
      <h3 class="ticket__title">{{ eventTitle || `Bilet #${ticket.id}` }}</h3>

      <div class="ticket__grid">
        <div>
          <span>Tarih</span>
          <strong>{{ startDate ? formatDateTime(startDate) : formatDateTime(ticket.purchasedAt) }}</strong>
        </div>
        <div v-if="venue">
          <span>Mekan</span>
          <strong>{{ venue }}</strong>
        </div>
        <div>
          <span>Tutar</span>
          <strong>{{ formatMoney(ticket.unitPrice) }}</strong>
        </div>
        <div v-if="ticket.checkedInAt">
          <span>Giriş</span>
          <strong>{{ formatDateTime(ticket.checkedInAt) }}</strong>
        </div>
      </div>
    </div>

    <div class="ticket__stub">
      <TicketQr :value="ticket.uniqueCode" :dim="!isLive" />
      <p class="ticket__code">{{ ticket.uniqueCode }}</p>
    </div>
  </component>
</template>

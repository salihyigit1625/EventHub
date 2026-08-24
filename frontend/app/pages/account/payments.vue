<script setup lang="ts">
import type { PagedPaymentDto } from '~/api-client'

definePageMeta({ layout: 'account', middleware: ['attendee'] })

const api = useApi()
const page = ref(1)

const { data } = await useAsyncData(
  'payments',
  () => api.get<PagedPaymentDto>('/api/payments/mine', { page: page.value, pageSize: 10 }),
  { watch: [page] }
)
</script>

<template>
  <section>
    <h1>Ödemeler</h1>
    <p v-if="!data?.items?.length" class="empty">Ödeme kaydı yok.</p>
    <table v-else class="table">
      <thead>
        <tr>
          <th>İşlem</th>
          <th>Bilet</th>
          <th>Tutar</th>
          <th>Durum</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="row in data.items" :key="row.id">
          <td>{{ row.transactionCode }}</td>
          <td>#{{ row.ticketId }}</td>
          <td>{{ formatMoney(row.amount) }}</td>
          <td>{{ PAYMENT_STATUS_LABEL[row.status ?? 0] }}</td>
        </tr>
      </tbody>
    </table>
    <AppPager
      :page="data?.page ?? 1"
      :page-size="data?.pageSize ?? 10"
      :total-count="data?.totalCount ?? 0"
      @change="page = $event"
    />
  </section>
</template>

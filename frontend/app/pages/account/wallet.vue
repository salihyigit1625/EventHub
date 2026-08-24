<script setup lang="ts">
import type { PagedWalletTransactionDto, WalletBalanceDto } from '~/api-client'

definePageMeta({ layout: 'account', middleware: ['attendee'] })

const api = useApi()
const notice = useNoticeStore()
const form = useZodForm(depositSchema)
const action = useAsyncAction()
const amount = ref(250)
const page = ref(1)

const { data: wallet, refresh: refreshWallet } = await useAsyncData(
  'wallet',
  () => api.get<WalletBalanceDto>('/api/wallet')
)

const { data: tx, refresh: refreshTx } = await useAsyncData(
  'wallet-tx',
  () => api.get<PagedWalletTransactionDto>('/api/wallet/transactions', { page: page.value, pageSize: 10 }),
  { watch: [page] }
)

async function deposit() {
  const parsed = form.parse({ amount: amount.value })
  if (!parsed) return

  // Para hareketi: çift gönderim engellenir.
  await action.run('deposit', async () => {
    try {
      await api.post<WalletBalanceDto>('/api/wallet/deposit', parsed)
      notice.flash(`${formatMoney(parsed.amount)} yüklendi.`)
      await Promise.all([refreshWallet(), refreshTx()])
    }
    catch (error) {
      form.fromApi(error)
      const apiError = toApiError(error)
      notice.fail('Yükleme başarısız', apiError.detail || apiError.title)
    }
  })
}
</script>

<template>
  <section>
    <h1>Cüzdan</h1>
    <p class="event-card__meta">Bakiye: <strong>{{ formatMoney(wallet?.balance) }}</strong> (demo yükleme, en fazla 10.000)</p>
    <p v-if="form.formError" class="notice error">{{ form.formError }}</p>
    <form class="form-row panel" style="margin: 1rem 0" @submit.prevent="deposit">
      <AppField label="Tutar" :error="form.errors.amount">
        <input v-model.number="amount" type="number" min="0.01" max="10000" step="0.01">
      </AppField>
      <div style="display:flex;align-items:end">
        <LoadingButton
          class="btn-primary"
          type="submit"
          :pending="action.isPending('deposit')"
          pending-label="Yükleniyor…"
        >
          Yükle
        </LoadingButton>
      </div>
    </form>
    <table class="table">
      <thead>
        <tr>
          <th>Tarih</th>
          <th>Tür</th>
          <th>Tutar</th>
          <th>Bakiye</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="row in tx?.items" :key="row.id">
          <td>{{ formatDateTime(row.createdAt) }}</td>
          <td>{{ WALLET_TYPE_LABEL[row.type ?? 0] }}</td>
          <td>{{ formatMoney(row.amount) }}</td>
          <td>{{ formatMoney(row.balanceAfter) }}</td>
        </tr>
      </tbody>
    </table>
    <AppPager
      :page="tx?.page ?? 1"
      :page-size="tx?.pageSize ?? 10"
      :total-count="tx?.totalCount ?? 0"
      @change="page = $event"
    />
  </section>
</template>

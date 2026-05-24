<template>
  <div class="list-container">
    <div class="list-header">
      <i class="mdi mdi-checkbox-marked-outline" />
      Todo Locations
      <span class="count-badge">{{ items.length }}</span>
      <span v-if="slotName" style="margin-left: auto; font-size: 0.75rem; color: #58a6ff;">
        <i class="mdi mdi-account" /> {{ slotName }}
      </span>
    </div>

    <div v-if="loading" class="loading-state">
      <ProgressSpinner style="width: 24px; height: 24px;" strokeWidth="4" />
      Loading...
    </div>
    <div v-else-if="error" class="error-state">
      <i class="mdi mdi-alert-circle" /> {{ error }}
    </div>
    <div v-else-if="items.length === 0" class="empty-state">
      <i class="mdi mdi-check-circle-outline" />
      No locations to check — nothing requested!
    </div>
    <div v-else style="flex: 1; overflow: auto;">
      <DataTable
        :value="items"
        size="small"
        scrollable
        scrollHeight="flex"
        stripedRows
        :rowHover="true"
      >
        <Column header="Prio" style="width: 3.5rem; text-align: center;">
          <template #body="{ data }">
            <span :class="['prio-badge', prioCls(data.priority)]">{{ data.priority }}</span>
          </template>
        </Column>
        <Column v-if="!slot" field="finderName" header="In World" style="width: 8rem;" />
        <Column field="locationName" header="Location to Check">
          <template #body="{ data }">
            <i :class="['mdi', 'mdi-' + (data.locationIcon || 'help-circle-outline')]" class="row-icon" />
            {{ displayName(data.locationName) }}
          </template>
        </Column>
        <Column field="itemName" header="Item">
          <template #body="{ data }">
            <i :class="['mdi', 'mdi-' + (data.itemIcon || 'help-circle-outline')]" class="row-icon" />
            {{ displayName(data.itemName) }}
          </template>
        </Column>
        <Column field="requesterName" header="For" style="width: 8rem;" />
        <Column header="Notes">
          <template #body="{ data }">
            <span class="info-text">{{ data.information }}</span>
          </template>
        </Column>
      </DataTable>
    </div>
  </div>
</template>

<script setup>
import { ref, watch, computed } from 'vue'
import { getTodo, displayName } from '../api.js'

const props = defineProps({
  channelId: String,
  slot: { type: Number, default: null },
  room: { type: Object, default: null }
})

const items = ref([])
const loading = ref(true)
const error = ref(null)

const slotName = computed(() => {
  if (props.slot === null || !props.room) return null
  return props.room.slots.find(s => s.slotId === props.slot)?.name ?? null
})

function prioCls(p) {
  if (p >= 10) return 'prio-crit'
  if (p >= 7) return 'prio-high'
  if (p >= 4) return 'prio-med'
  return 'prio-low'
}

async function load() {
  loading.value = true
  error.value = null
  try {
    items.value = await getTodo(props.channelId, props.slot)
  } catch (e) {
    error.value = e.message
  } finally {
    loading.value = false
  }
}

watch([() => props.channelId, () => props.slot], load, { immediate: true })
</script>

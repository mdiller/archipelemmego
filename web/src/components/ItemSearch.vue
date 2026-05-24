<template>
  <div class="search-container">
    <div class="search-controls">
      <Select
        v-model="searchType"
        :options="typeOptions"
        optionLabel="label"
        optionValue="value"
        style="width: 140px; flex-shrink: 0;"
      >
        <template #value="slotProps">
          <div style="display: flex; align-items: center; gap: 0.4rem;">
            <i :class="['mdi', searchType === 'items' ? 'mdi-sword' : 'mdi-map-marker']" />
            {{ slotProps.value === 'items' ? 'Items' : 'Locations' }}
          </div>
        </template>
        <template #option="slotProps">
          <div style="display: flex; align-items: center; gap: 0.5rem;">
            <i :class="['mdi', slotProps.option.icon]" />
            {{ slotProps.option.label }}
          </div>
        </template>
      </Select>

      <div class="search-input-wrap">
        <i class="mdi mdi-magnify" style="position: absolute; left: 0.6rem; top: 50%; transform: translateY(-50%); color: #8b949e; pointer-events: none;" />
        <InputText
          v-model="query"
          placeholder="Search..."
          style="width: 100%; padding-left: 2rem;"
          @input="onInput"
        />
      </div>

      <span v-if="loading" style="color: #8b949e; font-size: 0.8rem; white-space: nowrap;">
        <i class="mdi mdi-loading mdi-spin" /> Searching...
      </span>
      <span v-else style="color: #8b949e; font-size: 0.8rem; white-space: nowrap;">
        {{ results.length }} result{{ results.length !== 1 ? 's' : '' }}
      </span>
    </div>

    <div v-if="error" class="error-state">
      <i class="mdi mdi-alert-circle" /> {{ error }}
    </div>
    <div v-else-if="!query && results.length === 0" class="empty-state">
      <i class="mdi mdi-magnify" />
      Type to search {{ searchType }}
    </div>
    <div v-else style="flex: 1; overflow: auto;">
      <DataTable
        :value="results"
        size="small"
        scrollable
        scrollHeight="flex"
        stripedRows
        :rowHover="true"
      >
        <Column field="name" :header="searchType === 'items' ? 'Item' : 'Location'">
          <template #body="{ data }">
            <i :class="['mdi', 'mdi-' + (data.iconName || 'help-circle-outline')]" class="row-icon" />
            {{ displayName(data.name) }}
          </template>
        </Column>
        <Column field="slotName" header="Player" style="width: 9rem;" />
        <Column field="game" header="Game" />
        <Column v-if="searchType === 'items'" field="itemId" header="ID" style="width: 8rem; font-family: monospace; font-size: 0.75rem;" />
        <Column v-else field="locationId" header="ID" style="width: 8rem; font-family: monospace; font-size: 0.75rem;" />
      </DataTable>
    </div>
  </div>
</template>

<script setup>
import { ref, watch } from 'vue'
import { searchItems, searchLocations, displayName } from '../api.js'

const props = defineProps({
  channelId: String,
  slot: { type: Number, default: null }
})

const searchType = ref('items')
const query = ref('')
const results = ref([])
const loading = ref(false)
const error = ref(null)

const typeOptions = [
  { label: 'Items', value: 'items', icon: 'mdi-sword' },
  { label: 'Locations', value: 'locations', icon: 'mdi-map-marker' }
]

let debounceTimer = null

function onInput() {
  clearTimeout(debounceTimer)
  debounceTimer = setTimeout(doSearch, 250)
}

async function doSearch() {
  if (!query.value) {
    results.value = []
    return
  }
  loading.value = true
  error.value = null
  try {
    results.value = searchType.value === 'items'
      ? await searchItems(props.channelId, query.value, props.slot)
      : await searchLocations(props.channelId, query.value, props.slot)
  } catch (e) {
    error.value = e.message
  } finally {
    loading.value = false
  }
}

watch([searchType, () => props.slot], () => {
  if (query.value) doSearch()
})

watch(() => props.channelId, () => {
  query.value = ''
  results.value = []
})
</script>

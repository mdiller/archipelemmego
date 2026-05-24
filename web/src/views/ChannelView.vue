<template>
  <div class="channel-view">
    <div class="topbar">
      <div class="topbar-left">
        <i class="mdi mdi-island" style="font-size: 1.3rem; color: #58a6ff;" />
        <span class="app-title">ArchipeLemmeGo</span>
        <a
          v-if="room?.guildId && room.guildId !== '0'"
          :href="`https://discord.com/channels/${room.guildId}/${channelId}`"
          target="_blank"
          rel="noopener"
          class="channel-badge channel-badge-link"
        >{{ channelId }}</a>
        <span v-else class="channel-badge">{{ channelId }}</span>
      </div>

      <div class="topbar-controls">
        <span class="topbar-label">
          <i class="mdi mdi-controller" />
          Mode
        </span>
        <Select
          v-model="mode"
          :options="modeOptions"
          optionLabel="label"
          optionValue="value"
          style="min-width: 130px"
        >
          <template #value="slotProps">
            <div style="display: flex; align-items: center; gap: 0.4rem;">
              <i :class="['mdi', currentMode?.icon]" />
              {{ slotProps.value ? currentMode?.label : 'Select mode' }}
            </div>
          </template>
          <template #option="slotProps">
            <div style="display: flex; align-items: center; gap: 0.5rem;">
              <i :class="['mdi', slotProps.option.icon]" />
              {{ slotProps.option.label }}
            </div>
          </template>
        </Select>

        <span class="topbar-label">
          <i class="mdi mdi-account-group" />
          Slot
        </span>
        <Select
          v-model="selectedSlot"
          :options="slotOptions"
          optionLabel="label"
          optionValue="value"
          :loading="roomLoading"
          style="min-width: 160px"
          @change="onSlotChange"
        >
          <template #value="slotProps">
            <div style="display: flex; align-items: center; gap: 0.4rem;">
              <i class="mdi mdi-account" />
              {{ slotOptions.find(o => o.value === slotProps.value)?.label ?? 'All' }}
            </div>
          </template>
        </Select>
      </div>
    </div>

    <div class="content">
      <div v-if="roomError" class="error-state">
        <i class="mdi mdi-alert-circle" />
        {{ roomError }}
      </div>
      <template v-else>
        <WaitingList v-if="mode === 'waiting'" :channelId="channelId" :slot="selectedSlot" :room="room" />
        <TodoList v-if="mode === 'todo'" :channelId="channelId" :slot="selectedSlot" :room="room" />
        <ItemSearch v-if="mode === 'search'" :channelId="channelId" :slot="selectedSlot" />
        <DepGraph v-if="mode === 'deps'" :channelId="channelId" />
        <IconDebug v-if="mode === 'icons'" :channelId="channelId" />
      </template>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import WaitingList from '../components/WaitingList.vue'
import TodoList from '../components/TodoList.vue'
import ItemSearch from '../components/ItemSearch.vue'
import DepGraph from '../components/DepGraph.vue'
import IconDebug from '../components/IconDebug.vue'
import { getRoom } from '../api.js'

const route = useRoute()
const router = useRouter()

const channelId = computed(() => route.params.channelId)
const room = ref(null)
const roomLoading = ref(true)
const roomError = ref(null)
const mode = ref('waiting')
const selectedSlot = ref(null)

const modeOptions = [
  { label: 'Waiting', value: 'waiting', icon: 'mdi-clock-outline' },
  { label: 'Todo', value: 'todo', icon: 'mdi-checkbox-marked-outline' },
  { label: 'Search', value: 'search', icon: 'mdi-magnify' },
  { label: 'Dep Tree', value: 'deps', icon: 'mdi-graph-outline' },
  { label: 'Icons', value: 'icons', icon: 'mdi-palette-outline' }
]

const currentMode = computed(() => modeOptions.find(m => m.value === mode.value))

const slotOptions = computed(() => {
  if (!room.value) return [{ label: 'All', value: null }]
  return [
    { label: 'All', value: null },
    ...room.value.slots.map(s => ({ label: `${s.name}`, value: s.slotId }))
  ]
})

function onSlotChange() {
  const query = selectedSlot.value !== null
    ? { ...route.query, slot: selectedSlot.value }
    : Object.fromEntries(Object.entries(route.query).filter(([k]) => k !== 'slot'))
  router.replace({ query })
}

async function loadRoom() {
  roomLoading.value = true
  roomError.value = null
  try {
    room.value = await getRoom(channelId.value)
  } catch (e) {
    roomError.value = e.message
  } finally {
    roomLoading.value = false
  }
}

onMounted(() => {
  const slotParam = route.query.slot
  if (slotParam) selectedSlot.value = parseInt(slotParam)
  loadRoom()
})

watch(channelId, loadRoom)
</script>

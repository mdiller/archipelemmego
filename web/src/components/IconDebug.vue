<template>
  <div class="icon-debug-container">
    <div class="icon-debug-toolbar">
      <div class="icon-debug-search">
        <i class="mdi mdi-magnify" style="color: #8b949e;" />
        <input v-model="filterInput" placeholder="Filter icon or item name..." class="icon-filter-input" />
        <button v-if="filterInput" class="icon-filter-clear" @click="filterInput = ''">
          <i class="mdi mdi-close" />
        </button>
      </div>
      <span class="icon-debug-stats">
        <span>{{ totalGroups }} icons</span>
        <span class="icon-debug-sep">·</span>
        <span>{{ totalAssignments }} assignments</span>
      </span>
      <span v-if="hasFallback" class="icon-fallback-warning">
        <i class="mdi mdi-alert" />
        Some using fallback — matcher warming up
      </span>
      <div class="icon-debug-pager">
        <button class="pager-btn" :disabled="page === 0" @click="page--">
          <i class="mdi mdi-chevron-left" />
        </button>
        <span class="pager-label">{{ page + 1 }} / {{ totalPages }}</span>
        <button class="pager-btn" :disabled="page >= totalPages - 1" @click="page++">
          <i class="mdi mdi-chevron-right" />
        </button>
      </div>
    </div>

    <div class="icon-debug-body">
      <div v-if="loading" class="loading-state">
        <ProgressSpinner style="width: 2rem; height: 2rem;" />
        Loading...
      </div>
      <div v-else-if="error" class="error-state">
        <i class="mdi mdi-alert-circle" /> {{ error }}
      </div>
      <div v-else-if="!groups.length" class="empty-state">
        No results{{ filterInput ? ` for "${filterInput}"` : '' }}
      </div>
      <template v-else>
        <div
          v-for="group in groups"
          :key="group.iconName"
          class="icon-group-card"
          :class="{ 'is-fallback': group.iconName === FALLBACK }"
        >
          <div class="icon-group-header" @click="toggle(group.iconName)">
            <i :class="`mdi mdi-${group.iconName}`" class="icon-big-preview" />
            <div class="icon-group-meta">
              <code class="icon-name-code">{{ group.iconName }}</code>
              <span class="icon-count-badge">{{ group.count }}</span>
            </div>
            <div class="icon-group-preview-pills">
              <span
                v-for="ex in group.examples.slice(0, 4)"
                :key="ex.name"
                :class="['mini-pill', ex.type === 'item' ? 'mini-pill-item' : 'mini-pill-loc']"
              >{{ ex.name }}</span>
              <span v-if="group.count > 4" class="mini-pill mini-pill-more">+{{ group.count - 4 }} more</span>
            </div>
            <i :class="['mdi', expanded.has(group.iconName) ? 'mdi-chevron-up' : 'mdi-chevron-down']" class="icon-expand-toggle" />
          </div>

          <div v-if="expanded.has(group.iconName)" class="icon-group-examples">
            <div class="example-columns">
              <span
                v-for="ex in group.examples"
                :key="ex.name + ex.type"
                :class="['example-pill', ex.type === 'item' ? 'example-pill-item' : 'example-pill-loc']"
              >
                <i :class="`mdi mdi-${group.iconName}`" style="font-size: 0.75rem; opacity: 0.6;" />
                {{ ex.name }}
                <span class="example-game">{{ ex.game }}</span>
              </span>
            </div>
          </div>
        </div>
      </template>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue'
import { getIcons } from '../api.js'

const props = defineProps({ channelId: String })

const PAGE_SIZE = 50
const FALLBACK = 'help-circle-outline'

const loading = ref(true)
const error = ref(null)
const groups = ref([])
const totalGroups = ref(0)
const totalAssignments = ref(0)
const page = ref(0)
const filterInput = ref('')
const filter = ref('')
const expanded = ref(new Set())

const totalPages = computed(() => Math.max(1, Math.ceil(totalGroups.value / PAGE_SIZE)))
const hasFallback = computed(() => groups.value.some(g => g.iconName === FALLBACK))

let filterTimer = null
watch(filterInput, val => {
  clearTimeout(filterTimer)
  filterTimer = setTimeout(() => {
    filter.value = val
    page.value = 0
  }, 300)
})

function toggle(iconName) {
  const s = new Set(expanded.value)
  s.has(iconName) ? s.delete(iconName) : s.add(iconName)
  expanded.value = s
}

async function load() {
  loading.value = true
  error.value = null
  try {
    const data = await getIcons(props.channelId, {
      page: page.value,
      pageSize: PAGE_SIZE,
      q: filter.value || null
    })
    groups.value = data.groups
    totalGroups.value = data.totalGroups
    totalAssignments.value = data.totalAssignments
  } catch (e) {
    error.value = e.message
  } finally {
    loading.value = false
  }
}

watch([page, filter, () => props.channelId], load)
onMounted(load)
</script>

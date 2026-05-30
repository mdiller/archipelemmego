<template>
  <div class="home-view">
    <div class="home-card">
      <div class="home-logo">
        <i class="mdi mdi-island" style="font-size: 2rem;" />
        <span class="home-title">ArchipeLemmeGo</span>
      </div>
      <p class="home-subtitle">Track your Archipelago randomizer session</p>
      <div class="search-row">
        <InputText
          v-model="channelInput"
          placeholder="Paste your Discord channel ID..."
          class="channel-input"
          @keyup.enter="go"
          autofocus
        />
        <Button label="Go" @click="go" />
      </div>
      <p v-if="error" class="home-error">{{ error }}</p>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'

const router = useRouter()
const channelInput = ref('')
const error = ref('')

function go() {
  const id = channelInput.value.trim()
  if (!id) {
    error.value = 'Please enter a channel ID.'
    return
  }
  if (!/^\d+$/.test(id)) {
    error.value = 'Channel ID must be a numeric Discord channel ID.'
    return
  }
  error.value = ''
  router.push(`/channel/${id}`)
}
</script>

<style scoped>
.home-view {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: #0d1117;
}

.home-card {
  background: #161b22;
  border: 1px solid #30363d;
  border-radius: 12px;
  padding: 2.5rem 3rem;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 1rem;
  max-width: 480px;
  width: 100%;
}

.home-logo {
  display: flex;
  align-items: center;
  gap: 0.6rem;
  color: #58a6ff;
}

.home-title {
  font-size: 1.5rem;
  font-weight: 600;
  color: #c9d1d9;
}

.home-subtitle {
  color: #6e7681;
  font-size: 0.9rem;
  margin: 0;
}

.search-row {
  display: flex;
  gap: 0.5rem;
  width: 100%;
}

.channel-input {
  flex: 1;
}

.home-error {
  color: #f85149;
  font-size: 0.85rem;
  margin: 0;
}
</style>

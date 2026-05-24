<template>
  <div class="login-wrap">
    <div v-if="loading" class="login-skeleton" />
    <Button
      v-else-if="!user"
      size="small"
      outlined
      severity="secondary"
      class="discord-login-btn"
      @click="login()"
    >
      <i class="mdi mdi-discord" />
      Login
    </Button>
    <div v-else class="user-chip">
      <img :src="user.avatarUrl" class="user-avatar" alt="" />
      <span class="user-name">{{ user.username }}</span>
      <button class="logout-btn" title="Logout" @click="doLogout">
        <i class="mdi mdi-logout" />
      </button>
    </div>
  </div>
</template>

<script setup>
import { useAuth } from '../composables/useAuth.js'

const { user, loading, login, logout } = useAuth()

async function doLogout() {
  await logout()
}
</script>

<style scoped>
.login-wrap {
  display: flex;
  align-items: center;
}

.login-skeleton {
  width: 80px;
  height: 28px;
  border-radius: 6px;
  background: #30363d;
  animation: pulse 1.2s ease-in-out infinite;
}

@keyframes pulse {
  0%, 100% { opacity: 1; }
  50% { opacity: 0.4; }
}

.discord-login-btn {
  gap: 0.35rem;
}

.user-chip {
  display: flex;
  align-items: center;
  gap: 0.4rem;
  background: #21262d;
  border: 1px solid #30363d;
  border-radius: 6px;
  padding: 3px 8px 3px 4px;
  font-size: 0.82rem;
}

.user-avatar {
  width: 20px;
  height: 20px;
  border-radius: 50%;
  object-fit: cover;
}

.user-name {
  color: #c9d1d9;
  max-width: 120px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.logout-btn {
  background: none;
  border: none;
  cursor: pointer;
  color: #6e7681;
  padding: 0 0 0 2px;
  font-size: 0.85rem;
  display: flex;
  align-items: center;
  transition: color 0.15s;
}

.logout-btn:hover {
  color: #f85149;
}
</style>

import { ref, computed } from 'vue'

const user = ref(null)
const loading = ref(true)
let initialized = false

export function useAuth() {
  async function fetchMe() {
    loading.value = true
    try {
      const res = await fetch('/auth/me')
      user.value = res.ok ? await res.json() : null
    } catch {
      user.value = null
    } finally {
      loading.value = false
    }
  }

  function init() {
    if (initialized) return
    initialized = true
    fetchMe()
  }

  function login(returnUrl = location.pathname + location.search) {
    window.location.href = `/auth/login?returnUrl=${encodeURIComponent(returnUrl)}`
  }

  async function logout() {
    await fetch('/auth/logout', { method: 'POST' })
    user.value = null
  }

  const isLoggedIn = computed(() => !!user.value)

  return { user, loading, isLoggedIn, init, login, logout }
}

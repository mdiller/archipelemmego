export function displayName(name) {
  return name.replace(/\[1\]/g, '').trim()
}

async function apiFetch(url) {
  const res = await fetch(url)
  if (!res.ok) throw new Error(res.status === 404 ? 'Channel not found' : `Error ${res.status}`)
  return res.json()
}

function buildUrl(path, params) {
  const url = new URL(path, location.origin)
  for (const [k, v] of Object.entries(params)) {
    if (v !== null && v !== undefined) url.searchParams.set(k, v)
  }
  return url.toString()
}

export function getRoom(channelId) {
  return apiFetch(`/api/${channelId}/room`)
}

export function getWaiting(channelId, slot = null) {
  return apiFetch(buildUrl(`/api/${channelId}/waiting`, { slot }))
}

export function getTodo(channelId, slot = null) {
  return apiFetch(buildUrl(`/api/${channelId}/todo`, { slot }))
}

export function searchItems(channelId, q, slot = null) {
  return apiFetch(buildUrl(`/api/${channelId}/items`, { q, slot }))
}

export function searchLocations(channelId, q, slot = null) {
  return apiFetch(buildUrl(`/api/${channelId}/locations`, { q, slot }))
}

export function getDeps(channelId) {
  return apiFetch(`/api/${channelId}/deps`)
}

export function getIcons(channelId, { page = 0, pageSize = 50, q = null } = {}) {
  return apiFetch(buildUrl(`/api/${channelId}/icons`, { page, pageSize, q }))
}

export async function updateHintInfo(channelId, requesterSlot, itemId, locationId, information) {
  const res = await fetch(`/api/${channelId}/hints/${requesterSlot}/${itemId}/${locationId}/info`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ information })
  })
  if (!res.ok) throw new Error(`Error ${res.status}`)
}

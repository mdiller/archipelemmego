<template>
  <div class="dep-graph-container">
    <div class="dep-helper-banner">
      <i class="mdi mdi-information-outline" style="color: #58a6ff; font-size: 1rem;" />
      <span>Build your dependency graph in Discord:</span>
      <code class="dep-cmd">/dep add [location] [item]</code>
      <code class="dep-cmd">/dep addregex [location-regex] [item-regex]</code>
      <code class="dep-cmd">/dep show</code>
      <span class="dep-hint">· Scroll to zoom · Drag to pan · Drag nodes to pin</span>
    </div>

    <div ref="wrapper" class="dep-svg-wrapper">
      <div v-if="loading" class="loading-state">
        <ProgressSpinner style="width: 2rem; height: 2rem;" />
        Loading dependencies...
      </div>
      <div v-else-if="error" class="error-state">
        <i class="mdi mdi-alert-circle" />
        {{ error }}
      </div>
      <div v-else-if="!nodes.length" class="empty-state dep-empty">
        <i class="mdi mdi-graph-outline" style="font-size: 3rem; color: #30363d; margin-bottom: 0.5rem;" />
        <p style="margin: 0; color: #6e7681;">No dependencies defined yet.</p>
        <p style="margin: 0.4rem 0 0; color: #8b949e; font-size: 0.82rem;">
          Use <code class="dep-cmd">/dep add</code> or <code class="dep-cmd">/dep addregex</code> in Discord to add some.
        </p>
      </div>
      <svg v-else ref="svgEl" class="dep-svg"></svg>
    </div>

    <div v-if="nodes.length && !loading" class="dep-legend">
      <span class="dep-legend-item">
        <svg width="14" height="14" style="flex-shrink:0">
          <rect x="1" y="1" width="12" height="12" rx="3" fill="#111e2e" stroke="#58a6ff" stroke-width="1.5"/>
        </svg>
        Item (prerequisite)
      </span>
      <span class="dep-legend-item">
        <svg width="14" height="14" style="flex-shrink:0">
          <rect x="1" y="1" width="12" height="12" rx="3" fill="#1a1228" stroke="#a371f7" stroke-width="1.5"/>
        </svg>
        Location (unlocked by)
      </span>
      <span class="dep-count">{{ nodes.length }} nodes · {{ edges.length }} edges</span>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted, onUnmounted, watch, nextTick } from 'vue'
import * as d3 from 'd3'
import { getDeps } from '../api.js'

const props = defineProps({ channelId: String })

const wrapper = ref(null)
const svgEl = ref(null)
const loading = ref(true)
const error = ref(null)
const nodes = ref([])
const edges = ref([])

const NODE_W = 180
const LINE_H = 15
const NODE_PAD_V = 10
const ICON_H = 22
const WRAP_CHARS = 22
const MAX_LINES = 3

function wrapText(text) {
  const words = text.split(' ')
  const lines = []
  let line = ''
  for (const word of words) {
    if (lines.length >= MAX_LINES) break
    if (!line) {
      line = word
    } else if (line.length + 1 + word.length <= WRAP_CHARS) {
      line += ' ' + word
    } else {
      lines.push(line)
      line = word
    }
  }
  if (line && lines.length < MAX_LINES) {
    if (lines.length === MAX_LINES - 1 && line.length > WRAP_CHARS) {
      line = line.substring(0, WRAP_CHARS - 3) + '...'
    }
    lines.push(line)
  }
  return lines.length ? lines : [text.substring(0, WRAP_CHARS)]
}

function calcNodeH(lines) {
  return Math.max(64, ICON_H + NODE_PAD_V + (lines.length + 1) * LINE_H + NODE_PAD_V)
}

function rectBoundary(cx, cy, hw, hh, angle) {
  const cos = Math.cos(angle)
  const sin = Math.sin(angle)
  const tx = Math.abs(cos) > 1e-6 ? hw / Math.abs(cos) : Infinity
  const ty = Math.abs(sin) > 1e-6 ? hh / Math.abs(sin) : Infinity
  const t = Math.min(tx, ty)
  return [cx + cos * t, cy + sin * t]
}

async function loadAndRender() {
  loading.value = true
  error.value = null
  nodes.value = []
  edges.value = []
  try {
    const data = await getDeps(props.channelId)
    nodes.value = data.nodes || []
    edges.value = data.edges || []
  } catch (e) {
    error.value = e.message
    loading.value = false
    return
  }
  loading.value = false
  if (nodes.value.length) {
    await nextTick()
    renderGraph()
  }
}

function renderGraph() {
  if (!svgEl.value || !wrapper.value) return

  const W = wrapper.value.clientWidth || 800
  const H = wrapper.value.clientHeight || 600

  const svg = d3.select(svgEl.value)
  svg.selectAll('*').remove()
  svg.attr('width', W).attr('height', H)

  const defs = svg.append('defs')

  defs.append('marker')
    .attr('id', 'dep-arrow')
    .attr('viewBox', '0 0 10 10')
    .attr('refX', 9)
    .attr('refY', 5)
    .attr('markerWidth', 7)
    .attr('markerHeight', 7)
    .attr('orient', 'auto')
    .append('path')
    .attr('d', 'M 0 0 L 10 5 L 0 10 z')
    .attr('fill', '#4a8fd4')
    .attr('fill-opacity', 0.75)

  const addGlow = (id, stdDev) => {
    const f = defs.append('filter')
      .attr('id', id)
      .attr('x', '-50%').attr('y', '-50%')
      .attr('width', '200%').attr('height', '200%')
    f.append('feGaussianBlur').attr('stdDeviation', stdDev).attr('result', 'blur')
    const m = f.append('feMerge')
    m.append('feMergeNode').attr('in', 'blur')
    m.append('feMergeNode').attr('in', 'SourceGraphic')
  }
  addGlow('glow-item', 4)
  addGlow('glow-loc', 4)

  const nodeData = nodes.value.map(n => {
    const lines = wrapText(n.name)
    return { ...n, lines, h: calcNodeH(lines), w: NODE_W }
  })

  const nodeById = Object.fromEntries(nodeData.map(n => [n.id, n]))

  const linkData = edges.value
    .filter(e => nodeById[e.source] && nodeById[e.target])
    .map(e => ({ source: nodeById[e.source], target: nodeById[e.target] }))

  const g = svg.append('g')
  svg.call(
    d3.zoom().scaleExtent([0.1, 4]).on('zoom', ev => g.attr('transform', ev.transform))
  )

  const linkEl = g.append('g')
    .selectAll('path')
    .data(linkData)
    .join('path')
    .attr('fill', 'none')
    .attr('stroke', '#4a8fd4')
    .attr('stroke-width', 1.5)
    .attr('stroke-opacity', 0.5)
    .attr('marker-end', 'url(#dep-arrow)')

  const nodeEl = g.append('g')
    .selectAll('g')
    .data(nodeData)
    .join('g')
    .call(
      d3.drag()
        .on('start', (ev, d) => { if (!ev.active) sim.alphaTarget(0.3).restart(); d.fx = d.x; d.fy = d.y })
        .on('drag', (ev, d) => { d.fx = ev.x; d.fy = ev.y })
        .on('end', (ev, d) => { if (!ev.active) sim.alphaTarget(0); d.fx = null; d.fy = null })
    )

  nodeEl.append('rect')
    .attr('x', d => -d.w / 2)
    .attr('y', d => -d.h / 2)
    .attr('width', d => d.w)
    .attr('height', d => d.h)
    .attr('rx', 8)
    .attr('fill', d => d.type === 'item' ? '#111e2e' : '#1a1228')
    .attr('stroke', d => d.type === 'item' ? '#58a6ff' : '#a371f7')
    .attr('stroke-width', 1.5)
    .attr('filter', d => d.type === 'item' ? 'url(#glow-item)' : 'url(#glow-loc)')

  nodeEl.each(function(d) {
    const sel = d3.select(this)
    const iconName = d.iconName || 'help-circle-outline'
    const color = d.type === 'item' ? '#93c5fd' : '#c4b5fd'

    // Icon via foreignObject so MDI CSS classes apply
    sel.append('foreignObject')
      .attr('x', -9)
      .attr('y', -d.h / 2 + 3)
      .attr('width', 18)
      .attr('height', 18)
      .append('xhtml:body')
      .style('margin', '0').style('padding', '0').style('background', 'transparent')
      .append('xhtml:i')
      .attr('class', `mdi mdi-${iconName}`)
      .style('font-size', '13px')
      .style('color', color)
      .style('display', 'block')
      .style('text-align', 'center')
      .style('line-height', '18px')

    // Name lines — shifted down to sit below the icon
    const startY = -d.h / 2 + ICON_H + NODE_PAD_V + LINE_H * 0.85
    d.lines.forEach((line, i) => {
      sel.append('text')
        .attr('x', 0).attr('y', startY + i * LINE_H)
        .attr('text-anchor', 'middle')
        .attr('font-size', '11px')
        .attr('font-weight', '600')
        .attr('fill', color)
        .attr('font-family', "'Segoe UI', system-ui, sans-serif")
        .text(line)
    })

    sel.append('text')
      .attr('x', 0).attr('y', startY + d.lines.length * LINE_H)
      .attr('text-anchor', 'middle')
      .attr('font-size', '9.5px')
      .attr('fill', '#4e5560')
      .attr('font-family', "'Segoe UI', system-ui, sans-serif")
      .text(`(${d.slotName})`)
  })

  const sim = d3.forceSimulation(nodeData)
    .force('link', d3.forceLink(linkData).id(d => d.id).distance(240).strength(0.5))
    .force('charge', d3.forceManyBody().strength(-500))
    .force('center', d3.forceCenter(W / 2, H / 2))
    .force('collide', d3.forceCollide(d => Math.max(d.w, d.h) / 2 + 18))
    .force('x', d3.forceX(d => d.type === 'item' ? W * 0.28 : W * 0.72).strength(0.08))

  sim.on('tick', () => {
    linkEl.attr('d', d => {
      const sx = d.source.x, sy = d.source.y
      const tx = d.target.x, ty = d.target.y
      const dx = tx - sx, dy = ty - sy
      const len = Math.sqrt(dx * dx + dy * dy) || 1
      const angle = Math.atan2(dy, dx)

      const [x1, y1] = rectBoundary(sx, sy, d.source.w / 2, d.source.h / 2, angle)
      const [x2, y2] = rectBoundary(tx, ty, d.target.w / 2, d.target.h / 2, angle + Math.PI)

      const mx = (x1 + x2) / 2
      const my = (y1 + y2) / 2
      const curve = len * 0.12
      const cx = mx - (dy / len) * curve
      const cy = my + (dx / len) * curve

      return `M${x1},${y1} Q${cx},${cy} ${x2},${y2}`
    })

    nodeEl.attr('transform', d => `translate(${d.x},${d.y})`)
  })
}

let resizeObs = null
onMounted(() => {
  loadAndRender()
  resizeObs = new ResizeObserver(() => {
    if (!loading.value && !error.value && nodes.value.length) renderGraph()
  })
  if (wrapper.value) resizeObs.observe(wrapper.value)
})
onUnmounted(() => { if (resizeObs) resizeObs.disconnect() })
watch(() => props.channelId, loadAndRender)
</script>

import { createRouter, createWebHistory } from 'vue-router'
import ChannelView from '../views/ChannelView.vue'

export default createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/:channelId', component: ChannelView },
    { path: '/', redirect: '/invalid' }
  ]
})

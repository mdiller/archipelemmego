import { createApp } from 'vue'
import PrimeVue from 'primevue/config'
import Aura from '@primeuix/themes/aura'
import { definePreset } from '@primeuix/themes'

const ArchipelagoPreset = definePreset(Aura, {
  semantic: {
    colorScheme: {
      dark: {
        surface: {
          0:   '#ffffff',
          50:  '#f0f6fc',
          100: '#c9d1d9',
          200: '#b1bac4',
          300: '#8b949e',
          400: '#6e7681',
          500: '#484f58',
          600: '#30363d',
          700: '#21262d',
          800: '#161b22',
          900: '#0d1117',
          950: '#010409'
        }
      }
    }
  }
})
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Select from 'primevue/select'
import InputText from 'primevue/inputtext'
import Button from 'primevue/button'
import Tag from 'primevue/tag'
import ProgressSpinner from 'primevue/progressspinner'
import 'primeicons/primeicons.css'
import '@mdi/font/css/materialdesignicons.min.css'
import './style.css'
import App from './App.vue'
import router from './router/index.js'

const app = createApp(App)

app.use(router)
app.use(PrimeVue, {
  theme: {
    preset: ArchipelagoPreset,
    options: {
      darkModeSelector: '.p-dark',
      cssLayer: false
    }
  }
})

app.component('DataTable', DataTable)
app.component('Column', Column)
app.component('Select', Select)
app.component('InputText', InputText)
app.component('Button', Button)
app.component('Tag', Tag)
app.component('ProgressSpinner', ProgressSpinner)

document.documentElement.classList.add('p-dark')

app.mount('#app')

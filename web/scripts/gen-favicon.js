const sharp = require('sharp')
const fs = require('fs')
const path = require('path')

const src = path.join(__dirname, '../public/favicon.svg')
const dst = path.join(__dirname, '../public/favicon.png')

sharp(fs.readFileSync(src)).resize(32, 32).png().toFile(dst, err => {
  if (err) { console.error('favicon gen failed:', err.message); process.exit(1) }
  console.log('favicon.png generated')
})

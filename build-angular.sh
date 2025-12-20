#!/bin/bash

# Script para construir la aplicación Angular

echo "🔨 Building Angular application..."

cd angular-app

# Verificar si node_modules existe
if [ ! -d "node_modules" ]; then
    echo "📦 Installing dependencies..."
    npm install
fi

# Construir la aplicación
echo "🏗️  Building for production..."
npm run build

echo "✅ Angular build completed! Output: ../wwwroot"

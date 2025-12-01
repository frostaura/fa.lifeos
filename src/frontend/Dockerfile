# Build stage
FROM node:20-alpine AS build
WORKDIR /app

# Copy package files
COPY package*.json ./

# Install dependencies
RUN npm install

# Copy source code
COPY . .

# Build the app
RUN npm run build

# Runtime stage - use node for development server
FROM node:20-alpine AS runtime
WORKDIR /app

# Copy package files and install only production deps for serve
COPY package*.json ./
RUN npm install

# Copy all source for vite dev server
COPY . .

EXPOSE 5173

# Run vite dev server with host flag
CMD ["npm", "run", "dev", "--", "--host", "0.0.0.0"]

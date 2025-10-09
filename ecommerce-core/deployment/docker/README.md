# Docker Configuration

This directory contains Docker configurations for containerizing the e-commerce application.

## Files

- `Dockerfile` - Multi-stage build for .NET application
- `docker-compose.yml` - Local development environment
- `docker-compose.prod.yml` - Production configuration
- `.dockerignore` - Docker build exclusions

## Features

- Multi-stage build optimization
- SQL Server container for local development
- Environment variable configuration
- Health check implementation
- Production-ready security settings
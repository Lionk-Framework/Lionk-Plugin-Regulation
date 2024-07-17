# Lionk-Plugin-Regulation for Lionk Framework

## Introduction
This project aims to create a regulation system for a 29 kW accumulation fireplace. The system should allow temperature regulation and energy transfer to two thermal accumulators. This project is developed using .NET 8.0 or higher, with a web interface developed in Blazor.

## Objectives
- Migrate the regulation system from Python to .NET.
- Add a regulation system to avoid sudden temperature variations.
- Add secure access to data from anywhere via a web platform.
- Add a notification system in case of system failure or overheating.
- Add an automatic startup system for the program.
- Enable secure communication between mobile widgets and the installation.
- Implement a monitoring and logging system for the installation.
- Implement an automatic software component update system.

## Prerequisites
- .NET 8.0 or higher
- Raspberry Pi 4B 4Gb
- Linux server
- Blazor Server
- Docker for containerization
- GitHub Actions for continuous updates

## Server Configuration
1. Install .NET 8.0 on the Raspberry Pi and the Linux server.
2. Configure the server to run the necessary programs for temperature regulation and notifications.

## Database
Set up a database optimized for time-series data storage.

## Deployment
Use Docker to containerize the applications and GitHub Actions for automatic deployments.

## Automatic Startup
Configure the Raspberry Pi to automatically start the programs on boot.

## Usage

### Data Access
Temperature data can be viewed in real-time from the web interface. Access is secured and requires authentication.

### Notifications
The system will send notifications in case of energy transport system failure or overheating. Notifications can be configured to use an external service.


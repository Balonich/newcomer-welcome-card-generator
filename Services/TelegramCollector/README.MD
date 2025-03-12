# TelegramCollector Service

## Overview

This service collects newcomer information from a Telegram Bot and publishes the structured data to the CardGenerationQueue for processing.

## Features

- Connects to the Telegram Bot API
- Processes incoming messages from users
- Parses structured newcomer data
- Publishes data to the CardGenerationQueue via RabbitMQ

## Configuration

The service requires the following configuration in `appsettings.json`:

```json
{
  "Telegram": {
    "BotToken": "YOUR_BOT_TOKEN_HERE",
    "WebhookUrl": "https://your-domain.com/api/telegram/webhook",
    "SecretToken": "YOUR_SECRET_TOKEN_HERE"
  },
  "RabbitMQ": {
    "HostName": "localhost",
    "UserName": "guest",
    "Password": "guest",
    "Port": 5672
  }
}
```

## Getting Started

1. Create a new Telegram Bot using BotFather
2. Configure the `appsettings.json` with your bot token
3. Set up a publicly accessible endpoint for the webhook OR use long polling
4. Run the service

## Development Mode

In development mode, set `WebhookUrl` to null in `appsettings.Development.json` to disable webhook setup. Long polling will be used instead.

## Message Format

The service expects newcomer messages in the following format:

```
/newcomer Name|Position|Department|Bio|PhotoUrl|Hobby1,Hobby2
```

Example:
```
/newcomer John Doe|Software Engineer|Engineering|I love coding!|https://example.com/photo.jpg|Reading,Gaming,Hiking
```
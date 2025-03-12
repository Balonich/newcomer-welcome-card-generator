# Overview

The project is an automated system for generating and distributing newcomer introduction cards in a company. It follows a microservices architecture, where each service is responsible for a specific task and communicates through a message broker. The system does not store data persistently, processing each request in real time.

The workflow begins with data collection. Newcomer information might be gathered from different sources so there should be a set of interfaces, bases/abstract classes that must be used to implement different source collection. The only service that is going to be implemented right now is TelegramCollector, which parses messages from Telegram Bot. Any type of Collector service should then publish structured newcomer data to the CardGenerationQueue.

The CardGeneration API listens to the CardGenerationQueue and receives the newcomer data. It establishes a WebSocket connection with the frontend, where it sends the received data to be rendered. The frontend, built with HTML, CSS, and JavaScript, dynamically fills a predefined template and converts it into an image. The generated image is then sent back to the CardGeneration API.

Once the image is received, the CardGeneration API publishes it to the ImageQueue. The ImageSender service listens to this queue and retrieves the image along with the newcomer's details. Depending on the configuration, it sends the generated introduction card via Slack, Microsoft Teams, or Telegram using their respective APIs. For now only Telegram API should be implemented.

All communication between backend services is handled asynchronously through a message broker RabbitMQ, ensuring decoupling and scalability. WebSockets are used for real-time communication between the CardGeneration API and the frontend, allowing instant updates when new data is received. The system is designed to be modular, where each component can be replaced or extended independently without affecting the overall workflow.

# TODO

## Phase 1
- [ ] Create interfaces and base classes in the Common project for:
  - [ ] Data collection sources
  - [X] Message queue abstractions
  - [X] Shared models for newcomer data

## Phase 2
- [ ] Implement the TelegramCollector service to:
  - [ ] Connect to the Telegram Bot API
  - [ ] Parse incoming messages
  - [ ] Publish structured data to CardGenerationQueue

## Phase 3 
- [ ] Enhance CardGeneration service to:
  - [ ] Set up WebSockets for frontend communication
  - [ ] Process data from the queue
  - [ ] Handle image reception and publish to ImageQueue

## Phase 4
- [ ] Develop ImageSender to:
  - [ ] Connect to Telegram API
  - [ ] Process images from the queue
  - [ ] Send images to configured channels
  - [ ] Set up RabbitMQ integration for inter-service communication
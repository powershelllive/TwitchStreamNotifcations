# TwitchStreamNotifications

This is an Azure Functions project which allows for notifications to services such as twitter, slack, and discord when a streaming channel has gone online.

## Architecture

Subscribed Twitch webhooks will submit to the `TwitchWebhookIngestion` function.
The `TwitchWebhookIngestion` is responsible to validating the webhook payload and then queues it in the `TwitchStreamActivity` queue.
The `TwitchStreamEventHandler` is triggered by the `TwitchStreamActivity` and is responsible for directing the event to notification handler queues.
For now, the `TwitchStreamEventHandler` only queues the event to the `TwitterNotifications` queue.
The `TwitterEventHandler` function is triggered by the `TwitterNotifications` and will create a tweet stating a channel has gone live.

## Functions

### TwitchWebhookIngestion

* Trigger: Http
* Inputs: HttpRequest, StreamName
* Route: `TwitchWebhookIngestion/{StreamName}`
* Output: `TwitchStreamActivity` queue

This is the endpoint where Twitch submits webhook payloads.
It is responsible for validating the payload against the provided hash and the calculated hash using the `TwitchSubscriptionsHashSecret`.
Valid webhook payloads are then enqueued in the `TwitchStreamEventHandler queue.

### TwitchStreamEventHandler

* Trigger: `TwitchStreamActivity` queue
* Output: `TwitterNotifications` queue

This function is responsible for routing Twitch Stream events to various event handlers.
Currently, the only event handler is `TwitterEventHandler`.

### TwitterEventHandler

* Trigger: `TwitterNotifications` queue

This function is an Event handler for Twitter.
Currently, the only action it takes is to create new tweets when a Twitch Stream has gone live.

## Application Settings

* `TwitchStreamStorage` - Storage connection string to use for Queues
* `TwitchSubscriptionsHashSecret` - Secret used when subscribing to web hooks. This is used to perform a HMAC SHA265 signature check on the webhook.
* `TwitchStreamActivity` - Queue name for Twitch Stream Events.
* `TwitterNotifications` - Queue name for Twitter Events.
* `TwitterConsumerKey` - Twitter App Consumer API Key.
* `TwitterConsumerSecret` - Twitter App Consumer API secret.
* `TwitterAccessToken` - Twitter Access token for the user that wills send out tweets.
* `TwitterAccessTokenSecret` - Twitter Access Token Secret for the user that wills send out tweets.
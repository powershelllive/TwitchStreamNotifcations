# TwitchStreamNotifications

This is an Azure Functions project which allows for notifications to services such as twitter, slack, and discord when a streaming channel has gone live.

## Architecture

### Webhook

Subscribed Twitch webhooks will submit to the `TwitchWebhookIngestion` function.
The `TwitchWebhookIngestion` is responsible to validating the webhook payload and then queues it in the `TwitchStreamActivity` queue.
The `TwitchStreamEventHandler` is triggered by the `TwitchStreamActivity` and is responsible for directing the event to notification handler queues.
For now, the `TwitchStreamEventHandler` only queues the event to the `TwitterNotifications` and `DiscordNotifications` queues.
The `TwitterEventHandler` function is triggered by the `TwitterNotifications` and will create a tweet stating a channel has gone live.
The `DiscordEventHandler` function is triggered by the `DiscordNotifications` and will create a discord message stating a channel has gone live.

### Subscription Management

A JSON payload like the one in [Subscriptions.json](./config/Subscriptions.json) will be posted to `TwitchSubscriptionRegistration` function using the function key.
The `TwitchSubscriptionRegistration` will verify the requested subscriptions against the current subscriptions registered with Twitch.
Requested subscriptions that are missing from the current registered subscriptions will be added to the `TwitchSubscribeQueue` queue.
Currently registered subscriptions that are not included in the requested subscriptions will be added to the `TwitchUnsubscribeQueue` queue.

The `TwitchSubscriptionAdd` will be triggered by the `TwitchSubscribeQueue` and will send a subscribe request to the Twitch API.

The `TwitchSubscriptionRemove` will be triggered by the `TwitchUnsubscribeQueue` and will send an unsubscribe request to the Twitch API.

Subscribe and Unsubscribe requests to the Twitch API will result in the Twitch API making a callback to the `TwitchWebhookIngestion` which will be responsible for returning the `hub.challenge` query parameter back to the Twitch API.

## Functions

### Webhook Functions

#### TwitchWebhookIngestion

* Trigger: Http
* Inputs: HttpRequest, StreamName, TwitterName
* Route: `TwitchWebhookIngestion/{StreamName}/{TwitterName?}`
* Output: `TwitchStreamActivity` queue

This is the endpoint where Twitch submits webhook payloads.
It is responsible for validating the payload against the provided hash and the calculated hash using the `TwitchSubscriptionsHashSecret`.
Valid webhook payloads are then enqueued in the `TwitchStreamEventHandler queue.

#### TwitchStreamEventHandler

* Trigger: `TwitchStreamActivity` queue
* Output: `TwitterNotifications` queue, `DiscordNotifications` queue

This function is responsible for routing Twitch Stream events to various event handlers.
Currently, the only event handlers are `TwitterEventHandler` and `DiscordEventHandler`.

#### TwitterEventHandler

* Trigger: `TwitterNotifications` queue

This function is an Event handler for Twitter.
Currently, the only action it takes is to create new tweets when a Twitch Stream has gone live.

#### DiscordEventHandler

* Trigger: `DiscordNotifications` queue

This function is an Event handler for Discord.
Currently, the only action it takes is to create new Discord Messages when a Twitch Stream has gone live.

### Subscription Management Functions

#### TwitchSubscriptionRegistration

* Trigger: Http
* Inputs: HttpRequest (Json payload [Subscriptions.json](./config/Subscriptions.json))
* Route: `TwitchSubscriptionRegistration`
* Output: `TwitchSubscribeQueue` queue, `TwitchUnsubscribeQueue`

Provides idempotent Twitch webhook subscription registrations based on the JSON payload.
The function queries the Twitch API for currently registered subscriptions and compares them to the requested subscriptions.
Missing subscriptions will be added to the `TwitchSubscribeQueue`.
Extra subscriptions will be added to the `TwitchUnsubscribeQueue`.

#### TwitchSubscriptionAdd

* Trigger: `TwitchSubscribeQueue`

Subscriptions from the `TwitchSubscribeQueue` will be sent to the Twitch API to be subscribed.

#### TwitchSubscriptionRemove

* Trigger: `TwitchUnsubscribeQueue`

Subscriptions from the `TwitchUnsubscribeQueue` will be sent to the Twitch API to be unsubscribed.

## Application Settings

* `TwitchStreamStorage` - Storage connection string to use for Queues
* `TwitchSubscriptionsHashSecret` - Secret used when subscribing to web hooks. This is used to perform a HMAC SHA265 signature check on the webhook.
* `TwitchStreamActivity` - Queue name for Twitch Stream Events.
* `TwitterNotifications` - Queue name for Twitter Events.
* `TwitterConsumerKey` - Twitter App Consumer API Key.
* `TwitterConsumerSecret` - Twitter App Consumer API secret.
* `TwitterAccessToken` - Twitter Access token for the user that wills send out tweets.
* `TwitterAccessTokenSecret` - Twitter Access Token Secret for the user that wills send out tweets.
* `DISABLE_NOTIFICATIONS` - When set to `true`, notification event handlers (e.g. `TwitterEventHandler`) will not perform notification actions. Used for troubleshooting and debugging.
* `TwitterTweetTemplate` - String format template. Called with `string.Format(TwitterTweetTemplate, streamUri, username, (UTCDateTime), game);` where `streamUri` is the URL to the twitch stream and the `username` is either the twitch stream or twitter handle (if a twitter handle was provided when registering).
* `TwitchSubscribeQueue` - Storage Queue Name for twitch subscriptions to add
* `TwitchUnsubscribeQueue` - Storage Queue Name for twitch subscriptions to remove
* `TwitchClientId` - Twitch APP Client Id used for authenticating to Twitch API
* `TwitchClientSecret` - Twitch APP Client Secret used for authenticating to Twitch API
* `TwitchClientRedirectUri` - Twitch APP ClientRedirect Uri used for authenticating to Twitch API
* `TwitchWebhookBaseUri` - The base URI used for Twitch webhook callbacks. `https://{AzureFuncionsWebAppName}.azurewebsites.net/api/TwitchWebhookIngestion`
* `DiscordWebhookUri` - The URI for the Discord webhook
* `DiscordMessageTemplate` - Message template for Discord messages.
* `DiscordNotifications` - Queue name for Discord Events.
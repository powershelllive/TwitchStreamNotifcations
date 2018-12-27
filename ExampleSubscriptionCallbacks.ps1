<#
GET https://yourwebsite.com/path/to/callback/handler? \
hub.mode=subscribe& \
hub.topic=https://api.twitch.tv/helix/users/follows?first=1&to_id=1337& \
hub.lease_seconds=864000& \
hub.challenge=HzSGH_h04Cgl6VbDJm7IyXSNSlrhaLvBi9eft3bw

GET https://yourwebsite.com/path/to/callback/handler? \
hub.mode=denied& \
hub.topic=https://api.twitch.tv/helix/users/follows?first=1&to_id=1337& \
hub.reason=unauthorized
#>
Enable-Tls -Tls12 -Confirm:$false
$Params = @{
    Uri = @(
        'http://localhost:7071/api/TwitchWebhookIngestion/markekraus?'
        'hub.mode=subscribe&'
        'hub.topic=https://api.twitch.tv/helix/users/follows?first=1&to_id=1337&'
        'hub.lease_seconds=864000&'
        'hub.challenge=HzSGH_h04Cgl6VbDJm7IyXSNSlrhaLvBi9eft3bw'
    ) -join ''
    Method = 'Get'
}
$result = Invoke-WebRequest @Params
$result

$Params = @{
    Uri = @(
        'http://localhost:7071/api/TwitchWebhookIngestion/markekraus?'
        'hub.mode=denied&'
        'hub.topic=https://api.twitch.tv/helix/users/follows?first=1&to_id=1337&'
        'hub.reason=unauthorized'
    ) -join ''
    Method = 'Get'
}
$FailResult = Invoke-WebRequest @Params
$FailResult

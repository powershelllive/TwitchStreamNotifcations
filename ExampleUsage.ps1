$secret = 'testing123'
$hmacsha = New-Object System.Security.Cryptography.HMACSHA256
$hmacsha.key = [Text.Encoding]::ASCII.GetBytes($secret)

$Body = @{
    data = @(
        @{
          "id" = "0123456789"
          "user_id" = "5678"
          "user_name" = "wjdtkdqhs"
          "game_id" = "21779"
          "community_ids" = @()
          "type" = "live"
          "title" = "Best Stream Ever"
          "viewer_count" = 417
          "started_at" = "2017-12-01T10:09:45Z"
          "language" = "en"
          "thumbnail_url" = "https://link/to/thumbnail.jpg"
        }
    )
} | ConvertTo-Json


$Hash = $hmacsha.ComputeHash([Text.Encoding]::ASCII.GetBytes($Body))
$Hash = -join($Hash |ForEach-Object ToString X2)

$StreamName = 'wjdtkdqhs'

$Params = @{
    Uri = "http://localhost:7071/api/TwitchWebhookIngestion/{0}" -f $StreamName
    Method = 'Post'
    Body = $Body
    Headers = @{
        'X-Hub-Signature' = "sha256=$Hash"
    }
    ContentType = 'application/json'
}
Invoke-WebRequest @Params

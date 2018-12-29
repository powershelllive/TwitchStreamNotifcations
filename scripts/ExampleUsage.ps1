Enable-Tls -Tls12 -Confirm:$false
$secret = 'testing123'
$hmacsha = New-Object System.Security.Cryptography.HMACSHA256
$hmacsha.key = [Text.Encoding]::UTF8.GetBytes($secret)

$Body = @{
    data = @(
        @{
          "id" = "0123456789"
          "user_id" = "403106760"
          "user_name" = "markekraus"
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


$Hash = $hmacsha.ComputeHash([Text.Encoding]::UTF8.GetBytes($Body))
$Hash = $(-join($Hash |ForEach-Object ToString X2)).ToLower()

$StreamName = 'markekraus'

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

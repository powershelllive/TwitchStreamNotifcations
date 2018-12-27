function New-TwitchApp {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [pscredential]
        $AppCredentials,

        [uri]
        $RedirectUri
    )

    process {
        [PSCustomObject]@{
            ClientId = $AppCredentials.UserName
            ClientSecret = $AppCredentials.GetNetworkCredential().Password
            RedirectUri = $RedirectUri
        }
    }
}

function Get-TwitchOAuthToken {
    [CmdletBinding()]
    param (
        $TwitchApp,

        [Parameter(DontShow)]
        [string]
        $BaseUri = 'https://id.twitch.tv/oauth2/token'
    )

    process {
        $Params = @{
            uri = $BaseUri
            Body = @{
                'client_id' = $TwitchApp.ClientId
                'client_secret' = $TwitchApp.clientSecret
                'grant_type' = 'client_credentials'
            }
            Method = 'Post'
        }
        $Response = Invoke-RestMethod @Params
        [PSCustomObject]@{
            TwitchApp = $TwitchApp
            AccessToken = $Response.'access_token'
            RefreshToken = $Response.'refresh_token'
        }
    }
}

function Get-TwitchUser {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory, ValueFromPipeline)]
        [ValidateNotNullOrEmpty()]
        [string[]]
        $StreamName,

        [ValidateNotNullOrEmpty()]
        $TwitchApp,

        [Parameter(DontShow)]
        [ValidateNotNullOrEmpty()]
        [string]
        $BaseUri = 'https://api.twitch.tv/helix/users'
    )

    process {
        foreach ($Stream in $StreamName) {
            $Params = @{
                Method = "Get"
                Uri = '{0}?login={1}' -f $BaseUri, $Stream
                Headers = @{
                    'Client-ID' = $TwitchApp.ClientId
                }
            }
            (Invoke-RestMethod @Params).data
        }
    }
}


function Register-TwitchStreamWebhookSubscription {
    [CmdletBinding()]
    param (
        [Parameter(ValueFromPipeline)]
        [ValidateNotNullOrEmpty()]
        [string[]]
        $StreamName,

        [string]
        $HubSecret,

        [ValidateRange(0,864000)]
        [int]
        $HubLeaseSeconds = 0,

        [ValidateNotNullOrEmpty()]
        $TwitchApp,

        [Parameter(DontShow)]
        [ValidateNotNullOrEmpty()]
        [string]
        $WebHookBaseUri = 'https://twitchstreamnotifications.azurewebsites.net/api/TwitchWebhookIngestion'
    )

    process {
        foreach ($Stream in $StreamName) {
            $UserId = (Get-TwitchUser -StreamName $Stream -TwitchAccessToken $TwitchApp).id
            $Body = @{
                "hub.callback" = "{0}/{1}" -f $WebHookBaseUri, $Stream
                "hub.topic" = "https://api.twitch.tv/helix/streams?user_id={0}" -f $UserId
                "hub.mode" = "subscribe"
                "hub.lease_seconds" = $HubLeaseSeconds
            }

            if ($HubSecret) {
                $Body['hub.secret'] = $HubSecret
            }

            $Params = @{
                Uri = 'https://api.twitch.tv/helix/webhooks/hub'
                Method = 'Post'
                Body = $Body | ConvertTo-Json -Depth 10
                Headers = @{
                    'Client-ID' = $TwitchAccessToken.TwitchApp.ClientId
                }
                ContentType = 'application/json'
            }
            $Params
            Invoke-WebRequest @Params
        }
    }
}

$AppCreds = Get-Credential
$HubSecret = Read-Host -AsSecureString
$HubSecret = ([pscredential]::new('foo', $HubSecret)).GetNetworkCredential().Password

$TwitchApp = New-TwitchApp -AppCredentials $AppCreds -RedirectUri 'https://127.0.0.1/'
$Result = Register-TwitchStreamWebhookSubscription -StreamName 'markekraus' -HubSecret $HubSecret -HubLeaseSeconds 0 -TwitchApp $TwitchApp
$Result

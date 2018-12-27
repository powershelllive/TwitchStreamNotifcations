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
        } | Add-Member -MemberType ScriptMethod -Name ToString -Value {"TwitchApp"} -Force -PassThru
    }
}

function Get-TwitchOAuthToken {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
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

        [Parameter(Mandatory)]
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
        [Parameter(Mandatory, ValueFromPipeline)]
        [ValidateNotNullOrEmpty()]
        [string[]]
        $StreamName,

        [string]
        $HubSecret,

        [ValidateRange(0,864000)]
        [int]
        $HubLeaseSeconds = 0,

        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        $TwitchApp,

        [Parameter(DontShow)]
        [ValidateNotNullOrEmpty()]
        [string]
        $WebHookBaseUri = 'https://twitchstreamnotifications.azurewebsites.net/api/TwitchWebhookIngestion'
    )

    process {
        foreach ($Stream in $StreamName) {
            $UserId = (Get-TwitchUser -StreamName $Stream -TwitchApp $TwitchApp).id
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
                    'Client-ID' = $TwitchApp.ClientId
                }
                ContentType = 'application/json'
            }
            $Null = Invoke-WebRequest @Params
            Start-Sleep -Milliseconds 100
            Get-TwitchStreamWebhookSubscription -TwitchApp $TwitchApp | where-object {
                $_.callback -eq $Body.'hub.callback' -and
                $_.topic    -eq $Body.'hub.topic'
            }
        }
    }
}

function Get-TwitchStreamWebhookSubscription {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        $TwitchApp,

        [Parameter()]
        [ValidateRange(1,100)]
        [int]
        $First = 100,

        [Parameter(DontShow)]
        [ValidateNotNullOrEmpty()]
        $BaseUri = 'https://api.twitch.tv/helix/webhooks/subscriptions'
    )

    process {
        $AccessToken = Get-TwitchOAuthToken -TwitchApp $TwitchApp
        $Params = @{
            Uri = '{0}/?first={1}' -f $BaseUri, $First
            Method = 'GET'
            Headers = @{
                'Authorization' = 'Bearer {0}' -f $AccessToken.AccessToken
            }
            ContentType = 'application/json'
        }
        $Result = (Invoke-RestMethod @Params).data
        $Result | ForEach-Object{
            $StreamName = $_.callback -split '/' | Select-Object -Last 1
            $_ | Add-Member -MemberType NoteProperty -Name StreamName -Value $StreamName
            $_ | Add-Member -MemberType NoteProperty -Name TwitchApp -Value $TwitchApp -PassThru
        } 
    }
}

function Unregister-TwitchStreamWebhookSubscription {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory, ValueFromPipelineByPropertyName)]
        [ValidateNotNullOrEmpty()]
        [string]
        $StreamName,

        [string]
        $HubSecret,

        [Parameter(Mandatory, ValueFromPipelineByPropertyName)]
        [ValidateNotNullOrEmpty()]
        $TwitchApp,

        [Parameter(DontShow)]
        [ValidateNotNullOrEmpty()]
        [string]
        $WebHookBaseUri = 'https://twitchstreamnotifications.azurewebsites.net/api/TwitchWebhookIngestion'
    )

    process {
        $UserId = (Get-TwitchUser -StreamName $StreamName -TwitchApp $TwitchApp).id
        $Body = @{
            "hub.callback" = "{0}/{1}" -f $WebHookBaseUri, $StreamName
            "hub.topic" = "https://api.twitch.tv/helix/streams?user_id={0}" -f $UserId
            "hub.mode" = "unsubscribe"
            "hub.lease_seconds" = 864000
        }

        if ($HubSecret) {
            $Body['hub.secret'] = $HubSecret
        }

        Write-verbose ("'{0}'" -f $Body."hub.callback")

        $JsonBody = $Body | ConvertTo-Json -Depth 10

        $Params = @{
            Uri = 'https://api.twitch.tv/helix/webhooks/hub'
            Method = 'Post'
            Body = $JsonBody
            Headers = @{
                'Client-ID' = $TwitchApp.ClientId
            }
            ContentType = 'application/json'
        }
        Invoke-WebRequest @Params
    }
}

$AppCreds = Get-Credential
$HubSecret = Read-Host -AsSecureString
$HubSecret = ([pscredential]::new('foo', $HubSecret)).GetNetworkCredential().Password

$TwitchApp = New-TwitchApp -AppCredentials $AppCreds -RedirectUri 'https://127.0.0.1/'
Get-TwitchUser -StreamName markekraus -TwitchApp $TwitchApp
$Result = Register-TwitchStreamWebhookSubscription -StreamName 'markekraus' -HubSecret $HubSecret -HubLeaseSeconds 0 -TwitchApp $TwitchApp

$Subscriptions = Get-TwitchStreamWebhookSubscription -TwitchApp $TwitchApp -First 100
$Subscriptions | Format-List StreamName, topic, callback
$Subscriptions[0] | Unregister-TwitchStreamWebhookSubscription -Verbose -HubSecret $HubSecret

$Results = @(
    'markekraus'
    'corbob'
    'halbaradkenafin'
    'MrThomasRayner'
    'steviecoaster'
    'PowerShellTeam'
    'tylerleonhardt'
    'potatoqualitee'
    'kevinmarquette'
    'PowerShellDoug'
    'glennsarti'
) | Register-TwitchStreamWebhookSubscription -HubSecret $HubSecret -HubLeaseSeconds (60*60*24) -TwitchApp $TwitchApp

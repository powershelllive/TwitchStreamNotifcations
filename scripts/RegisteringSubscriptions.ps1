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
        [Parameter(Mandatory, ValueFromPipelineByPropertyName)]
        [ValidateNotNullOrEmpty()]
        [string]
        $StreamName,

        [Parameter(ValueFromPipelineByPropertyName)]
        [string]
        $TwitterName,

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
        $UserId = (Get-TwitchUser -StreamName $StreamName -TwitchApp $TwitchApp).id
        $CallBackUri = if ($TwitterName) {
            "{0}/{1}/{2}" -f $WebHookBaseUri, $StreamName, $TwitterName
        } else {
            "{0}/{1}" -f $WebHookBaseUri, $StreamName
        }
        $Body = @{
            "hub.callback" = $CallBackUri
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
            $Parts = $_.callback -split '/'
            switch ($Parts.Count) {
                6 {
                    $StreamName = $Parts[-1]
                    $TwitterName = $null
                }
                7 {
                    $StreamName = $Parts[-2]
                    $TwitterName = $Parts[-1]
                }
                Default {}
            }
            $_ | Add-Member -MemberType NoteProperty -Name StreamName -Value $StreamName
            $_ | Add-Member -MemberType NoteProperty -Name TwitterName -Value $TwitterName
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

        [Parameter(ValueFromPipelineByPropertyName)]
        [string]
        $TwitterName,

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
        $CallBackUri = if ($TwitterName) {
            "{0}/{1}/{2}" -f $WebHookBaseUri, $StreamName, $TwitterName
        } else {
            "{0}/{1}" -f $WebHookBaseUri, $StreamName
        }
        $Body = @{
            "hub.callback" = $CallBackUri
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
$Subscriptions | Format-List StreamName, TwitterName, topic, callback
$Subscriptions[0] | Unregister-TwitchStreamWebhookSubscription -HubSecret $HubSecret
$Subscriptions | Unregister-TwitchStreamWebhookSubscription -HubSecret $HubSecret


$Results = @(
    [PSCustomObject]@{StreamName='markekraus'; TwitterName='markekraus'}
    [PSCustomObject]@{StreamName='corbob'; TwitterName='CoryKnox'}
    [PSCustomObject]@{StreamName='halbaradkenafin'; TwitterName='halbaradkenafin'}
    [PSCustomObject]@{StreamName='MrThomasRayner'; TwitterName='MrThomasRayner'}
    [PSCustomObject]@{StreamName='steviecoaster'; TwitterName='steviecoaster'}
    [PSCustomObject]@{StreamName='PowerShellTeam'; TwitterName='PowerShell_Team'}
    [PSCustomObject]@{StreamName='tylerleonhardt'; TwitterName='TylerLeonhardt'}
    [PSCustomObject]@{StreamName='potatoqualitee'; TwitterName='cl'}
    [PSCustomObject]@{StreamName='kevinmarquette'; TwitterName='kevinmarquette'}
    [PSCustomObject]@{StreamName='PowerShellDoug'; TwitterName='dfinke'}
    [PSCustomObject]@{StreamName='glennsarti'; TwitterName='GlennSarti'}
    [PSCustomObject]@{StreamName='veronicageek'; TwitterName='veronicageek'}
) | Register-TwitchStreamWebhookSubscription -HubSecret $HubSecret -HubLeaseSeconds 864000 -TwitchApp $TwitchApp

$FuncCode = Read-Host -AsSecureString
$FuncCode = ([pscredential]::new('foo', $HubSecret)).GetNetworkCredential().Password

function Invoke-TwitchSubscriptionRegistration {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string]
        $Path,

        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string]
        $FunctionSecret,

        [Parameter(DontShow)]
        [string]
        $FunctionUri = 'https://twitchstreamnotifications.azurewebsites.net/api/TwitchSubscriptionRegistration'
        )

    end {
        try {
            $Body = Get-Content -Raw -Path $Path -ErrorAction stop
        } catch {
            $PSCmdlet.ThrowTerminatingError($_)
        }
        $Params = @{
            Headers = @{
                'x-functions-key' = $FunctionSecret
            }
            Uri = $FunctionUri
            Method = 'POST'
            Body = $Body
            ContentType = 'application/json'
        }
        Invoke-RestMethod @Params
    }
}

$Result = Invoke-TwitchSubscriptionRegistration -Path 'config/Subscriptions.json' -FunctionSecret $FuncCode
'-----------------'
'Added:'
$Result.AddSubscriptions
' '
'Removed:'
$Result.RemoveSubscriptions
' '
'Requested:'
$Result.RequestSubscriptions
' '
'Current:'
$Result.CurrentSubscriptions.subscription

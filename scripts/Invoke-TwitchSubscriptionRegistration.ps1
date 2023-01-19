function Register-TwitchSubscription {
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
        $FunctionUri = 'https://twitchstreamnotifications2.azurewebsites.net/api/RegisterSubscription'
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
function Clear-TwitchSubscription {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string]
        $FunctionSecret,

        [Parameter(DontShow)]
        [string]
        $FunctionUri = 'https://twitchstreamnotifications2.azurewebsites.net/api/ClearSubscriptions'
    )

    end {
        $Params = @{
            Headers = @{
                'x-functions-key' = $FunctionSecret
            }
            Uri = $FunctionUri
            Method = 'POST'
            ContentType = 'application/json'
        }
        Invoke-RestMethod @Params
    }
}
function Get-TwitchSubscription {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory)]
        [ValidateNotNullOrEmpty()]
        [string]
        $FunctionSecret,

        [Parameter(DontShow)]
        [string]
        $FunctionUri = 'https://twitchstreamnotifications2.azurewebsites.net/api/GetSubscriptions'
    )

    end {
        $Params = @{
            Headers = @{
                'x-functions-key' = $FunctionSecret
            }
            Uri = $FunctionUri
            Method = 'GET'
            ContentType = 'application/json'
        }
        Invoke-RestMethod @Params
    }
}
Clear-TwitchSubscription -FunctionSecret $env:FunctionCode

do {
    Start-Sleep -Seconds 10
    $Subscriptions = Get-TwitchSubscription -FunctionSecret $env:FunctionCode
    $Subscriptions.data.condition.count
} while ($Subscriptions.data.condition.count -gt 0)

$invokeTwitchSubscriptionRegistrationSplat = @{
    Path = 'config/Subscriptions.json'
    FunctionSecret = $env:FunctionCode
}
Register-TwitchSubscription @invokeTwitchSubscriptionRegistrationSplat

# ' '
# ' '
# ' '
# ' '
# ' '
# '-----------------'
# 'Requested Subscriptions:'
# $Result.RequestSubscriptions | Format-Table -AutoSize twitchname, twittername, discordname
# ' '
# '-----------------'
# 'Current Subscriptions:'
# $Result.CurrentSubscriptions.subscription | Format-Table -AutoSize twitchname, twittername, discordname
# ' '
# '-----------------'
# 'Queued for Subscribe'
# $Result.AddSubscriptions | Format-Table -AutoSize twitchname, twittername, discordname
# ' '
# '-----------------'
# 'Queued for Unsubscribe'
# $Result.RemoveSubscriptions | Format-Table -AutoSize twitchname, twittername, discordname
# ' '
# ' '
# '-----------------'
# 'Queued for Renewal'
# $Result.RenewSubscriptions | Format-Table -AutoSize twitchname, twittername, discordname
# ' '
# ' '

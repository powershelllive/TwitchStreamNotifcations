[CmdletBinding()]
param (
    [Parameter()]
    [string]
    $AzureOutput = $env:AzureOutput,

    [Parameter()]
    $ConfigFile = (Resolve-Path "storage.json").Path
)
end {
    $AzureOutput

    'Processing AzureOutput'
    $AzOutput = $AzureOutput | ConvertFrom-Json -ErrorAction 'Stop'
    $StorageAccount = $AzOutput.StorageAccount.value
    $ResourceGroup = $AzOutput.ResourceGroup.value

    'Processing {0}' -f $ConfigFile
    $Config = Get-Content -raw -Path $ConfigFile | ConvertFrom-Json -ErrorAction Stop
    $Account = Get-AzureRmStorageAccount -Name $StorageAccount -ResourceGroupName $ResourceGroup
    $Context = $Account.Context

    foreach($queue in $Config.queues){
        try {
            'Adding queue {0}' -f $queue
            New-AzureStorageQueue -Name $queue -Context $Context -ErrorAction 'Stop'
            'Added queue {0}' -f $queue
        } catch [Microsoft.WindowsAzure.Commands.Storage.Common.ResourceAlreadyExistException] {
            'Queue {0} already exists.' -f $queue
        } catch {
            $PSCmdlet.ThrowTerminatingError($_)
        }
    }

    foreach($table in $Config.tables){
        try {
            'Adding table {0}' -f $table
            New-AzureStorageTable -Name $table -Context $Context -ErrorAction 'Stop' 
            'Added table {0}' -f $table
        } catch [Microsoft.WindowsAzure.Commands.Storage.Common.ResourceAlreadyExistException] {
            'Table {0} already exists.' -f $table
        } catch {
            $PSCmdlet.ThrowTerminatingError($_)
        }
    }

    foreach ($container in $Config.containers) {
        try {
            'Adding container {0}' -f $container
            New-AzureStorageContainer -Context $Context -Name $container -ErrorAction Stop
            'Added container {0}' -f $container
        } catch [Microsoft.WindowsAzure.Commands.Storage.Common.ResourceAlreadyExistException] {
           'Container {0} already exists' -f $container
        } catch {
            $PSCmdlet.ThrowTerminatingError($_)
        }
    }

    $QueueList = Get-AzureStorageQueue -Context $Context | ForEach-Object -MemberName Name
    $RemoveQueues = [System.Linq.Enumerable]::Except([string[]]$QueueList, [string[]]$Config.queues)
    foreach($queue in $RemoveQueues) {
        try {
            'Removing queue {0}' -f $queue
            Remove-AzureStorageQueue -Name $queue -Context $Context -Force -ErrorAction 'stop'
            'Removed queue {0}' -f $queue
        } catch [Microsoft.WindowsAzure.Commands.Storage.Common.ResourceNotFoundException] {
            'Queue {0} already removed.' -f $queue
        } catch {
            $PSCmdlet.ThrowTerminatingError($_)
        }
    }

    $TableList = Get-AzureStorageTable -Context $Context | ForEach-Object -MemberName Name
    $RemoveTables = [System.Linq.Enumerable]::Except([string[]]$TableList, [string[]]$Config.tables)
    foreach($table in $RemoveTables) {
        try {
            'Removing table {0}' -f $table
            Remove-AzureStorageTable -Name $table -Context $Context -Force -ErrorAction 'stop'
            'Removed table {0}' -f $table
        } catch [Microsoft.WindowsAzure.Commands.Storage.Common.ResourceNotFoundException] {
            'Table {0} already removed.' -f $table
        } catch {
            $PSCmdlet.ThrowTerminatingError($_)
        }
    }

    $ContainerList = Get-AzureRmStorageContainer -ResourceGroupName $ResourceGroup -StorageAccountName $StorageAccount | ForEach-Object -MemberName Name
    $RemoveContainers = [System.Linq.Enumerable]::Except([string[]]$ContainerList, [string[]]$Config.containers)
    foreach($container in $RemoveContainers) {
        try {
            'Removing container {0}' -f $container
            Remove-AzureStorageContainer -Context $Context -Name $container -Force -ErrorAction 'stop'
            'Removed container {0}' -f $container
        } catch [Microsoft.WindowsAzure.Commands.Storage.Common.ResourceNotFoundException] {
            'container {0} already removed.' -f $container
        } catch {
            $PSCmdlet.ThrowTerminatingError($_)
        }
    }
}

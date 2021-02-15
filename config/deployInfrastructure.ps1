[cmdletbinding()]
param(
    $RGName = 'TwitchStreamNotify',
    $ParametersPath = (Resolve-Path "main.parameters.json").Path,
    $StorageConfigPath = (Resolve-Path "storage.json").Path
)

$ArmParameters = Get-Content -Path $ParametersPath | ConvertFrom-Json
$Location = $ArmParameters.parameters.location.value

$null = az group create --location $Location --name $RGName

$DeploymentResult = az deployment group create -g $RGName -f .\main.json -p $ParametersPath --only-show-errors | ConvertFrom-Json
if($DeploymentResult.properties.provisioningState -ne 'Succeeded') {
    if($DeploymentResult.properties.error) {
        $Message = $DeploymentResult.properties.error
    }
    else {
        $Message = 'Failed to deploy'
    }
    throw $Message
}
$StorageAccount = $DeploymentResult.properties.outputs.storageAccountName.value

$Config = Get-Content -Raw -Path $storageConfigPath | ConvertFrom-Json -ErrorAction Stop
foreach($queue in $Config.queues){
    'Adding queue {0}' -f $queue
    $created = az storage queue create --account-name $StorageAccount --name $queue -o tsv --only-show-errors
    if($created -eq 'True') {
        'Added queue {0}' -f $queue
    }
    elseif ($created -eq 'False') {
        'Queue {0} already exists.' -f $queue
    }
    else {
        $PSCmdlet.ThrowTerminatingError($created)
    }
}

foreach($table in $Config.tables){
    'Adding table {0}' -f $table
    $created = az storage table create --account-name $StorageAccount --name $table -o tsv --only-show-errors
    if($created -eq 'True') {
        'Added table {0}' -f $table
    }
    elseif ($created -eq 'False') {
        'Table {0} already exists.' -f $table
    }
    else {
        $PSCmdlet.ThrowTerminatingError($created)
    }
}

foreach ($container in $Config.containers) {
    'Adding container {0}' -f $container
    $created = az storage container create  --account-name $StorageAccount --name $container -o tsv --only-show-errors
    if($created -eq 'True') {
        'Added container {0}' -f $container
    }
    elseif ($created -eq 'False') {
        'Container {0} already exists' -f $container
    }
    else {
        $PSCmdlet.ThrowTerminatingError($created)
    }
}

$QueueList = az storage queue list --account-name $StorageAccount --query '[].name' -o tsv --only-show-errors
$RemoveQueues = [System.Linq.Enumerable]::Except([string[]]$QueueList, [string[]]$Config.queues)
foreach($queue in $RemoveQueues) {
    'Removing queue {0}' -f $queue
    $removed = az storage queue delete --account-name $StorageAccount --name $queue -o tsv --only-show-errors
    if($removed -eq 'True') {
        'Removed queue {0}' -f $queue
    }
    elseif ($removed -eq 'False') {
        'Queue {0} already removed.' -f $queue
    }
    else {
        $PSCmdlet.ThrowTerminatingError($removed)
    }
}

$TableList = az storage table list --account-name $StorageAccount --query '[].name' -o tsv --only-show-errors
$RemoveTables = [System.Linq.Enumerable]::Except([string[]]$TableList, [string[]]$Config.tables)
foreach($table in $RemoveTables) {
    'Removing table {0}' -f $table
    $removed = az storage table delete --account-name $StorageAccount --name $table -o tsv --only-show-errors
    if($removed -eq 'True') {
        'Removed table {0}' -f $table
    }
    elseif ($removed -eq 'False') {
        'Table {0} already removed.' -f $table
    }
    else {
        $PSCmdlet.ThrowTerminatingError($removed)
    }
}

$ContainerList = az storage container list --account-name $StorageAccount --query '[].name' -o tsv --only-show-errors
$RemoveContainers = [System.Linq.Enumerable]::Except([string[]]$ContainerList, [string[]]$Config.containers)
foreach($container in $RemoveContainers) {
    'Removing container {0}' -f $container
    $removed = az storage container delete --account-name $StorageAccount --name $container -o tsv --only-show-errors
    if($removed -eq 'True') {
        'Removed container {0}' -f $container
    }
    elseif ($removed -eq 'False') {
        'Container {0} already removed.' -f $container
    }
    else {
        $PSCmdlet.ThrowTerminatingError($removed)
    }
}


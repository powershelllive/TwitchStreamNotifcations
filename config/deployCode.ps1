param(
    [Parameter(Mandatory)]
    $SystemName
)

Push-Location '..\src'
func azure functionapp publish $SystemName
Pop-Location
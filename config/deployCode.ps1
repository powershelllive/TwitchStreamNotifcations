$Parameters = Get-Content '.\main.parameters.json' | ConvertFrom-Json
$SystemName = $Parameters.parameters.systemName.value
Push-Location '..\src'
func azure functionapp publish $SystemName
Pop-Location
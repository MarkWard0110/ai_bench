param(
    [int]$count = 100,
    [string]$label = "default",
    [string]$optionsCfg = "config\\default-options.json"
)

for ($i = 1; $i -le $count; $i++) {
    Write-Host "----------------------------------------------------------------------------"
    Write-Host "Run $i of $count"
    & ".\AgentBenchmark.exe" --label $label --optionsCfg $optionsCfg
}
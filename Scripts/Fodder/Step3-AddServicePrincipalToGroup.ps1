﻿$adSecurityGroupName = "Power BI Apps"
$adSecurityGroup = Get-AzureADGroup -Filter "DisplayName eq '$adSecurityGroupName'"

$adApplicationName = "App-only Demo App"
$servicePrincipal = Get-AzureADServicePrincipal -Filter "DisplayName eq '$adApplicationName'"

Add-AzureADGroupMember -ObjectId $($adSecurityGroup.ObjectId) -RefObjectId $($servicePrincipal.ObjectId)

Write-Host 
Write-Host "Group members for" $adSecurityGroup.DisplayName

Get-AzureADGroupMember -ObjectId $($adSecurityGroup.ObjectId) | Format-Table ObjectType, ObjectId, DisplayName
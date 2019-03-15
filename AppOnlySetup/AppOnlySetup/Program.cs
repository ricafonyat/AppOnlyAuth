﻿using System;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.PowerBI.Api.V2;
using Microsoft.PowerBI.Api.V2.Models;
using Microsoft.Rest;

class Program {

  private static string tenantId = "[YOUR_TENANT_ID_HERE]";
  private static string applicationId = "[YOUR_APP_ID_HERE]";
  private static string applicationPassword = "[YOUR_APP_SECRET_HERE]";
  private static string servicePrincipalId = "[YOUR_SERVICE_PRINCIPAL_ID_HERE]";

  private static string aadAuthorizationRoot = "https://login.microsoftonline.com/";
  private static string aadAuthorizationTenantEndpoint = aadAuthorizationRoot + tenantId + "/";

  private static string resourceUriPowerBi = "https://analysis.windows.net/powerbi/api";
  private static string urlPowerBiRestApiRoot = "https://api.powerbi.com/";

  static void Main() {
    CreateV2AppWorkspace("My First App Workspace");
  }

  // perform work using app-only identity
  private static string GetAppOnlyAccessToken() {

    var authenticationContext = new AuthenticationContext(aadAuthorizationTenantEndpoint);

    var clientCredential = new ClientCredential(applicationId, applicationPassword);

    AuthenticationResult userAuthnResult =
      authenticationContext.AcquireTokenAsync(resourceUriPowerBi, clientCredential).Result;

    return userAuthnResult.AccessToken;
  }

  static PowerBIClient GetPowerBiClientAsApp() {
    var tokenCredentials = new TokenCredentials(GetAppOnlyAccessToken(), "Bearer");
    return new PowerBIClient(new Uri(urlPowerBiRestApiRoot), tokenCredentials);
  }

  static void CreateV2AppWorkspace(string Name) {

    string restUrlWorkspace = "https://api.powerbi.com/v1.0/myorg/groups?workspaceV2=True";

    // serialize C# object into JSON
    string requestBody = @"{ ""name"": """ + Name + @"""}";

    // add JSON to HttpContent object and configure content type
    HttpContent postRequestBody = new StringContent(requestBody);
    postRequestBody.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/json");

    // prepare PATCH request
    var method = new HttpMethod("POST");
    var request = new HttpRequestMessage(method, restUrlWorkspace);
    request.Content = postRequestBody;

    HttpClient client = new HttpClient();
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.DefaultRequestHeaders.Add("Authorization", "Bearer " + GetAppOnlyAccessToken());

    // send PATCH request to Power BI service 
    var result = client.SendAsync(request).Result;
  }
 
  // perform work using Master User Account identity
  private static string GetMasterUserAccessToken() {

    string appId = "APP_ID_FOR_MASTER_USER";

    AuthenticationContext authenticationContext = new AuthenticationContext(aadAuthorizationRoot + "common");

    UserPasswordCredential creds = new UserPasswordCredential("MASTER_USER_ACCOUNT", "PASSWORD");

    AuthenticationResult userAuthnResult =
      authenticationContext.AcquireTokenAsync(resourceUriPowerBi, appId, creds).Result;

    return userAuthnResult.AccessToken;
  }

  static PowerBIClient GetPowerBiClientAsMasterUser() {
    var tokenCredentials = new TokenCredentials(GetMasterUserAccessToken(), "Bearer");
    return new PowerBIClient(new Uri(urlPowerBiRestApiRoot), tokenCredentials);
  }

  static void SetAppWorkspaceMembership() {

    // add name of target workspace
    string targetAppWorkspaceName = "";

    PowerBIClient pbiClient = GetPowerBiClientAsMasterUser();
    var workspaces = pbiClient.Groups.GetGroupsAsAdminAsync(top: 100).Result.Value;

    foreach (var workspace in workspaces) {

      // find the target app workspace and add service principal as admin
      if (workspace.Name.Contains(targetAppWorkspaceName)) {

        Console.WriteLine("Adding service principal as admin to " + workspace.Name);
        var perms = new GroupUserAccessRight("Admin",
                                              identifier: servicePrincipalId,
                                              principalType: "App");

        pbiClient.Groups.AddGroupUserAsync(workspace.Id, perms);

      }
    }
  }
  
}


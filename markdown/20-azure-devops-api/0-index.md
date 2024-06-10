---
theme: Azure DevOps
title: Forcing support for unsupported platforms
visible: true
---

I wanted to use the Azure DevOps .NET client libraries to build something simple that talks to the Azure DevOps api. While the api of Azure DevOps is pretty decent; you can even discover how it works by clicking around in the UI while observing what calls are being made, [their client library situation](https://learn.microsoft.com/en-us/azure/devops/integrate/concepts/dotnet-client-libraries?view=azure-devops&viewFallbackFrom=vsts) is quite something else. The namespaces are all over te place, and documentation on how to do anything is mostly missing. And after I've got everything working, I found out that running those client libraries in web assembly is not supported. But I got it working anyway.

:::aside
To get to the data I wanted to get, I needed to use the `Microsoft.TeamFoundation.Core.WebApi.ProjectHttpClient` client to fetch the projects. That makes sense, but to get the pipelines of those projects I needed to use `Microsoft.Azure.Pipelines.WebApi.PipelinesHttpClient`. To get the runs of those pipelines, I needed to feed the `pipelineId` into `Microsoft.TeamFoundation.Build.WebApi.BuildHttpClient`, which wasn't obvious. There is no clear relation between the REST api and Http Clients, so you just have to look around in all the namespaces. That those namespaces are inside 15 packages, makes it a bit hard to get stared.
:::

I didn't want to reimplement the entire web api all over again, or copy over all the stuff I needed and cobble something together. But, since the client libraries assume they can use the `Credential` feature of `HttpClient`, they are not compatible with the web assembly runtime:

```
crit: Microsoft.AspNetCore.Components.WebAssembly.Rendering.WebAssemblyRenderer[100]
      Unhandled exception rendering component: Operation is not supported on this platform.
System.PlatformNotSupportedException: Operation is not supported on this platform.
   at System.Net.Http.BrowserHttpHandler.get_Credentials()
   at System.Net.Http.HttpClientHandler.set_UseDefaultCredentials(Boolean value)
   at Microsoft.VisualStudio.Services.Common.VssHttpMessageHandler.ApplySettings(HttpMessageHandler handler, ICredentials defaultCredentials, VssHttpRequestSettings settings)
   at Microsoft.VisualStudio.Services.Common.VssHttpMessageHandler..ctor(VssCredentials credentials, VssHttpRequestSettings settings, HttpMessageHandler innerHandler)
   at Microsoft.VisualStudio.Services.Common.VssHttpMessageHandler..ctor(VssCredentials credentials, VssHttpRequestSettings settings)
   at Microsoft.VisualStudio.Services.WebApi.VssConnection..ctor(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings)
   at Microsoft.VisualStudio.Services.WebApi.VssConnection..ctor(Uri baseUrl, VssCredentials credentials)
   at Overwatch.PipelineStatusService.GetPipelineStatusAsync(String pat, String projectName)+MoveNext() in C:\Program.cs:line 29
   at Overwatch.PipelineStatusService.GetPipelineStatusAsync(String pat, String projectName)+System.Threading.Tasks.Sources.IValueTaskSource<System.Boolean>.GetResult()
   at Overwatch.App.Pages.Home.OnInitializedAsync() in C:\Pages\Home.razor:line 37
   at Overwatch.App.Pages.Home.OnInitializedAsync() in C:\Pages\Home.razor:line 37
   at Microsoft.AspNetCore.Components.ComponentBase.RunInitAndSetParametersAsync() blazor.webassembly.js:1:46958
```

This exception is thrown when you run the [example code](https://learn.microsoft.com/en-us/azure/devops/integrate/concepts/dotnet-client-libraries?view=azure-devops#connect):

```csharp
var connection = new VssConnection(orgUrl, new VssBasicCredential(string.Empty, personalAccessToken));

// get a GitHttpClient to talk to the Git endpoints
var gitClient = connection.GetClient<GitHttpClient>();

// This will throw
var repo = await gitClient.GetRepositoryAsync(c_projectName, c_repoName);
```

That BrowserHttpHandler in the stack trace [is a prettey nerfed handler](https://github.com/dotnet/runtime/blob/main/src/libraries/System.Net.Http/src/System/Net/Http/BrowserHttpHandler/BrowserHttpHandler.cs) that has a fair amount of properties that just blow up. So, to get this working I need to get bypass the `Credential` part of the HttpClient setup. Too bad none of the constructors of any of the HttpClients really allow you to do that:

```csharp
public ProjectHttpClient(Uri baseUrl, VssCredentials credentials)
    : base(baseUrl, credentials)
{
}

public ProjectHttpClient(Uri baseUrl, VssCredentials credentials, params DelegatingHandler[] handlers)
    : base(baseUrl, credentials, handlers)
{
}

public ProjectHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings)
    : base(baseUrl, credentials, settings)
{
}

public ProjectHttpClient(Uri baseUrl, VssCredentials credentials, VssHttpRequestSettings settings, params DelegatingHandler[] handlers)
    : base(baseUrl, credentials, settings, handlers)
{
}

public ProjectHttpClient(Uri baseUrl, HttpMessageHandler pipeline, bool disposeHandler)
    : base(baseUrl, pipeline, disposeHandler)
{
}
```

It would have been great if they add another constructor that just allows you to pass in a custom `HttpClient`. They don't, so we need to do two things. First is to construct such a client with a specific `HttpMessageHandler`, this bypasses some setup and prevents the client library from touching any of the `Credential` stuff. I have simply new-ed up a `HttpClientHandler` with no configuration. Although this works, the handler has no credentials configured causing all api calls to result in 401s (or 304s as you're redirected to the login page). The second step is to gently convince the client library to use a specific, correctly configured `HttpClient`. To do that, we have to resort to a little reflection:

```csharp
var orgUrl = new Uri("https://dev.azure.com/{org}");

// plain handler used as a decoy
var handler = new HttpClientHandler();

// client with correct credentials without using Credential
var client = new HttpClient { BaseAddress = orgUrl };
client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($":{pat}")));

// the deepest base of client library has a field that stores the HttpClient that used to make HTTP calls
var clientField = typeof(VssHttpClientBase).GetField("m_client", BindingFlags.NonPublic | BindingFlags.Instance)!;

// create the client
var projectClient = new ProjectHttpClient(orgUrl, handler, false);

// override the HttpClient
clientField.SetValue(projectClient, client);

// works!
var projects = await projectClient.GetProjects(ProjectState.WellFormed);
```

So as long as that `m_client` field doesn't change, this will work. 

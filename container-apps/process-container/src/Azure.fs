module Azure

open System
open Azure.Identity
open Azure.Messaging.ServiceBus

let fqns = Environment.GetEnvironmentVariable("ServiceBusFqns")
let tenantId = Environment.GetEnvironmentVariable("TenantId")
let clientId = Environment.GetEnvironmentVariable("ClientId")

let options = 
    match clientId with
    | uidClientId when uidClientId <> null -> DefaultAzureCredentialOptions (TenantId = tenantId, ManagedIdentityClientId = uidClientId)
    | _ -> DefaultAzureCredentialOptions (TenantId = tenantId)

let credential = DefaultAzureCredential(options)

let client = new ServiceBusClient(fqns, credential)

let receiver = client.CreateReceiver("process")

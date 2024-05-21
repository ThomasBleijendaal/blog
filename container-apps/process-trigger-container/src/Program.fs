open Azure.Identity
open Azure.Messaging.ServiceBus
open System
open System.Threading.Tasks

let fqns = Environment.GetEnvironmentVariable("SB_FQNS")
let tenantId = Environment.GetEnvironmentVariable("TENANT_ID")
let clientId = Environment.GetEnvironmentVariable("CLIENT_ID")

type Message = { Now: DateTimeOffset }

let options = 
    match clientId with
    | uidClientId when uidClientId <> null -> DefaultAzureCredentialOptions (TenantId = tenantId, ManagedIdentityClientId = uidClientId)
    | _ -> DefaultAzureCredentialOptions (TenantId = tenantId)

let credential = DefaultAzureCredential(options)

let client = new ServiceBusClient(fqns, credential)
let sender = client.CreateSender("process")

let message = ServiceBusMessage(BinaryData.FromObjectAsJson({ Now = DateTimeOffset.UtcNow }))

let task = sender.SendMessageAsync(message)

Task.WaitAll task

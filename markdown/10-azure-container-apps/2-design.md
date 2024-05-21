---
theme: Azure Container Apps
title: Design
visible: True
---

There will be 4 containers deployed into the App Container environment.

```ascii
                                               ┌──────────────────────┐
                                    Gets file  │                      │
                                        ┌─────►│  Storage container   │
                                        │      │                      │
               ┌───────────────────┐    │      └──────────────────────┘
 Internet      │                   ├────┘              ▲               
──────────────►│  HTTP Container   │                   │               
               │                   │                   │               
               └─┬─────────────────┘                   │               
                 │                                     │               
                 │                                     │               
                 │         ┌───────────────────┐       │               
                 │Queries  │                   │       │               
                 └────────►│ Database container│       │               
                           │                   │       │               
                           └───────────────────┘       │               
                                       ▲               │ Updates files 
                                       │ Updates data  │               
                                       │               │               
                           ┌───────────┴───────┐       │               
 GitHub action             │                   │       │               
──────────────────────────►│ Process container ├───────┘               
                           │                   │                       
                           └───────────────────┘                       
```

Http Container
:   Azure Function in a container serving HTTP request. Will set the correct HTTP headers and other functionality. I want to use an function app here so I can test how good it works.

Storage Container
:   Some ASP.NET application only connected to internal network of the Container Apps Environment, implementing a simple file API using gRPC. This gives me a change to mess around with gRPC and persistent storage in containers. This might be written in F#.

Database Container
:   Some open-source database that can be used to search on index blog data. I'll have the investigate what fancy database I can use for this and if I'm actually going to use this.

Process Container
:   This container will run some Console or Azure Function-based code that will convert the markdown from the GitHub repo, builds all the pages, beautifies the HTML as I want readable HTML source, and stores it in the Storage Container. This will be written in F#. The trigger of this container will be a service bus queue that receives messages from a trigger in the GitHub repo.

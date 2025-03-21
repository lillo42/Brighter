﻿using System;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;

namespace Paramore.Brighter.MessagingGateway.AzureServiceBus.ClientProvider;

public class ServiceBusAzureCliCredentialClientProvider : ServiceBusClientProvider
{
    /// <summary>
    /// Initializes an implementation is <see cref="IServiceBusClientProvider"/> using Azure Cli Credentials for Authentication.
    /// </summary>
    /// <param name="fullyQualifiedNameSpace">The Fully Qualified Namespace i.e. my-servicebus.azureservicebus.net</param>
    /// <exception cref="ArgumentNullException">Throws is the namespace is null</exception>
    public ServiceBusAzureCliCredentialClientProvider(string fullyQualifiedNameSpace)
    {
        if (string.IsNullOrEmpty(fullyQualifiedNameSpace))
        {
            throw new ArgumentNullException(nameof(fullyQualifiedNameSpace),
                "Fully qualified Namespace is null or empty, ensure this is set in the constructor.");
        }

        var credential = new AzureCliCredential();
        Client = new ServiceBusClient(fullyQualifiedNameSpace, credential);
        AdminClient = new ServiceBusAdministrationClient(fullyQualifiedNameSpace, credential);
    }
}

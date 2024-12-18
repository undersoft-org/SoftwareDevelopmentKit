﻿using System.Diagnostics.CodeAnalysis;
using Undersoft.SDK.Service.Data.Object;

namespace Undersoft.SDK.Service.Data.Blob
{
    public class DefaultBlobProviderSelector : IBlobProviderSelector
    {
        protected IEnumerable<IBlobProvider> BlobProviders { get; }

        protected IBlobContainerConfigurationProvider ConfigurationProvider { get; }

        public DefaultBlobProviderSelector(
            IBlobContainerConfigurationProvider configurationProvider,
            IEnumerable<IBlobProvider> blobProviders)
        {
            ConfigurationProvider = configurationProvider;
            BlobProviders = blobProviders;
        }

        public virtual IBlobProvider Get([DisallowNull] string containerName)
        {

            var configuration = ConfigurationProvider.Get(containerName);

            if (!BlobProviders.Any())
            {
                throw new Exception("No BLOB Storage InnerProvider was registered! At least one InnerProvider must be registered to be able to use the BLOB Storing System.");
            }

            if (configuration.ProviderType == null)
            {
                throw new Exception("No BLOB Storage InnerProvider was used! At least one InnerProvider must be configured to be able to use the BLOB Storing System.");
            }

            foreach (var provider in BlobProviders)
            {
                if (provider.GetDataType().IsAssignableTo(configuration.ProviderType))
                {
                    return provider;
                }
            }

            throw new Exception(
                $"Could not find the BLOB Storage InnerProvider with the type ({configuration.ProviderType.AssemblyQualifiedName}) configured for the container {containerName} and no default InnerProvider was Set."
            );
        }
    }
}
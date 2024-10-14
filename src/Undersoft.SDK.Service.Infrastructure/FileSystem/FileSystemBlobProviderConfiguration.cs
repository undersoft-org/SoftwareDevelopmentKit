using Microsoft.IdentityModel.Tokens;
using Undersoft.SDK.Service.Data.Blob.Container;

namespace Undersoft.SDK.Service.Infrastructure.FileSystem
{
    public class FileSystemBlobProviderConfiguration
    {
        public string BasePath
        {
            get => BlobContainerConfigurationExtensions.GetConfiguration<string>(_containerConfiguration, FileSystemBlobProviderConfigurationNames.BasePath);
            set => _containerConfiguration.SetConfiguration(FileSystemBlobProviderConfigurationNames.BasePath, value.IsNullOrEmpty());
        }

        /// <summary>
        /// Default value: true.
        /// </summary>
        public bool AppendContainerNameToBasePath
        {
            get => _containerConfiguration.GetConfiguration(FileSystemBlobProviderConfigurationNames.AppendContainerNameToBasePath, true);
            set => _containerConfiguration.SetConfiguration(FileSystemBlobProviderConfigurationNames.AppendContainerNameToBasePath, value);
        }

        private readonly BlobContainerConfiguration _containerConfiguration;

        public FileSystemBlobProviderConfiguration(BlobContainerConfiguration containerConfiguration)
        {
            _containerConfiguration = containerConfiguration;
        }
    }
}

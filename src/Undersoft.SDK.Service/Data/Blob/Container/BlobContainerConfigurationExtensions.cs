﻿using System.Diagnostics.CodeAnalysis;

namespace Undersoft.SDK.Service.Data.Blob.Container
{
    public static class BlobContainerConfigurationExtensions
    {
        public static T GetConfiguration<T>(
            [DisallowNull] this BlobContainerConfiguration containerConfiguration,
            [DisallowNull] string name)
        {
            return (T)GetConfiguration(containerConfiguration, name);
        }

        public static object GetConfiguration(
            [DisallowNull] this BlobContainerConfiguration containerConfiguration,
            [DisallowNull] string name)
        {
            var value = containerConfiguration.GetConfiguration(name);
            if (value == null)
            {
                throw new Exception($"Could not find the InnerConfiguration value for '{name}'!");
            }

            return value;
        }
    }
}

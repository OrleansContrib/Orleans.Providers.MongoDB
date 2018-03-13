﻿using System;

namespace Orleans.Providers.MongoDB.Configuration
{
    public sealed class MongoDBGrainStorageOptionsValidator : IConfigurationValidator
    {
        private readonly MongoDBGrainStorageOptions options;
        private readonly string name;

        public MongoDBGrainStorageOptionsValidator(MongoDBGrainStorageOptions options, string name)
        {
            this.options = options;
            this.name = name;
        }

        public void ValidateConfiguration()
        {
            options.Validate(name);
        }
    }
}

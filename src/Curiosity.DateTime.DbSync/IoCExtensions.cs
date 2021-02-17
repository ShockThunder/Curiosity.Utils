using System;
using Curiosity.AppInitializer;
using Curiosity.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Curiosity.DateTime.DbSync
{
    /// <summary>
    /// Extensions methods for <see cref="IServiceCollection"/> for date time services.
    /// </summary>
    public static class DateTimeServiceServiceCollectionExtensions
    {
        /// <summary>
        /// Adds to IoC service for working with date and time via database.
        /// </summary>
        /// <param name="serviceCollection"></param>
        /// <param name="dbDateTimeOptions"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static IServiceCollection AddDbSyncDateTimeServices(
            this IServiceCollection serviceCollection,
            DbDateTimeOptions dbDateTimeOptions)
        {
            if (serviceCollection == null) throw new ArgumentNullException(nameof(serviceCollection));
            if (dbDateTimeOptions == null) throw new ArgumentNullException(nameof(dbDateTimeOptions));

            serviceCollection.TryAddSingleton(dbDateTimeOptions);
            serviceCollection.TryAddSingleton<DbSyncDateTimeService>();
            serviceCollection.TryAddSingleton<IDateTimeService>(s => s.GetRequiredService<DbSyncDateTimeService>());
            serviceCollection.AddAppInitializer<DateTimeServiceInitializer>();
            serviceCollection.AddSingleton<IHostedService, DbDateTimeSynchronizationWatchdog>();

            return serviceCollection;
        }
    }
}
using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Curiosity.AppInitializer
{
    /// <summary>
    /// Provides extension methods to register async initializers.
    /// </summary>
    public static class IoCExtensions
    {
        /// <summary>
        /// Registers necessary services for async initialization support.
        /// </summary>
        /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the service to.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IServiceCollection AddAppInitialization(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            services.TryAddTransient<AppInitializer>();
            return services;
        }

        /// <summary>
        /// Adds an async initializer of the specified type.
        /// </summary>
        /// <typeparam name="TInitializer">The type of the async initializer to add.</typeparam>
        /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the service to.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IServiceCollection AddAppInitializer<TInitializer>(this IServiceCollection services)
            where TInitializer : class, IAppInitializer
        {
            return services
                .AddAppInitialization()
                .AddTransient<IAppInitializer, TInitializer>();
        }

        /// <summary>
        /// Adds the specified async initializer instance.
        /// </summary>
        /// <typeparam name="TInitializer">The type of the async initializer to add.</typeparam>
        /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the service to.</param>
        /// <param name="initializer">The service initializer</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IServiceCollection AddAppInitializer<TInitializer>(this IServiceCollection services, TInitializer initializer)
            where TInitializer : class, IAppInitializer
        {
            if (initializer == null)
                throw new ArgumentNullException(nameof(initializer));

            return services
                .AddAppInitialization()
                .AddSingleton<IAppInitializer>(initializer);
        }

        /// <summary>
        /// Adds an async initializer with a factory specified in <paramref name="implementationFactory" />.
        /// </summary>
        /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the service to.</param>
        /// <param name="implementationFactory">The factory that creates the async initializer.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IServiceCollection AddAppInitializer(this IServiceCollection services, Func<IServiceProvider, IAppInitializer> implementationFactory)
        {
            if (implementationFactory == null)
                throw new ArgumentNullException(nameof(implementationFactory));

            return services
                .AddAppInitialization()
                .AddTransient(implementationFactory);
        }

        /// <summary>
        /// Adds an async initializer of the specified type
        /// </summary>
        /// <param name="services">The <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> to add the service to.</param>
        /// <param name="initializerType">The type of the async initializer to add.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static IServiceCollection AddAppInitializer(this IServiceCollection services, Type initializerType)
        {
            if (initializerType == null)
                throw new ArgumentNullException(nameof(initializerType));

            return services
                .AddAppInitialization()
                .AddTransient(typeof(IAppInitializer), initializerType);
        }
    }
}
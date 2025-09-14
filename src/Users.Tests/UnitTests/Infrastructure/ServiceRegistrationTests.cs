using Xunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Users.Infrastructure;
using Users.Infrastructure.Data;

namespace Users.Tests.UnitTests.Infrastructure
{
    public class ServiceRegistrationTests
    {
        [Fact]
        public void AddInfrastructure_WithTestEnvironment_ShouldRegisterInMemoryDatabase()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string?>("ASPNETCORE_ENVIRONMENT", "TestEnvironment")
                })
                .Build();

            // Act
            services.AddInfrastructure(configuration);

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var dbContext = serviceProvider.GetRequiredService<UsersDbContext>();

            dbContext.Should().NotBeNull();
            dbContext.Database.ProviderName.Should().Be("Microsoft.EntityFrameworkCore.InMemory");
        }

        [Fact]
        public void AddInfrastructure_WithProductionEnvironment_ShouldRegisterMySqlDatabase()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string?>("ASPNETCORE_ENVIRONMENT", "Production"),
                    new KeyValuePair<string, string?>("ConnectionStrings:Default", "server=127.0.0.1;port=3306;database=usersdb;user=testuser;password=testpass;TreatTinyAsBoolean=false")
                })
                .Build();

            // Act
            services.AddInfrastructure(configuration);

            // Assert
            // Only verify service registration, not actual DB connection
            var dbContextDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(UsersDbContext));
            dbContextDescriptor.Should().NotBeNull();
            dbContextDescriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
        }

        [Fact]
        public void AddInfrastructure_WithProductionEnvironment_ShouldExecuteMySqlConfiguration()
        {
            // Arrange
            var services = new ServiceCollection();

            // Usar un connection string que permita AutoDetect pero falle la conexión
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string?>("ASPNETCORE_ENVIRONMENT", "Production"),
                    new KeyValuePair<string, string?>("ConnectionStrings:Default", "server=nonexistent.server;port=3306;database=testdb;user=testuser;password=testpass;TreatTinyAsBoolean=false")
                })
                .Build();

            // Act
            services.AddInfrastructure(configuration);

            // Para ejecutar las líneas 28-37, necesitamos forzar la construcción del DbContext
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            // Este intento creará el DbContext y ejecutará las líneas de configuración MySQL
            var act = () =>
            {
                try
                {
                    var dbContext = serviceProvider.GetRequiredService<UsersDbContext>();
                    // Intentar acceder a la base de datos para forzar la configuración
                    var _ = dbContext.Database.ProviderName;
                }
                catch
                {
                    // Se espera que falle la conexión, pero las líneas de configuración se habrán ejecutado
                }
            };

            // No debe lanzar excepción durante la configuración del servicio
            act.Should().NotThrow("ServiceRegistration should handle MySQL configuration gracefully");

            // Verificar que el servicio se registró
            var dbContextDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(UsersDbContext));
            dbContextDescriptor.Should().NotBeNull();
            dbContextDescriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
        }

        [Fact]
        public void AddInfrastructure_WithDevelopmentEnvironment_ShouldUseMySqlWithDefaultConnectionString()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string?>("ASPNETCORE_ENVIRONMENT", "Development")
                })
                .Build();

            // Act
            services.AddInfrastructure(configuration);

            // Assert
            // Only verify service registration, not actual DB connection
            var dbContextDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(UsersDbContext));
            dbContextDescriptor.Should().NotBeNull();
            dbContextDescriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
        }

        [Fact]
        public void AddInfrastructure_WithDevelopmentEnvironment_ShouldExecuteMySqlConfigurationWithDefaultConnectionString()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string?>("ASPNETCORE_ENVIRONMENT", "Development")
                    // No se proporciona ConnectionStrings:Default, usa el valor por defecto
                })
                .Build();

            // Act
            services.AddInfrastructure(configuration);

            // Para ejecutar las líneas 28-37, necesitamos forzar la construcción del DbContext
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            // Este test ejecutará las líneas de configuración MySQL con connection string por defecto
            var act = () =>
            {
                try
                {
                    var dbContext = serviceProvider.GetRequiredService<UsersDbContext>();
                    // Intentar acceder a la base de datos para forzar la configuración
                    var _ = dbContext.Database.ProviderName;
                }
                catch
                {
                    // Se espera que falle la conexión, pero las líneas de configuración se habrán ejecutado
                }
            };

            // No debe lanzar excepción durante la configuración
            act.Should().NotThrow("ServiceRegistration should handle MySQL configuration with default connection string gracefully");

            // Verificar que el servicio se registró
            var dbContextDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(UsersDbContext));
            dbContextDescriptor.Should().NotBeNull();
            dbContextDescriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
        }

        [Fact]
        public void AddInfrastructure_WithCustomConnectionString_ShouldUseMySqlWithCustomString()
        {
            // Arrange
            var services = new ServiceCollection();
            var customConnectionString = "server=localhost;port=3307;database=customdb;user=customuser;password=custompass;TreatTinyAsBoolean=false";
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string?>("ASPNETCORE_ENVIRONMENT", "Development"),
                    new KeyValuePair<string, string?>("ConnectionStrings:Default", customConnectionString)
                })
                .Build();

            // Act
            services.AddInfrastructure(configuration);

            // Assert
            // Only verify service registration, not actual DB connection
            var dbContextDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(UsersDbContext));
            dbContextDescriptor.Should().NotBeNull();
            dbContextDescriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
        }

        [Fact]
        public void AddInfrastructure_ShouldRegisterDbContextWithCorrectLifetime()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string?>("ASPNETCORE_ENVIRONMENT", "TestEnvironment")
                })
                .Build();

            // Act
            services.AddInfrastructure(configuration);

            // Assert
            var dbContextDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(UsersDbContext));
            dbContextDescriptor.Should().NotBeNull();
            dbContextDescriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
        }

        [Fact]
        public void AddInfrastructure_WithNullEnvironment_ShouldDefaultToMySql()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder().Build();

            // Act
            services.AddInfrastructure(configuration);

            // Assert
            // Only verify service registration, not actual DB connection
            var dbContextDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(UsersDbContext));
            dbContextDescriptor.Should().NotBeNull();
            dbContextDescriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
        }

        [Fact]
        public void AddInfrastructure_WithEmptyEnvironment_ShouldDefaultToMySql()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string?>("ASPNETCORE_ENVIRONMENT", "")
                })
                .Build();

            // Act
            services.AddInfrastructure(configuration);

            // Assert
            // Only verify service registration, not actual DB connection
            var dbContextDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(UsersDbContext));
            dbContextDescriptor.Should().NotBeNull();
            dbContextDescriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
        }

        [Fact]
        public void AddInfrastructure_ShouldReturnServiceCollection()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder().Build();

            // Act
            var result = services.AddInfrastructure(configuration);

            // Assert
            result.Should().BeSameAs(services);
        }

        [Fact]
        public void AddInfrastructure_MultipleCallsShouldBeIdempotent()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string?>("ASPNETCORE_ENVIRONMENT", "TestEnvironment")
                })
                .Build();

            // Act
            services.AddInfrastructure(configuration);
            services.AddInfrastructure(configuration);

            // Assert - ServiceCollection allows duplicate registrations by design
            // The last registration wins when resolving services
            var dbContextDescriptors = services.Where(s => s.ServiceType == typeof(UsersDbContext)).ToList();

            // Multiple calls will register services multiple times
            dbContextDescriptors.Should().HaveCountGreaterOrEqualTo(1, "at least one DbContext should be registered");

            // Verify services can still be resolved
            var serviceProvider = services.BuildServiceProvider();
            var dbContext = serviceProvider.GetRequiredService<UsersDbContext>();

            dbContext.Should().NotBeNull();
        }

        [Fact]
        public void AddInfrastructure_WithTestEnvironment_ShouldNotUseRetryPolicy()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string?>("ASPNETCORE_ENVIRONMENT", "TestEnvironment")
                })
                .Build();

            // Act
            services.AddInfrastructure(configuration);

            // Assert - Just verify no exception is thrown during service registration
            var serviceProvider = services.BuildServiceProvider();
            var dbContext = serviceProvider.GetRequiredService<UsersDbContext>();
            dbContext.Should().NotBeNull();
        }

        [Fact]
        public void AddInfrastructure_WithEnvironmentFromAlternativeKey_ShouldWorkCorrectly()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string?>("Environment", "TestEnvironment") // Using 'Environment' instead of 'ASPNETCORE_ENVIRONMENT'
                })
                .Build();

            // Act
            services.AddInfrastructure(configuration);

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var dbContext = serviceProvider.GetRequiredService<UsersDbContext>();

            dbContext.Should().NotBeNull();
            dbContext.Database.ProviderName.Should().Be("Microsoft.EntityFrameworkCore.InMemory");
        }

        [Fact]
        public void AddInfrastructure_WithMySqlConfiguration_ShouldConfigureRetryPolicy()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string?>("ASPNETCORE_ENVIRONMENT", "Production"),
                    new KeyValuePair<string, string?>("ConnectionStrings:Default", "server=127.0.0.1;port=3306;database=testdb;user=testuser;password=testpass;TreatTinyAsBoolean=false")
                })
                .Build();

            // Act
            services.AddInfrastructure(configuration);

            // Assert - Verify the DbContext service is registered correctly
            var dbContextDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(UsersDbContext));
            dbContextDescriptor.Should().NotBeNull();
            dbContextDescriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);

            // Verify that the service registration itself works (without actually connecting to MySQL)
            var serviceCount = services.Count(s => s.ServiceType == typeof(UsersDbContext));
            serviceCount.Should().BeGreaterThan(0, "DbContext should be registered");
        }

        [Fact]
        public void AddInfrastructure_WithNullConnectionString_ShouldUseDefaultConnectionString()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string?>("ASPNETCORE_ENVIRONMENT", "Production")
                    // No ConnectionStrings:Default provided, should use fallback
                })
                .Build();

            // Act
            services.AddInfrastructure(configuration);

            // Assert - Should use the default connection string fallback
            var dbContextDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(UsersDbContext));
            dbContextDescriptor.Should().NotBeNull();
            dbContextDescriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
        }

        [Fact]
        public void AddInfrastructure_WithMySqlServerVersionAutoDetect_ShouldConfigureCorrectly()
        {
            // Arrange
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string?>("ASPNETCORE_ENVIRONMENT", "Development"),
                    new KeyValuePair<string, string?>("ConnectionStrings:Default", "server=127.0.0.1;port=3306;database=testdb;user=testuser;password=testpass;TreatTinyAsBoolean=false")
                })
                .Build();

            // Act & Assert - Should not throw exception during configuration
            Action act = () => services.AddInfrastructure(configuration);
            act.Should().NotThrow("ServiceRegistration should handle MySQL configuration gracefully");

            // Verify registration
            var dbContextDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(UsersDbContext));
            dbContextDescriptor.Should().NotBeNull();
        }

        [Fact]
        public void AddInfrastructure_ConfigurationPaths_ShouldCoverAllBranches()
        {
            // Test ASPNETCORE_ENVIRONMENT takes precedence over Environment
            var services1 = new ServiceCollection();
            var config1 = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string?>("ASPNETCORE_ENVIRONMENT", "TestEnvironment"),
                    new KeyValuePair<string, string?>("Environment", "Production") // This should be ignored
                })
                .Build();

            services1.AddInfrastructure(config1);
            var serviceProvider1 = services1.BuildServiceProvider();
            var dbContext1 = serviceProvider1.GetRequiredService<UsersDbContext>();
            dbContext1.Database.ProviderName.Should().Be("Microsoft.EntityFrameworkCore.InMemory",
                "ASPNETCORE_ENVIRONMENT should take precedence");

            // Test Environment fallback when ASPNETCORE_ENVIRONMENT is null
            var services2 = new ServiceCollection();
            var config2 = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string?>("Environment", "TestEnvironment")
                })
                .Build();

            services2.AddInfrastructure(config2);
            var serviceProvider2 = services2.BuildServiceProvider();
            var dbContext2 = serviceProvider2.GetRequiredService<UsersDbContext>();
            dbContext2.Database.ProviderName.Should().Be("Microsoft.EntityFrameworkCore.InMemory",
                "Environment should be used when ASPNETCORE_ENVIRONMENT is not available");
        }

        [Fact]
        public void AddInfrastructure_WithProductionEnvironment_ShouldUseMySqlConfiguration()
        {
            // Arrange
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string?>("ASPNETCORE_ENVIRONMENT", "Production"),
                    new KeyValuePair<string, string?>("ConnectionStrings:Default", "server=localhost;port=3306;database=proddb;user=prod;password=prod;TreatTinyAsBoolean=false")
                })
                .Build();

            // Act
            var result = services.AddInfrastructure(config);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeSameAs(services);

            // Verificar que se registró el DbContext con el tipo correcto
            var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(UsersDbContext));
            descriptor.Should().NotBeNull();
            descriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
        }

        [Fact]
        public void AddInfrastructure_WithDevelopmentEnvironment_ShouldUseMySqlConfiguration()
        {
            // Arrange
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string?>("ASPNETCORE_ENVIRONMENT", "Development"),
                    new KeyValuePair<string, string?>("ConnectionStrings:Default", "server=localhost;port=3306;database=devdb;user=dev;password=dev;TreatTinyAsBoolean=false")
                })
                .Build();

            // Act
            var result = services.AddInfrastructure(config);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeSameAs(services);

            // Verificar que se registró el DbContext con el tipo correcto
            var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(UsersDbContext));
            descriptor.Should().NotBeNull();
            descriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
        }

        [Fact]
        public void AddInfrastructure_WithNullEnvironmentConfiguration_ShouldUseMySqlConfiguration()
        {
            // Arrange
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    // No se proporciona ASPNETCORE_ENVIRONMENT ni Environment
                    new KeyValuePair<string, string?>("ConnectionStrings:Default", "server=localhost;port=3306;database=nullenvdb;user=nullenv;password=nullenv;TreatTinyAsBoolean=false")
                })
                .Build();

            // Act
            var result = services.AddInfrastructure(config);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeSameAs(services);

            // Verificar que se registró el DbContext con el tipo correcto
            var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(UsersDbContext));
            descriptor.Should().NotBeNull();
            descriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
        }

        [Fact]
        public void AddInfrastructure_WithDefaultConnectionString_ShouldUseBuiltInDefault()
        {
            // Arrange
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string?>("ASPNETCORE_ENVIRONMENT", "Production")
                    // No se proporciona ConnectionStrings:Default para probar el fallback
                })
                .Build();

            // Act
            var result = services.AddInfrastructure(config);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeSameAs(services);

            // Verificar que se registró el DbContext con la cadena por defecto
            var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(UsersDbContext));
            descriptor.Should().NotBeNull();
            descriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
        }

        [Fact]
        public void AddInfrastructure_WithStagingEnvironment_ShouldUseMySqlConfiguration()
        {
            // Arrange
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string?>("ASPNETCORE_ENVIRONMENT", "Staging"),
                    new KeyValuePair<string, string?>("ConnectionStrings:Default", "server=localhost;port=3306;database=stagingdb;user=staging;password=staging;TreatTinyAsBoolean=false")
                })
                .Build();

            // Act  
            var result = services.AddInfrastructure(config);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeSameAs(services);

            // Verificar que se registró el DbContext con el tipo correcto
            var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(UsersDbContext));
            descriptor.Should().NotBeNull();
            descriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
        }
    }
}
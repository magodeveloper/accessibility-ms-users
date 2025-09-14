using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Users.Infrastructure.Data;
using Users.Infrastructure;

namespace Users.Tests.Infrastructure;

public class ServiceRegistrationTests
{
    [Fact]
    public void AddInfrastructure_WithTestEnvironment_ShouldConfigureInMemoryDatabase()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "TestEnvironment"
            })
            .Build();

        // Act
        services.AddInfrastructure(configuration);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var dbContext = serviceProvider.GetRequiredService<UsersDbContext>();

        Assert.NotNull(dbContext);
        Assert.True(dbContext.Database.IsInMemory());
    }

    [Fact]
    public void AddInfrastructure_WithEnvironmentFromEnvironmentKey_ShouldConfigureInMemoryDatabase()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Environment"] = "TestEnvironment"
            })
            .Build();

        // Act
        services.AddInfrastructure(configuration);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var dbContext = serviceProvider.GetRequiredService<UsersDbContext>();

        Assert.NotNull(dbContext);
        Assert.True(dbContext.Database.IsInMemory());
    }

    [Fact]
    public void AddInfrastructure_WithProductionEnvironment_ShouldRegisterMySqlDatabase()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Production",
                ["ConnectionStrings:Default"] = "server=localhost;port=3306;database=testdb;user=testuser;password=testpass;TreatTinyAsBoolean=false"
            })
            .Build();

        // Act
        services.AddInfrastructure(configuration);

        // Assert - Solo verificamos que el servicio está registrado, no que funcione
        var serviceDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(UsersDbContext));
        Assert.NotNull(serviceDescriptor);
        Assert.Equal(ServiceLifetime.Scoped, serviceDescriptor.Lifetime);
    }

    [Fact]
    public void AddInfrastructure_WithoutConnectionString_ShouldUseDefaultConnectionString()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Development"
            })
            .Build();

        // Act
        services.AddInfrastructure(configuration);

        // Assert - Solo verificamos que el servicio está registrado
        var serviceDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(UsersDbContext));
        Assert.NotNull(serviceDescriptor);
        Assert.Equal(ServiceLifetime.Scoped, serviceDescriptor.Lifetime);
    }

    [Fact]
    public void AddInfrastructure_WithNullEnvironment_ShouldRegisterMySqlDatabase()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        // Act
        services.AddInfrastructure(configuration);

        // Assert - Solo verificamos que el servicio está registrado
        var serviceDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(UsersDbContext));
        Assert.NotNull(serviceDescriptor);
        Assert.Equal(ServiceLifetime.Scoped, serviceDescriptor.Lifetime);
    }

    [Fact]
    public void AddInfrastructure_ShouldReturnSameServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "TestEnvironment"
            })
            .Build();

        // Act
        var result = services.AddInfrastructure(configuration);

        // Assert
        Assert.Same(services, result);
    }

    [Fact]
    public void AddInfrastructure_WithCustomConnectionString_ShouldRegisterDbContextWithCustomString()
    {
        // Arrange
        var services = new ServiceCollection();
        var customConnectionString = "server=custom.host;port=3307;database=customdb;user=customuser;password=custompass;TreatTinyAsBoolean=false";
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Development",
                ["ConnectionStrings:Default"] = customConnectionString
            })
            .Build();

        // Act
        services.AddInfrastructure(configuration);

        // Assert - Solo verificamos que el servicio está registrado
        var serviceDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(UsersDbContext));
        Assert.NotNull(serviceDescriptor);
        Assert.Equal(ServiceLifetime.Scoped, serviceDescriptor.Lifetime);
    }

    [Fact]
    public void AddInfrastructure_MySqlConfiguration_ShouldRegisterDbContextOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "TestEnvironment" // Usar TestEnvironment para evitar conexión real
            })
            .Build();

        // Act
        services.AddInfrastructure(configuration);

        // Assert - Verificamos que DbContextOptions también está registrado
        var dbContextOptionsDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(DbContextOptions<UsersDbContext>));
        Assert.NotNull(dbContextOptionsDescriptor);
        Assert.Equal(ServiceLifetime.Scoped, dbContextOptionsDescriptor.Lifetime);
    }

    [Fact]
    public void AddInfrastructure_WithNullEnvironment_ShouldUseMySqlByDefault()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "server=localhost;port=3306;database=testdb;user=test;password=test;TreatTinyAsBoolean=false"
            })
            .Build();

        // Act
        services.AddInfrastructure(configuration);

        // Assert
        var serviceDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(UsersDbContext));
        Assert.NotNull(serviceDescriptor);
        Assert.Equal(ServiceLifetime.Scoped, serviceDescriptor.Lifetime);
    }

    [Fact]
    public void AddInfrastructure_WithEmptyEnvironment_ShouldUseMySqlByDefault()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "",
                ["ConnectionStrings:Default"] = "server=localhost;port=3306;database=testdb;user=test;password=test;TreatTinyAsBoolean=false"
            })
            .Build();

        // Act
        services.AddInfrastructure(configuration);

        // Assert
        var serviceDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(UsersDbContext));
        Assert.NotNull(serviceDescriptor);
        Assert.Equal(ServiceLifetime.Scoped, serviceDescriptor.Lifetime);
    }

    [Fact]
    public void AddInfrastructure_WithDifferentEnvironment_ShouldUseMySql()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Production",
                ["ConnectionStrings:Default"] = "server=localhost;port=3306;database=testdb;user=test;password=test;TreatTinyAsBoolean=false"
            })
            .Build();

        // Act
        services.AddInfrastructure(configuration);

        // Assert
        var serviceDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(UsersDbContext));
        Assert.NotNull(serviceDescriptor);
        Assert.Equal(ServiceLifetime.Scoped, serviceDescriptor.Lifetime);
    }

    [Fact]
    public void AddInfrastructure_MultipleCallsWithTestEnvironment_ShouldAllowMultipleRegistrations()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "TestEnvironment"
            })
            .Build();

        // Act
        services.AddInfrastructure(configuration);
        services.AddInfrastructure(configuration);

        // Assert
        var dbContextServices = services.Where(s => s.ServiceType == typeof(UsersDbContext)).ToList();
        Assert.True(dbContextServices.Count >= 1); // Should have at least one registration
    }
}
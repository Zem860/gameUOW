  using System.Reflection;
  using Game.Abstractions.DependencyInjection;

  namespace Game.Web.Extensions
  {
      /// <summary>
      /// 慣例註冊擴展方法：掃描專案，自動登記實作標記介面的類別
      /// </summary>
      /// <remarks>
      /// <para>ITransientDependency / IScopedDependency / ISingletonDependency 決定「要不要登記、生命週期」。</para>
      /// <para>[ExposeServices] 決定「用哪些介面登記」；沒貼時，實作的介面（扣掉標記介面）與類別本身都會登記。</para>
      /// <para>泛型類別掃描不到（不知道要登記成哪種型別），要另外手動登記，例：MongoDbExtensions 的泛型
  Repository。</para>
      /// </remarks>
      public static class ConventionalRegistrationExtensions
      {
          /// <summary>標記介面：只用來決定生命週期，本身不登記</summary>
          private static readonly Type[] DependencyInterfaces =
          [
              typeof(ITransientDependency),
              typeof(IScopedDependency),
              typeof(ISingletonDependency),
          ];

          /// <summary>
          /// 掃描 Application、Infrastructure 專案，自動登記服務
          /// </summary>
          /// <param name="services">服務集合</param>
          /// <returns>服務集合（支援鏈式呼叫）</returns>
          public static IServiceCollection AddConventionalServices(this IServiceCollection services)
          {
              // 用專案名稱載入：Application 目前還沒有任何類別，無法用 typeof(某類別).Assembly 取得
              Assembly[] assemblies =
              [
                  Assembly.Load("Game.Application"),
                  Assembly.Load("Game.Infrastructure"),
              ];

              foreach (Assembly assembly in assemblies)
              {
                  RegisterServicesFromAssembly(services, assembly);
              }

              return services;
          }

          /// <summary>
          /// 登記單一專案內符合慣例的類別
          /// </summary>
          private static void RegisterServicesFromAssembly(IServiceCollection services, Assembly assembly)
          {
              IEnumerable<Type> types = assembly.GetTypes()
                  .Where(type => type.IsClass && !type.IsAbstract && !type.IsGenericType);

              foreach (Type type in types)
              {
                  ServiceLifetime? lifetime = ResolveLifetime(type);
                  if (lifetime is null)
                  {
                      continue;
                  }

                  foreach (Type serviceType in ResolveServiceTypes(type))
                  {
                      RegisterService(services, serviceType, type, lifetime.Value);
                  }
              }
          }

          /// <summary>
          /// 依標記介面決定生命週期；沒有標記介面回傳 null（不登記）
          /// </summary>
          private static ServiceLifetime? ResolveLifetime(Type type)
          {
              if (typeof(ITransientDependency).IsAssignableFrom(type))
              {
                  return ServiceLifetime.Transient;
              }

              if (typeof(IScopedDependency).IsAssignableFrom(type))
              {
                  return ServiceLifetime.Scoped;
              }

              if (typeof(ISingletonDependency).IsAssignableFrom(type))
              {
                  return ServiceLifetime.Singleton;
              }

              return null;
          }

          /// <summary>
          /// 決定要用哪些型別登記：有 [ExposeServices] 就只用它列出的介面，否則用所有介面（扣掉標記介面）加類別本身
          /// </summary>
          private static IEnumerable<Type> ResolveServiceTypes(Type type)
          {
              ExposeServicesAttribute? exposeServices = type.GetCustomAttribute<ExposeServicesAttribute>();
              if (exposeServices is { ServiceTypes.Length: > 0 })
              {
                  return exposeServices.ServiceTypes;
              }

              return type.GetInterfaces()
                  .Where(serviceType => !DependencyInterfaces.Contains(serviceType))
                  .Append(type);
          }

          /// <summary>
          /// 登記一筆服務；同樣的組合已登記過就略過
          /// </summary>
          private static void RegisterService(
              IServiceCollection services,
              Type serviceType,
              Type implementationType,
              ServiceLifetime lifetime)
          {
              if (services.Any(descriptor =>
                      descriptor.ServiceType == serviceType && descriptor.ImplementationType == implementationType))
              {
                  return;
              }

              services.Add(new ServiceDescriptor(serviceType, implementationType, lifetime));
          }
      }
  }
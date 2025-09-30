namespace ZapEditor.Services

open System
open System.Collections.Generic
open Microsoft.Extensions.DependencyInjection

type ServiceLifetime =
    | Singleton
    | Transient
    | Scoped

type ServiceRegistration =
    { Interface: Type
      Implementation: Type
      Lifetime: ServiceLifetime
      Factory: unit -> obj option }

type IDependencyContainer =
    abstract member RegisterService: Type -> Type -> ServiceLifetime -> unit
    abstract member RegisterService: Type -> (unit -> obj) -> ServiceLifetime -> unit
    abstract member RegisterService<'TInterface, 'TImplementation when 'TInterface : not struct and 'TImplementation :> 'TInterface> : ServiceLifetime -> unit
    abstract member RegisterInstance<'TInterface when 'TInterface : not struct> : 'TInterface -> unit
    abstract member RegisterSingleton<'TService when 'TService : not struct> : unit -> unit
    abstract member RegisterTransient<'TService when 'TService : not struct> : unit -> unit
    abstract member Resolve: Type -> obj
    abstract member Resolve<'T> : 'T
    abstract member Build: unit -> unit

type DependencyContainer() =
    let services = ServiceCollection()
    let mutable serviceProvider: ServiceProvider option = None
    let registrations = Dictionary<Type, ServiceRegistration>()
    let lockObj = obj()

    interface IDependencyContainer with
        member this.RegisterService(interfaceType: Type, implementationType: Type, lifetime: ServiceLifetime) =
            lock lockObj (fun () ->
                let serviceLifetime =
                    match lifetime with
                    | Singleton -> Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton
                    | Transient -> Microsoft.Extensions.DependencyInjection.ServiceLifetime.Transient
                    | Scoped -> Microsoft.Extensions.DependencyInjection.ServiceLifetime.Scoped

                match lifetime with
                | Singleton -> services.AddSingleton(interfaceType, implementationType) |> ignore
                | Transient -> services.AddTransient(interfaceType, implementationType) |> ignore
                | Scoped -> services.AddScoped(interfaceType, implementationType) |> ignore

                registrations.[interfaceType] <- {
                    Interface = interfaceType
                    Implementation = implementationType
                    Lifetime = lifetime
                    Factory = None
                })

        member this.RegisterService(interfaceType: Type, factory: unit -> obj, lifetime: ServiceLifetime) =
            lock lockObj (fun () ->
                let serviceLifetime =
                    match lifetime with
                    | Singleton -> Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton
                    | Transient -> Microsoft.Extensions.DependencyInjection.ServiceLifetime.Transient
                    | Scoped -> Microsoft.Extensions.DependencyInjection.ServiceLifetime.Scoped

                match lifetime with
                | Singleton -> services.AddSingleton(interfaceType, fun sp -> factory()) |> ignore
                | Transient -> services.AddTransient(interfaceType, fun sp -> factory()) |> ignore
                | Scoped -> services.AddScoped(interfaceType, fun sp -> factory()) |> ignore

                registrations.[interfaceType] <- {
                    Interface = interfaceType
                    Implementation = interfaceType
                    Lifetime = lifetime
                    Factory = Some factory
                })

        member this.RegisterService<'TInterface, 'TImplementation when 'TInterface : not struct and 'TImplementation :> 'TInterface>(lifetime: ServiceLifetime) =
            (this :> IDependencyContainer).RegisterService(typeof<'TInterface>, typeof<'TImplementation>, lifetime)

        member this.RegisterInstance<'TInterface when 'TInterface : not struct>(instance: 'TInterface) =
            lock lockObj (fun () ->
                services.AddSingleton<'TInterface>(instance) |> ignore
                registrations.[typeof<'TInterface>] <- {
                    Interface = typeof<'TInterface>
                    Implementation = typeof<'TInterface>
                    Lifetime = Singleton
                    Factory = Some (fun () -> upcast instance)
                })

        member this.RegisterSingleton<'TService when 'TService : not struct>() =
            (this :> IDependencyContainer).RegisterService<'TService, 'TService>(Singleton)

        member this.RegisterTransient<'TService when 'TService : not struct>() =
            (this :> IDependencyContainer).RegisterService<'TService, 'TService>(Transient)

        member this.Resolve(serviceType: Type) : obj =
            match serviceProvider with
            | Some sp -> sp.GetRequiredService(serviceType)
            | None -> raise (InvalidOperationException("Container has not been built. Call Build() first."))

        member this.Resolve<'T> : 'T =
            (this :> IDependencyContainer).Resolve(typeof<'T>) :?> 'T

        member this.Build() =
            lock lockObj (fun () ->
                match serviceProvider with
                | Some _ -> ()
                | None ->
                    serviceProvider <- Some services.BuildServiceProvider())

// Simple service locator
module ServiceLocator =
    let mutable private container: IDependencyContainer option = None

    let Initialize(container: IDependencyContainer) =
        ServiceLocator.container <- Some container

    let GetService<'T>() : 'T =
        match container with
        | Some c -> c.Resolve<'T>()
        | None -> raise (InvalidOperationException("ServiceLocator not initialized"))

    let GetService(serviceType: Type) : obj =
        match container with
        | Some c -> c.Resolve(serviceType)
        | None -> raise (InvalidOperationException("ServiceLocator not initialized"))

    let TryGetService<'T>() : 'T option =
        try
            match container with
            | Some c -> Some (c.Resolve<'T>())
            | None -> None
        with
        | _ -> None

// アプリケーションコンポジションルート
module ApplicationComposition =
    let CreateContainer() : IDependencyContainer =
        let container = DependencyContainer()

        // 設定サービスを登録
        container.RegisterSingleton<IConfigurationService>(fun () ->
            ConfigurationService.Load() :> IConfigurationService)

        // ログサービスを登録
        container.RegisterSingleton<ILoggerService>(fun () ->
            let configService = container.Resolve<IConfigurationService>()
            LoggingService.Create(configService) :> ILoggerService)

        // ファイルサービスを登録
        container.RegisterTransient<IFileService>(fun () ->
            let configService = container.Resolve<IConfigurationService>()
            FileService(?configService = Some configService) :> IFileService)

        // ツール検証サービスを登録
        container.RegisterTransient<IExternalToolValidator>(fun () ->
            ExternalToolValidator() :> IExternalToolValidator)

        // MainWindowViewModelを登録
        container.RegisterTransient<MainWindowViewModel>(fun () ->
            let fileService = container.Resolve<IFileService>()
            let toolValidator = container.Resolve<IExternalToolValidator>()
            let configService = container.Resolve<IConfigurationService>()
            let loggerService = container.Resolve<ILoggerService>()
            MainWindowViewModel(
                ?fileService = Some fileService,
                ?toolValidator = Some toolValidator,
                ?configService = Some configService,
                ?loggerService = Some loggerService
            ))

        container.Build()
        container

    let InitializeApplication() =
        let container = CreateContainer()
        ServiceLocator.Initialize(container)

        // ログを初期化
        let loggerService = container.Resolve<ILoggerService>()
        LogHelper.Initialize(loggerService)

        container
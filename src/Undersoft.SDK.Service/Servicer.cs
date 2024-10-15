﻿using MediatR;

namespace Undersoft.SDK.Service;

using Invoking;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using System.Threading.Tasks;
using Undersoft.SDK.Service.Data.Repository.Client;
using Undersoft.SDK.Service.Data.Repository.Source;
using Undersoft.SDK.Service.Operation.Command.Notification;

public class Servicer : ServiceManager, IServicer, IMediator
{
    new bool disposedValue;
    protected IMediator mediator;

    public Servicer() : base() { }

    public Servicer(IServiceManager serviceManager) : base(serviceManager) { }

    public IMediator Mediator => mediator ??= Session.GetService<IMediator>();   

    public Task<R> Run<T, R>(Func<T, Task<R>> function) where T : class
    {
        using (var servicer = CreateServicer())
        {
            return function.Invoke(servicer.Session.GetService<T>());
        }
    }

    public Task Run<T>(Func<T, Task> function) where T : class
    {
        using (var servicer = CreateServicer())
        {
            return function.Invoke(servicer.Session.GetService<T>());
        }
    }

    public Task<object> Run<T>(string methodname, params object[] parameters) where T : class
    {
        using (var servicer = CreateServicer())
        {
            return new Invoker(servicer.Session.GetService<T>(), methodname).InvokeAsync(parameters);
        }
    }

    public Task<R> Run<T, R>(string methodname, params object[] parameters) where T : class where R : class
    {
        using (var servicer = CreateServicer())
        {
            Invoker deputy = new Invoker(
            servicer.Session.GetService<T>(),
            methodname,
            parameters.ForEach(p => p.GetType()).Commit()
        );
            return deputy.InvokeAsync<R>(parameters);
        }
    }

    public Task<object> Run<T>(Arguments arguments) where T : class
    {
        using (var servicer = CreateServicer())
        {
            Invoker deputy = new Invoker<T>(servicer.Session.GetService<T>(), arguments);
            return deputy.InvokeAsync(arguments);
        }
    }

    public Task<R> Run<T, R>(Arguments arguments) where T : class where R : class
    {
        using (var servicer = CreateServicer())
        {
            Invoker deputy = new Invoker<T>(servicer.Session.GetService<T>(), arguments);
            return deputy.InvokeAsync<R>(arguments);
        }
    }

    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        using (var servicer = CreateServicer())
        {
            return await servicer.Mediator.Send(request, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<object> Send(object request, CancellationToken cancellationToken = default)
    {
        using (var servicer = CreateServicer())
        {
            return await servicer.Mediator.Send(request, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
    {
        using (var servicer = CreateServicer())
        {
            await servicer.Mediator.Send(request, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task Publish(object notification, CancellationToken cancellationToken = default)
    {
        using (var servicer = CreateServicer())
        {
            await servicer.Mediator.Publish(notification, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification
    {
        using (var servicer = CreateServicer())
        {
            await servicer.Mediator.Publish(notification, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task<TResponse> Report<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        return await Mediator.Send(request, cancellationToken);
    }

    public async Task<object> Report(object request, CancellationToken cancellationToken = default)
    {
        return await Mediator.Send(request, cancellationToken).ConfigureAwait(false);
    }   

    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        using (var servicer = CreateServicer())
        {
            return servicer.Mediator
             .CreateStream(request, cancellationToken);
        }
    }

    public IAsyncEnumerable<object> CreateStream(object request, CancellationToken cancellationToken = default)
    {
        using (var servicer = CreateServicer())
        {
            return servicer.Mediator
             .CreateStream(request, cancellationToken);
        }
    }

    public async Task<R> Serve<T, R>(Func<T, Task<R>> function) where T : class
    {
        using (var servicer = CreateServicer())
        {
            return await function.Invoke(servicer.Session.GetService<T>());
        }
    }

    public async Task Serve<T>(Func<T, Task> function) where T : class
    {
        using (var servicer = CreateServicer())
        {
            await function.Invoke(servicer.Session.GetService<T>()).ConfigureAwait(false);
        }
    }

    public async Task Serve<T>(string methodname, params object[] parameters) where T : class
    {
        using (var servicer = CreateServicer())
        {
            await new Invoker(
            servicer.Session.GetService<T>(),
            methodname
        ).InvokeAsync(parameters).ConfigureAwait(false); ;
        }
    }

    public async Task<R> Serve<T, R>(string methodname, params object[] parameters) where T : class where R : class
    {
        using (var servicer = CreateServicer())
        {
            return await new Invoker(
            servicer.Session.GetService<T>(),
            methodname
        ).InvokeAsync<R>(parameters).ConfigureAwait(false); ;
        }
    }

    public async Task Serve<T>(string methodname, Arguments arguments) where T : class
    {
        using (var servicer = CreateServicer())
        {
            await new Invoker(
            servicer.Session.GetService<T>(),
            methodname,
            arguments.TypeArray
        ).InvokeAsync(arguments).ConfigureAwait(false); ;
        }
    }

    public async Task<R> Serve<T, R>(string methodname, Arguments arguments) where T : class where R : class
    {
        using (var servicer = CreateServicer())
        {
            return await new Invoker(
               servicer.Session.GetService<T>(),
                methodname,
                arguments.TypeArray
            ).InvokeAsync<R>(arguments).ConfigureAwait(false); ;
        }
    }

    public Lazy<R> LazyServe<T, R>(Func<T, R> function)
       where T : class
       where R : class
    {

        return new Lazy<R>(() => {
            using (var servicer = CreateServicer())
            {
                return function.Invoke(servicer.Session.GetService<T>());
            }
        });
    }

    public async Task Save(bool asTransaction = false)
    {
        await SaveStores(true);
        await SaveClients(true);
    }

    public async Task<int> SaveClient(IRepositoryClient client, bool asTransaction = false)
    {
        return await client.Save(asTransaction);
    }

    public async Task<int> SaveClients(bool asTransaction = false)
    {
        int changes = 0;
        for (int i = 0; i < Clients.Count; i++)
        {
            changes += await SaveClient(Clients[i], asTransaction);
        }

        return changes;
    }

    public async Task<int> SaveStore(IRepositorySource source, bool asTransaction = false)
    {
        return await source.Save(asTransaction);
    }

    public async Task<int> SaveStores(bool asTransaction = false)
    {
        int changes = 0;
        for (int i = 0; i < Sources.Count; i++)
        {
            changes += await SaveStore(Sources[i], asTransaction);
        }

        return changes;
    }

    public override ValueTask DisposeAsyncCore()
    {
        return base.DisposeAsyncCore();
    }

    protected override void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)            
                base.Dispose(true);
            
            disposedValue = true;
        }
    }


}

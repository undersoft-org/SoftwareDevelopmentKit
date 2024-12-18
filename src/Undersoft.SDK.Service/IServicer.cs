﻿using MediatR;
using Undersoft.SDK.Service.Access;
using Undersoft.SDK.Service.Data.Repository.Client;
using Undersoft.SDK.Service.Data.Repository.Source;

namespace Undersoft.SDK.Service
{
    public interface IServicer : IServiceManager, IDisposable
    {    
        IMediator Mediator { get; }
        void SetAuthorization(IAuthorization auth);
        IAsyncEnumerable<object> CreateStream(object request, CancellationToken cancellationToken = default);
        IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default);
        Lazy<R> LazyServe<T, R>(Func<T, R> function)
            where T : class
            where R : class;
       
        Task<R> Run<T, R>(Func<T, Task<R>> function) where T : class;
        Task<R> Run<T, R>(string methodname, params object[] parameters) where T : class where R : class;
        Task Run<T>(Func<T, Task> function) where T : class;
        Task<object> Run<T>(string methodname, params object[] parameters) where T : class;
        Task<R> Run<T, R>(Arguments arguments) where T : class where R : class;
        Task<object> Run<T>(Arguments arguments) where T : class;
        Task Save(bool asTransaction = false);
        Task<int> SaveClient(IRepositoryClient client, bool asTransaction = false);
        Task<int> SaveClients(bool asTransaction = false);
        Task<int> SaveStore(IRepositorySource endpoint, bool asTransaction = false);
        Task<int> SaveStores(bool asTransaction = false);
        Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
        Task<object> Send(object request, CancellationToken cancellationToken = default);
        Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest;
        Task Publish(object notification, CancellationToken cancellationToken = default);
        Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification;
        Task<object> Report(object request, CancellationToken cancellationToken = default);
        Task<TResponse> Report<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);       
        Task<R> Serve<T, R>(Func<T, Task<R>> function) where T : class;
        Task<R> Serve<T, R>(string methodname, params object[] parameters) where T : class where R : class;
        Task Serve<T>(Func<T, Task> function) where T : class;
        Task Serve<T>(string methodname, params object[] parameters) where T : class;
    }
}
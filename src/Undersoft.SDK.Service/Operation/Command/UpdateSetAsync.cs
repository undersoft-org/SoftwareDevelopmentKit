﻿using MediatR;
using System.Linq.Expressions;

namespace Undersoft.SDK.Service.Operation.Command;

using Undersoft.SDK.Proxies;
using Undersoft.SDK.Service.Data.Event;
using Undersoft.SDK.Service.Data.Store;

public class UpdateSetAsync<TStore, TEntity, TDto>
    : UpdateSet<TStore, TEntity, TDto>,
        IStreamRequest<Command<TDto>>
    where TEntity : class, IOrigin, IInnerProxy
    where TDto : class, IOrigin, IInnerProxy
    where TStore : IDataServerStore
{
    public UpdateSetAsync(PublishMode publishPattern, TDto input, object key)
        : base(publishPattern, input, key) { }

    public UpdateSetAsync(PublishMode publishPattern, TDto[] inputs)
        : base(publishPattern, inputs) { }

    public UpdateSetAsync(
        PublishMode publishPattern,
        TDto[] inputs,
        Func<TDto, Expression<Func<TEntity, bool>>> predicate
    ) : base(publishPattern, inputs, predicate) { }

    public UpdateSetAsync(
        PublishMode publishPattern,
        TDto[] inputs,
        Func<TDto, Expression<Func<TEntity, bool>>> predicate,
        params Func<TDto, Expression<Func<TEntity, bool>>>[] conditions
    ) : base(publishPattern, inputs, predicate, conditions) { }
}

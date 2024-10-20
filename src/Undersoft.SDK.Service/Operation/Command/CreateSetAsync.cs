﻿using MediatR;
using System.Linq.Expressions;
using Undersoft.SDK.Service.Data.Event;

namespace Undersoft.SDK.Service.Operation.Command;

public class CreateSetAsync<TStore, TEntity, TDto>
    : CreateSet<TStore, TEntity, TDto>,
        IStreamRequest<Command<TDto>>
    where TEntity : class, IOrigin, IInnerProxy
    where TDto : class, IOrigin, IInnerProxy
    where TStore : IDataServerStore
{
    public CreateSetAsync(PublishMode publishPattern, TDto input, object key)
        : base(publishPattern, input, key) { }

    public CreateSetAsync(PublishMode publishPattern, TDto[] inputs)
        : base(publishPattern, inputs) { }

    public CreateSetAsync(
        PublishMode publishPattern,
        TDto[] inputs,
        Func<TEntity, Expression<Func<TEntity, bool>>> predicate
    ) : base(publishPattern, inputs, predicate) { }
}

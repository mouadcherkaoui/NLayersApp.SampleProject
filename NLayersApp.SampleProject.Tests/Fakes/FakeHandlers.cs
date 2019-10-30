using MediatR;
using NLayersApp.CQRS.Requests;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NLayersApp.Controllers.Tests.Fakes
{
    public class FakeReadEntitiesHandler<TEntity> : IRequestHandler<ReadEntitiesRequest<TEntity>, IEnumerable<TEntity>>
        where TEntity : class
    {
        IEnumerable<TEntity> _entities;
        public FakeReadEntitiesHandler(IEnumerable<TEntity> entities)
        {
            _entities = entities;
        }

        public Task<IEnumerable<TEntity>> Handle(ReadEntitiesRequest<TEntity> request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_entities);
        }
    }

    public class FakeReadEntityHandler<TKey, TEntity> : IRequestHandler<ReadEntityRequest<TKey, TEntity>, TEntity>
        where TEntity : class
    {
        List<TEntity> _entities;
        public FakeReadEntityHandler(List<TEntity> entities)
        {
            _entities = entities;
        }

        public Task<TEntity> Handle(ReadEntityRequest<TKey, TEntity> request, CancellationToken cancellationToken)
        {
            var keyProperty = typeof(TEntity).GetProperties().FirstOrDefault(p => p.GetCustomAttribute<KeyAttribute>() != null);
            return Task.FromResult(_entities.FirstOrDefault(e => ((TKey)keyProperty.GetValue(e)).Equals(request.Key)));
        }
    }

    public class FakeDeleteEntityHandler<TKey, TEntity> : IRequestHandler<DeleteEntityRequest<TKey, TEntity>, bool>
        where TEntity : class
    {
        List<TEntity> _entities;
        public FakeDeleteEntityHandler(List<TEntity> entities)
        {
            _entities = entities;
        }

        public Task<bool> Handle(DeleteEntityRequest<TKey, TEntity> request, CancellationToken cancellationToken)
        {
            var keyProperty = typeof(TEntity).GetProperties().FirstOrDefault(p => p.GetCustomAttribute<KeyAttribute>() != null);
            return Task.Run(() =>
            {
                var item = _entities.FirstOrDefault(e => ((TKey)keyProperty.GetValue(e)).Equals(request.Key));
                return _entities.Remove(item);
            });
        }
    }

    public class FakeUpdateEntityHandler<TKey, TEntity> : IRequestHandler<UpdateEntityRequest<TKey, TEntity>, TEntity>
        where TEntity : class
    {
        List<TEntity> _entities;
        public FakeUpdateEntityHandler(List<TEntity> entities)
        {
            _entities = entities;
        }

        public Task<TEntity> Handle(UpdateEntityRequest<TKey, TEntity> request, CancellationToken cancellationToken)
        {
            var keyProperty = typeof(TEntity).GetProperties().FirstOrDefault(p => p.GetCustomAttribute<KeyAttribute>() != null);
            return Task.Run(() =>
            {
                var item = _entities.FirstOrDefault(e => ((TKey)keyProperty.GetValue(e)).Equals(request.Key));
                item = request.Entity;
                return item;
            });
        }
    }

    public class FakeCreateEntityHandler<TKey, TEntity> : IRequestHandler<CreateEntityRequest<TEntity>, TEntity>
        where TEntity : class
    {
        List<TEntity> _entities;
        public FakeCreateEntityHandler(List<TEntity> entities)
        {
            _entities = entities;
        }

        public Task<TEntity> Handle(CreateEntityRequest<TEntity> request, CancellationToken cancellationToken)
        {
            var keyProperty = typeof(TEntity).GetProperties().FirstOrDefault(p => p.GetCustomAttribute<KeyAttribute>() != null);
            return Task.Run(() =>
            {
                _entities.Add(request.Entity);
                return request.Entity;
            });
        }
    }
}

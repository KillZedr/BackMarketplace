using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Payment.Application.Payment_DAL.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Payment.Application.Payment_DAL.RealisationInterfaces
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly DbContext _dbContext;
        private IDbContextTransaction _dbTransaction;
        public UnitOfWork(DbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public IDbContextTransaction BeginTransaction()
        {
            lock (_dbContext)
            {
                if (_dbTransaction != null)
                {
                    throw new UnitOfWorkAlreadyInTransactionStateException();
                }

                _dbTransaction = _dbContext.Database.BeginTransaction();
            }
            return _dbTransaction;
        }

        public Task<int> SaveShangesAsync()
        {
            return _dbContext.SaveChangesAsync();
        }

        IRepository<TEntity> IUnitOfWork.GetRepository<TEntity>()
        {
            return new Repository<TEntity>(_dbContext);
        }
    }
}

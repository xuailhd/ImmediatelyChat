using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.ModelConfiguration;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Xugl.ImmediatelyChat.Core;
using Xugl.ImmediatelyChat.Data.EF.Mapping;
using Xugl.ImmediatelyChat.Model;

namespace Xugl.ImmediatelyChat.Data.EF
{
    public class DefaultDBContext:DbContext,IDbContext
    {
        public DefaultDBContext()
            : base("Xugl.ImmediatelyChat.DBContext")
        {
            this.Configuration.ProxyCreationEnabled = false;
            Database.SetInitializer(new DropCreateDatabaseIfModelChanges<DefaultDBContext>());
        }

        public new IDbSet<TEntity> Set<TEntity>() where TEntity : class
        {
            return base.Set<TEntity>();
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();

            var mappings = Assembly.GetExecutingAssembly().GetTypes()
                .Where(type => type.BaseType != null && type.BaseType.IsGenericType && type.BaseType.GetGenericTypeDefinition() == typeof(EntityTypeConfiguration<>));

            if(!string.IsNullOrEmpty(System.Configuration.ConfigurationManager.AppSettings["ModelMapping"].ToString()))
            {
                mappings = mappings.Where(type => type.Namespace == System.Configuration.ConfigurationManager.AppSettings["ModelMapping"].ToString());
            }

            foreach(var mapping in mappings)
            {
                dynamic configurationInstance = Activator.CreateInstance(mapping);
                modelBuilder.Configurations.Add(configurationInstance);
            }

            base.OnModelCreating(modelBuilder);
        }

        public IList<TEntity> ExecuteStoredProduceList<TEntity>(string commandText, Func<TEntity, bool> autoDetectPredicate, params object[] parameters) where TEntity : class, new()
        {
            if (parameters != null && parameters.Length > 0)
            {

                for (int i = 0; i < parameters.Length; i++)
                {
                    var p = parameters[i] as DbParameter;
                    if (p == null)
                        throw new Exception("Not support parameter type");
                    commandText += i == 0 ? " " : ",";

                    commandText += "@" + p.ParameterName;
                    if (p.Direction == ParameterDirection.InputOutput || p.Direction == ParameterDirection.Output)
                    {
                        commandText += " OutPut";
                    }
                }
            }

            var result = this.Database.SqlQuery<TEntity>(commandText, parameters).ToList();

            bool acd = this.Configuration.AutoDetectChangesEnabled;

            try
            {
                this.Configuration.AutoDetectChangesEnabled = false;
                for (int i = 0; i < result.Count; i++)
                {
                    result[i] = AttachEntityToContext<TEntity>(result[i], autoDetectPredicate);
                }
            }
            finally
            {
                this.Configuration.AutoDetectChangesEnabled = acd;
            }
            return result;

        }

        public IEnumerable<TElement> SqlQuery<TElement>(string sql, params object[] parameters)
        {
            return this.Database.SqlQuery<TElement>(sql, parameters);
        }

        public int ExecuteSqlCommand(string sql, bool doNotEnsureTrancation = false, int? timeout = null, params object[] parameters)
        {
            int? preTimeout = null;
            if(timeout.HasValue)
            {
                preTimeout = ((IObjectContextAdapter)this).ObjectContext.CommandTimeout;
                ((IObjectContextAdapter)this).ObjectContext.CommandTimeout = timeout;
            }

            var transactionalBehavior = doNotEnsureTrancation ? TransactionalBehavior.DoNotEnsureTransaction : TransactionalBehavior.EnsureTransaction;
            var result = this.Database.ExecuteSqlCommand(transactionalBehavior, sql, parameters);

            if(timeout.HasValue)
            {
                ((IObjectContextAdapter)this).ObjectContext.CommandTimeout = preTimeout;
            }
            return result;
        }

        protected TEntity AttachEntityToContext<TEntity>(TEntity entity,Func<TEntity,bool> predicate) where TEntity:class
        {
            var alreadyAttached = Set<TEntity>().Local.FirstOrDefault(predicate);
            if(alreadyAttached==null)
            {
                Set<TEntity>().Attach(entity);
                return entity;
            }
            else
            {
                return alreadyAttached;
            }
        }

    }


    //public class  USMDBInitializer : DropCreateDatabaseIfModelChanges<DefaultDBContext>
    //{
    //    protected override void Seed(DefaultDBContext context)
    //    {
    //        ContactGroup contactGroup = new ContactGroup();
    //        contactGroup.GroupName = "Group1";
    //        contactGroup.GroupObjectID = "Group1";

    //        context.Set<ContactGroup>().Add(contactGroup);
    //        context.SaveChanges();

    //        //base.Seed(context);
    //    }
    //}
}

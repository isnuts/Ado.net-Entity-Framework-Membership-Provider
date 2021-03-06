﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace OmidID.Web.Security.DataContext {
    internal class InternalUserContext
#if USE_WEBMATRIX
        <TUser, TOAuthMembership, TKey> : DbContext
        where TUser : class
        where TOAuthMembership : class
        where TKey : struct {

        public DefaultUserContext<TUser, TOAuthMembership, TKey> Context { get; set; }
        public InternalUserContext(DefaultUserContext<TUser, TOAuthMembership, TKey> Context)
#else
<TUser, TKey> : DbContext
        where TUser : class
        where TKey : struct {

        public DefaultUserContext<TUser, TKey> Context { get; set; }
        public InternalUserContext(DefaultUserContext<TUser, TKey> Context)
#endif

            : base(Context.Provider.ConnectionString) {
            this.Context = Context;
            this.Configuration.AutoDetectChangesEnabled = false;
            //if (Context.Provider.AutoGenerateDatabase) {
            //    Database.SetInitializer(new DropCreateDatabaseIfModelChanges<InternalContext<TUser, TKey>>());
            //}
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder) {
            var type = typeof(DbModelBuilder);
            var method = type.GetMethod("Entity", new Type[] { });
            if (Context.Provider.SupportApplication) {
                var output = method.MakeGenericMethod(Context.ApplicationType).Invoke(modelBuilder, null);
                output.GetType().InvokeMember("ToTable", System.Reflection.BindingFlags.InvokeMethod, null, output, new object[] { Context.ApplicationTableName.Name, Context.ApplicationTableName.Schema });
            }

            foreach (var item in Context.Helper.TableNames) {
                var output = method.MakeGenericMethod(item.Key).Invoke(modelBuilder, null);
                output.GetType().InvokeMember("ToTable", System.Reflection.BindingFlags.InvokeMethod, null, output, new object[] { item.Value.Name, item.Value.Schema });
            }

#if USE_WEBMATRIX
            foreach (var item in Context.Helper_OAuth.TableNames) {
                var output = method.MakeGenericMethod(item.Key).Invoke(modelBuilder, null);
                output.GetType().InvokeMember("ToTable", System.Reflection.BindingFlags.InvokeMethod, null, output, new object[] { item.Value.Name, item.Value.Schema });
            }
#endif
        }

        public System.Data.Entity.DbSet<TUser> Users { get; set; }
#if USE_WEBMATRIX
        public System.Data.Entity.DbSet<TOAuthMembership> OAuthMembership { get; set; }
#endif

        Models.Application application;
        public Models.Application GetApplication() {
            if (!Context.Provider.SupportApplication) return null;
            if (application != null) return application;

            var myType = this.GetType();
            var method = myType.GetMethod("Set", new Type[] { }).MakeGenericMethod(Context.ApplicationType);
            var obj = method.Invoke(this, null);

            var dbType = obj.GetType();
            var Application = ((IEnumerable<Models.Application>)obj).FirstOrDefault(f => f.Name == Context.Provider.ApplicationName);
            if (Application == null) {
                Application = Activator.CreateInstance(Context.ApplicationType) as Models.Application;
                Application.Name = Context.Provider.ApplicationName;

                dbType.InvokeMember("Add", System.Reflection.BindingFlags.InvokeMethod, null, obj, new object[] { Application });
                SaveChanges();
            }

            application = Application;
            return application;
        }

    }
}

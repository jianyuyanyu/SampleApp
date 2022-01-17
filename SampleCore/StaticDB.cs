﻿using FreeSql.Internal;
using FreeSql.Internal.CommonProvider;
using System;
using System.Threading;

namespace FreeSql
{
    public class StaticDB : StaticDB<string> { }

    public abstract class StaticDB<DBKey>
    {
        protected static Lazy<IFreeSql> multiFreeSql = new Lazy<IFreeSql>(() => new MultiFreeSql());
        public static IFreeSql Instance => multiFreeSql.Value;
    }

    public class MultiFreeSql : MultiFreeSql<string> { }

    public class MultiFreeSql<TDBKey> : BaseDbProvider, IFreeSql
    {
        internal TDBKey _dbkeyMaster;
        internal AsyncLocal<TDBKey> _dbkeyCurrent = new AsyncLocal<TDBKey>();
        BaseDbProvider _ormMaster => _ib.Get(_dbkeyMaster) as BaseDbProvider;
        BaseDbProvider _ormCurrent => _ib.Get(Equals(_dbkeyCurrent.Value, default(TDBKey)) ? _dbkeyMaster : _dbkeyCurrent.Value) as BaseDbProvider;
        internal IdleBus<TDBKey, IFreeSql> _ib;

        public MultiFreeSql()
        {
            _ib = new IdleBus<TDBKey, IFreeSql>();
            _ib.Notice += (_, __) => { };
        }

        public override IAdo Ado => _ormCurrent.Ado;
        public override IAop Aop => _ormCurrent.Aop;
        public override ICodeFirst CodeFirst => _ormCurrent.CodeFirst;
        public override IDbFirst DbFirst => _ormCurrent.DbFirst;
        public override GlobalFilter GlobalFilter => _ormCurrent.GlobalFilter;
        public override void Dispose() => _ib.Dispose();

        public override CommonExpression InternalCommonExpression => _ormCurrent.InternalCommonExpression;
        public override CommonUtils InternalCommonUtils => _ormCurrent.InternalCommonUtils;

        public override ISelect<T1> CreateSelectProvider<T1>(object dywhere) => _ormCurrent.CreateSelectProvider<T1>(dywhere);
        public override IDelete<T1> CreateDeleteProvider<T1>(object dywhere) => _ormCurrent.CreateDeleteProvider<T1>(dywhere);
        public override IUpdate<T1> CreateUpdateProvider<T1>(object dywhere) => _ormCurrent.CreateUpdateProvider<T1>(dywhere);
        public override IInsert<T1> CreateInsertProvider<T1>() => _ormCurrent.CreateInsertProvider<T1>();
        public override IInsertOrUpdate<T1> CreateInsertOrUpdateProvider<T1>() => _ormCurrent.CreateInsertOrUpdateProvider<T1>();
    }

    public static class MultiFreeSqlExtensions
    {
        public static IFreeSql ChangeDB<TDBKey>(this IFreeSql fsql, TDBKey dbkey)
        {
            var multiFsql = fsql as MultiFreeSql<TDBKey>;
            if (multiFsql == null) throw new Exception("fsql 类型不是 MultiFreeSql<TDBKey>");
            multiFsql._dbkeyCurrent.Value = dbkey;
            return multiFsql;
        }

        public static IDisposable Change<TDBKey>(this IFreeSql Instance, TDBKey dbkey)
        {
            var olddbkey = (Instance as MultiFreeSql<TDBKey>)._dbkeyCurrent.Value;
            Instance.ChangeDB(dbkey);
            return new DBChangeDisposable(() => Instance.ChangeDB(olddbkey));
        }

        public static IFreeSql Register<TDBKey>(this IFreeSql fsql, TDBKey dbkey, Func<IFreeSql> create)
        {
            var multiFsql = fsql as MultiFreeSql<TDBKey>;
            if (multiFsql == null) throw new Exception("fsql 类型不是 MultiFreeSql<TDBKey>");
            if (multiFsql._ib.TryRegister(dbkey, create))
                if (multiFsql._ib.GetKeys().Length == 1)
                    multiFsql._dbkeyMaster = dbkey;
            return multiFsql;
        }
    }

    class DBChangeDisposable : IDisposable
    {
        Action _cancel;
        public DBChangeDisposable(Action cancel) => _cancel = cancel;
        public void Dispose() => _cancel?.Invoke();
    }
}

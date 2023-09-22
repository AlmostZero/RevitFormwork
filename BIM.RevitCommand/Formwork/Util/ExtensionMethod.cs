using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace BIM.RevitCommand.Formwork.Util
{
    internal static class ExtensionMethod
    {
        public static IQueryable<TSource> Distinct<TSource>(
    this IQueryable<TSource> source, Expression<Func<TSource, object>> predicate )
        {
            // TODO: Null-check arguments
            return from item in source.GroupBy( predicate ) select item.First();
        }

        public static IEnumerable<TSource> Distinct<TSource>(
            this IEnumerable<TSource> source, Func<TSource, object> predicate )
        {
            // TODO: Null-check arguments
            return from item in source.GroupBy( predicate ) select item.First();
        }

        public static IEnumerable<TSource> DistinctBy<TSource, TKey>
         ( this IEnumerable<TSource> source, Func<TSource, TKey> keySelector )
        {
            HashSet<TKey> knownKeys = new HashSet<TKey>();
            foreach ( TSource element in source )
            {
                if ( knownKeys.Add( keySelector( element ) ) )
                {
                    yield return element;
                }
            }
        }
    }

    public class GeneralPropertyComparer<T, TKey> : IEqualityComparer<T>
    {
        private Func<T, TKey> expr { get; set; }
        public GeneralPropertyComparer( Func<T, TKey> expr )
        {
            this.expr = expr;
        }
        public bool Equals( T left, T right )
        {
            var leftProp = expr.Invoke( left );
            var rightProp = expr.Invoke( right );
            if ( leftProp == null && rightProp == null )
                return true;
            else if ( leftProp == null ^ rightProp == null )
                return false;
            else
                return leftProp.Equals( rightProp );
        }
        public int GetHashCode( T obj )
        {
            var prop = expr.Invoke( obj );
            return ( prop == null ) ? 0 : prop.GetHashCode();
        }
    }

}

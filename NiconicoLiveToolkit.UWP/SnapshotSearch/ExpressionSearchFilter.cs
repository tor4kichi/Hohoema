using Microsoft.Toolkit.Diagnostics;
using NiconicoToolkit.SnapshotSearch.Filters;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace NiconicoToolkit.SnapshotSearch
{
    public class ExpressionSearchFilter : ISearchFilter
	{
		private List<KeyValuePair<string, string>> _keyValues;
        private readonly Expression<Func<SnapshotVideoItem, bool>> _compareOpExpression;

        public ExpressionSearchFilter(Expression<Func<SnapshotVideoItem, bool>> compareOpExpression)
		{
            _compareOpExpression = compareOpExpression;
        }

		public IEnumerable<KeyValuePair<string, string>> GetFilterKeyValues()
		{
			return _keyValues ?? (_keyValues = ConvertToKeyValues(_compareOpExpression.Body as BinaryExpression));
		}

		private List<KeyValuePair<string, string>> ConvertToKeyValues(BinaryExpression operation)
		{
			// see@: https://site.nicovideo.jp/search-api-docs/search.html
			
			// フィルタとして指定できるパラメータにfitlersとjsonFiltersがあり
			// jsonFiltersの方でのみNOTやORの条件検索ができます。ただしクライアント側にしろAPI側にしろ処理が重い。
			// filtersは常にANDのみで除外条件指定もできませんが軽いです。

			// 故に、BinaryExpressionとして OR か NOT が含まれる場合は常にJsonFilterとして構築し
			// 単一の比較、または複数の比較をAndのみで繋げている場合はfiltersとして構築します

			// 事前にBinaryExpressionの左・右辺式を末端まで調べて、OR/NOT が含まれていないかをチェックする
			bool isNeedJsonFilter = false;
			Stack<BinaryExpression> expressions = new Stack<BinaryExpression>();
			expressions.Push(operation);
			while (expressions.Count > 0)
			{
				var exp = expressions.Pop();
				if (exp.Left is BinaryExpression lbe)
				{
					if (lbe.NodeType == ExpressionType.OrElse || lbe.NodeType == ExpressionType.NotEqual)
					{
						isNeedJsonFilter = true;
						break;
					}

					expressions.Push(lbe);
				}
				if (exp.Right is BinaryExpression rbe)
				{
					if (rbe.NodeType == ExpressionType.OrElse || rbe.NodeType == ExpressionType.NotEqual)
					{
						isNeedJsonFilter = true;
						break;
					}

					expressions.Push(rbe);
				}
			}

			expressions.Clear();

			if (!isNeedJsonFilter)
			{
				// filtersで処理する、単一比較かAndで結ばれた複数の比較
				// && 以外の比較演算子の式をcompareExpListに対してフラット化
				var compareExpList = new List<BinaryExpression>();
				expressions.Push(operation);
				while (expressions.Count > 0)
				{
					var be = expressions.Pop();

					if (be.NodeType == ExpressionType.Equal
						|| be.NodeType == ExpressionType.GreaterThan
						|| be.NodeType == ExpressionType.GreaterThanOrEqual
						|| be.NodeType == ExpressionType.LessThan
						|| be.NodeType == ExpressionType.LessThanOrEqual
						)
					{
						compareExpList.Add(be);
					}
					else if (be.NodeType == ExpressionType.AndAlso)
					{
						expressions.Push((BinaryExpression)be.Left);
						expressions.Push((BinaryExpression)be.Right);
					}
					else
					{
						throw new ArgumentException("対応してない演算子が使われています : " + be.NodeType);
					}
				}

				List<KeyValuePair<string, string>> sb = new List<KeyValuePair<string, string>>();
				Dictionary<SearchFieldType, int> fieldTypeIndexMap = new Dictionary<SearchFieldType, int>();
				foreach (var be in compareExpList)
				{
					bool isNeedInverseCompereOperation = false;
					MemberExpression memberExpression = null;
					Expression valueExpression = null;
					if (be.Left is MemberExpression member1
						&& member1.Member.DeclaringType == typeof(SnapshotVideoItem)
						)
					{
						memberExpression = member1;
						valueExpression = be.Right;
					}

					if (be.Right is MemberExpression member2
						&& member2.Member.DeclaringType == typeof(SnapshotVideoItem)
						)
					{
						memberExpression = member2;
						valueExpression = be.Left;
						isNeedInverseCompereOperation = true;
					}

					if (memberExpression == null)
					{
						throw new ArgumentException();
					}

					var conditionType = be.NodeType switch
					{
						ExpressionType.Equal => SearchFilterCompareCondition.Equal,
						ExpressionType.GreaterThan => isNeedInverseCompereOperation ? SearchFilterCompareCondition.LessThan : SearchFilterCompareCondition.GreaterThan,
						ExpressionType.GreaterThanOrEqual => isNeedInverseCompereOperation ? SearchFilterCompareCondition.LessThenOrEqual : SearchFilterCompareCondition.GreaterThanOrEqual,
						ExpressionType.LessThan => isNeedInverseCompereOperation ? SearchFilterCompareCondition.GreaterThan : SearchFilterCompareCondition.LessThan,
						ExpressionType.LessThanOrEqual => isNeedInverseCompereOperation ? SearchFilterCompareCondition.GreaterThanOrEqual: SearchFilterCompareCondition.LessThenOrEqual,
						_ => throw new ArgumentException("対応してない演算子が使われています : " + be.NodeType),
					};

					var fieldType = (SearchFieldType)Enum.Parse(typeof(SearchFieldType), memberExpression.Member.Name);

					Guard.IsTrue(fieldType.IsFilterField(), "fieldType.IsFilterField()");

					object value = Expression.Lambda(valueExpression).Compile().DynamicInvoke();
					var valueText = value is DateTime time ? time.ToString("o") : value.ToString();
					if (conditionType == SearchFilterCompareCondition.Equal)
					{
						int count = fieldTypeIndexMap.TryGetValue(fieldType, out count) ? count : 0;
						sb.Add(new KeyValuePair<string, string>($"filters[{fieldType.GetDescription()}][{count}]", valueText));
						count += 1;
						fieldTypeIndexMap[fieldType] = count;
					}
					else
					{
						sb.Add(new KeyValuePair<string, string>($"filters[{fieldType.GetDescription()}][{conditionType.GetDescription()}]", valueText));
					}
				}

				return sb;
			}
			else
			{
				// jsonFilterで処理する
				// 論理演算の最適化は行わずExpressionTreeを愚直にJsonFilterのデータに変換する
				throw new NotImplementedException("OR/NOT演算は未実装です");
			}
		}

	}



}

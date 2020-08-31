using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace NiconicoLiveToolkit.Live.Search
{
    public sealed class SearchFilterField
    {
		public int CommunityId { get; set; }

		public DateTime OpenTime { get; set; }

		public DateTime StartTime { get; set; }

		public string Description { get; set; }

		public string Tags { get; set; }

		public DateTime LiveEndTime { get; set; }

		public bool TimeshiftEnabled { get; set; }

		public string CategoryTags { get; set; }

		public int ViewCounter { get; set; }

		public string ProviderType { get; set; }

		public int UserId { get; set; }

		public string Title { get; set; }

		public bool MemberOnly { get; set; }

		public int ScoreTimeshiftReserved { get; set; }

		public int CommentCounter { get; set; }

		public string CommunityText { get; set; }

		public int ChannelId { get; set; }

		public string LiveStatus { get; set; }


		public const string ProviderTypeOfficial = "official";
		public const string ProviderTypeCommunity = "community";
		public const string ProviderTypeChannel = "channel";


		public const string LiveStatusPast = "past";
		public const string LiveStatusOnAir = "onair";
		public const string LiveStatusReserved = "reserved";
	}




	public class ExpressionSearchFilter : ISearchFilter
	{
		private List<KeyValuePair<string, string>> _keyValues;
        private readonly Expression<Func<SearchFilterField, bool>> _compareOpExpression;

        public ExpressionSearchFilter(Expression<Func<SearchFilterField, bool>> compareOpExpression)
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
				Dictionary<LiveSearchFilterType, int> fieldTypeIndexMap = new Dictionary<LiveSearchFilterType, int>();
				foreach (var be in compareExpList)
				{
					bool isNeedInverseCompereOperation = false;
					MemberExpression memberExpression = null;
					Expression valueExpression = null;
					if (be.Left is MemberExpression member1
						&& member1.Member.DeclaringType == typeof(SearchFilterField)
						)
					{
						memberExpression = member1;
						valueExpression = be.Right;
					}

					if (be.Right is MemberExpression member2
						&& member2.Member.DeclaringType == typeof(SearchFilterField)
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
						ExpressionType.Equal => SearchFilterCompareCondition.EQ,
						ExpressionType.GreaterThan => isNeedInverseCompereOperation ? SearchFilterCompareCondition.LT : SearchFilterCompareCondition.GT,
						ExpressionType.GreaterThanOrEqual => isNeedInverseCompereOperation ? SearchFilterCompareCondition.LTE : SearchFilterCompareCondition.GTE,
						ExpressionType.LessThan => isNeedInverseCompereOperation ? SearchFilterCompareCondition.GT : SearchFilterCompareCondition.LT,
						ExpressionType.LessThanOrEqual => isNeedInverseCompereOperation ? SearchFilterCompareCondition.GTE : SearchFilterCompareCondition.LTE,
						_ => throw new ArgumentException("対応してない演算子が使われています : " + be.NodeType),
					};

					var fieldType = (LiveSearchFilterType)Enum.Parse(typeof(LiveSearchFilterType), memberExpression.Member.Name);
					object value = Expression.Lambda(valueExpression).Compile().DynamicInvoke();
					var valueText = value is DateTime time ? time.ToString("o") : value.ToString();
					if (conditionType == SearchFilterCompareCondition.EQ)
					{
						int count = fieldTypeIndexMap.TryGetValue(fieldType, out count) ? count : 0;
						sb.Add(new KeyValuePair<string, string>($"filters[{SearchHelpers.ToQueryString((LiveSearchFieldType)fieldType)}][{count}]", valueText));
						count += 1;
						fieldTypeIndexMap[fieldType] = count;
					}
					else
					{
						sb.Add(new KeyValuePair<string, string>($"filters[{SearchHelpers.ToQueryString((LiveSearchFieldType)fieldType)}][{SearchHelpers.ToQueryString(conditionType)}]", valueText));
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


	public class CompositionSearchFilter : ISearchFilter
	{
		private readonly IEnumerable<ISearchFilter> _filters;

		public CompositionSearchFilter(IEnumerable<ISearchFilter> filters)
		{
			_filters = filters;
		}

		public IEnumerable<KeyValuePair<string, string>> GetFilterKeyValues()
		{
			return _filters.SelectMany(x => x.GetFilterKeyValues());

		}
	}

	public class CompareSearchFilter<T> : ISearchFilter
	{
		private readonly SearchFieldType _filterType;
		private readonly T _value;
		private readonly SearchFilterCompareCondition _condition;

		public CompareSearchFilter(LiveSearchFilterType filterType, T value, SearchFilterCompareCondition condition)
		{
			_filterType = (SearchFieldType)filterType;
			_value = value;
			_condition = condition;
		}

		public IEnumerable<KeyValuePair<string, string>> GetFilterKeyValues()
		{
			return new[] { new KeyValuePair<string, string>($"filters[{SearchHelpers.GetParameterName(_filterType)}][{SearchHelpers.ToQueryString(_condition)}]", _value.ToString()) };
		}
	}



	public sealed class ValueContainsSearchFilter<T> : ISearchFilter
	{
		SearchFieldType _filterType;
		IEnumerable<T> _values;

		public ValueContainsSearchFilter(LiveSearchFilterType filterType, params T[] values)
		{
			_filterType = (SearchFieldType)filterType;
			_values = values;
		}

		public IEnumerable<KeyValuePair<string, string>> GetFilterKeyValues()
		{
			return _values.Select((x, i) => new KeyValuePair<string, string>($"filters[{SearchHelpers.GetParameterName(_filterType)}][{i}]", x.ToString()));
		}
	}



	public enum SearchFilterCompareCondition
	{
		EQ,
		GT,
		GTE,
		LT,
		LTE,
	}



}

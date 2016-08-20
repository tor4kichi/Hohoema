using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models
{
	public class VideoDownloadProgress
	{
		private static bool _ValueIsInsideRange(uint val, uint rangeStart, uint rangeEnd)
		{
			return rangeStart <= val && val <= rangeEnd;
		}

		private static bool _ValueIsInsideRange(uint val, ref KeyValuePair<uint, uint> range)
		{
			return _ValueIsInsideRange(val, range.Key, range.Value);
		}


		public VideoDownloadProgress(uint size)
		{
			Size = size;
			CachedRanges = new SortedDictionary<uint, uint>();
		}

		public VideoDownloadProgress Clone()
		{
			var prog = new VideoDownloadProgress(Size);

			foreach (var cached in CachedRanges)
			{
				prog.CachedRanges.Add(cached.Key, cached.Value);
			}

			return prog;
		}


		private void _InnerUpdate(uint position, uint length)
		{
			if (Size < position) { throw new ArgumentOutOfRangeException(); }
			if (length == 0) { return; }

			var posEnd = position + length;

			bool isMerged = false;
			bool isCollideCachedRange = false;

			for (int index = 0; index < CachedRanges.Count; ++index)
			{
				var pair = CachedRanges.ElementAt(index);

				if (pair.Key == position && pair.Value == posEnd)
				{
					isCollideCachedRange = true;
					continue;
				}

				// startとの
				var isStartInside = _ValueIsInsideRange(position, ref pair);
				var isEndInside = _ValueIsInsideRange(posEnd, ref pair);

				if (isStartInside && isEndInside)
				{
					// 範囲内だがすでにキャッシュ済みのため更新は不要
					isCollideCachedRange = true;
					break;
				}
				else if (isStartInside || isEndInside)
				{
					// どちらかの範囲内
					var minStart = Math.Min(position, pair.Key);
					var maxEnd = Math.Max(posEnd, pair.Value);

					CachedRanges.Remove(pair.Key);

					// 後続のアイテムがさらにマージ可能な場合をチェック
					if (CachedRanges.ContainsKey(maxEnd))
					{
						// 後続アイテムの終端位置
						var end = CachedRanges[maxEnd];

						// 後続アイテム削除
						CachedRanges.Remove(maxEnd);

						// 再登録する終端位置を修正
						maxEnd = end;
					}

					CachedRanges.Add(minStart, maxEnd);

					isCollideCachedRange = true;
					isMerged = true;
					break;
				}
			}

			// 登録済みキャッシュにマージされない場合は新規登録
			if (!isCollideCachedRange)
			{
				CachedRanges.Add(position, posEnd);
			}

			if (isMerged)
			{
				_InnerUpdate(position, length);
			}
		}

		public void Update(uint position, uint length)
		{
			_InnerUpdate(position, length);
		}


		public bool IsCachedRange(uint head, uint length)
		{
			var tail = head + length;
			if (tail > Size)
			{
				tail = Size;
			}

			foreach (var range in CachedRanges)
			{
				if (_ValueIsInsideRange(head, range.Key, range.Value) &&
					_ValueIsInsideRange(tail, range.Key, range.Value))
				{
					return true;
				}
			}

			return false;
		}



		public IEnumerable<KeyValuePair<uint, uint>> EnumerateIncompleteRanges()
		{
			uint nextIncompleteRangeStart = 0;

			if (CachedRanges.Count == 0)
			{
				yield return new KeyValuePair<uint, uint>(0, Size);
			}
			else
			{
				foreach (var range in CachedRanges)
				{
					if (nextIncompleteRangeStart < range.Key)
					{
						yield return new KeyValuePair<uint, uint>(nextIncompleteRangeStart, range.Key);
					}
					nextIncompleteRangeStart = range.Value;
				}

				if (nextIncompleteRangeStart < Size)
				{
					yield return new KeyValuePair<uint, uint>(nextIncompleteRangeStart, Size);
				}
			}

		}

		public uint BufferedSize()
		{
			uint bufferedSize = 0;
			foreach (var range in CachedRanges)
			{
				bufferedSize += range.Value - range.Key;
			}

			return bufferedSize;
		}

		public uint RemainSize()
		{
			uint remain = 0;
			foreach (var range in EnumerateIncompleteRanges())
			{
				remain += range.Value - range.Key;
			}

			return remain;
		}

		public bool CheckComplete()
		{
			if (CachedRanges.Count == 1)
			{
				// すべてのデータがダウンロードされた時
				// キャッシュ済み区間が0~Sizeに収束する
				var range = CachedRanges.ElementAt(0);
				return range.Key == 0 && range.Value == Size;
			}
			else
			{
				return false;
			}
		}

		public uint Size { get; set; }


		/// <summary>
		/// key = start, value = end
		/// valueには長さではなく終端の絶対位置を入れる
		/// </summary>
		public IDictionary<uint, uint> CachedRanges { get; private set; }
	}
}

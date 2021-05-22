using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NiconicoToolkit.Live.WatchSession
{
    public sealed class LiveChatData
    {
        public string Thread { get; set; }
        public int CommentId { get; set; }
        public TimeSpan VideoPosition { get; set; }
        public long Date { get; set; }
        public long DateUsec { get; set; }
        public int? __Premium { get; set; }
        public bool IsPremium => __Premium == 1;
        public bool IsOperater => __Premium >= 2;
        public bool IsUserComment => (__Premium ?? 0) <= 1;
        public bool IsAnonymity { get; set; }
        public string UserId { get; set; }
        public string Mail { get; set; }
        public string Content { get; set; }
        public int? Score { get; set; }
        public bool IsYourPost { get; set; }


        public bool HasOperatorCommand => Content?.StartsWith("/") ?? false;

        private string _OperatorCommandType;
        public string OperatorCommandType
        {
            get
            {
                if (!IsOperater) { throw new NotSupportedException(); }

                if (_OperatorCommandType != null) { return _OperatorCommandType; }

                ResetOperatorCommand();

                return _OperatorCommandType;
            }
        }

        private string[] _OperatorCommandParameters;
        public string[] OperatorCommandParameters
        {
            get
            {
                if (!IsOperater) { throw new NotSupportedException(); }

                if (_OperatorCommandType != null) { return _OperatorCommandParameters; }

                ResetOperatorCommand();

                return _OperatorCommandParameters;
            }
        }



        private void ResetOperatorCommand()
        {
            if (IsOperater)
            {
                if (Content?.StartsWith("/") ?? false)
                {
                    // 半角スペースで分割する
                    // 内部で ダブルクオートに囲まれている場合は囲まれている範囲の文字列同士を結合する

                    var splitedOperatorCommand = Content.Split(' ').ToList();
                    List<string> spaceConcatOperatorCommand = splitedOperatorCommand.Aggregate<string, List<string>>(new List<string>(),
                        (list, c) =>
                        {
                            // 前に追加した文字列がダブルクオートに囲まれた範囲の場合は空白で結合して
                            // 前に追加した文字列に上書きする
                            var last = list.LastOrDefault();
                            if (last != null && last.StartsWith("\"") && !last.EndsWith("\""))
                            {
                                list[list.Count - 1] = $"{last} {c}";
                            }
                            else
                            {
                                list.Add(c);
                            }

                            return list;
                        });

                    _OperatorCommandType = spaceConcatOperatorCommand.First().Remove(0, 1);

                    if (_OperatorCommandType == "perm")
                    {
                        _OperatorCommandParameters = new[] { string.Join(" ", spaceConcatOperatorCommand.Skip(1).ToArray()) };
                    }
                    else
                    {
                        _OperatorCommandParameters = spaceConcatOperatorCommand.Skip(1).ToArray();
                    }
                }
            }
        }

    }
}

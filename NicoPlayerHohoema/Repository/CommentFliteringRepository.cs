using LiteDB;
using NicoPlayerHohoema.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Repository
{
    public sealed class CommentFliteringRepository 
    {
        sealed class CommentFilteringSettings : FlagsRepositoryBase
        {
            public CommentFilteringSettings()
            {
                _IsFilteringCommentOwnerIdEnabled = Read<bool>(@default: true, nameof(IsFilteringCommentOwnerIdEnabled));
                _IsFilteringCommentTextEnabled = Read<bool>(@default: true, nameof(IsFilteringCommentTextEnabled));
                _IsFilteringCommandEnabled = Read<bool>(@default: true, nameof(IsFilteringCommandEnabled));
                _IgnoreCommands = Read<List<string>>(propertyName: nameof(IgnoreCommands));
                _NGShareScore = Read<int>(propertyName: nameof(NGShareScore));
            }

            bool _IsFilteringCommentOwnerIdEnabled;
            public bool IsFilteringCommentOwnerIdEnabled
            {
                get => _IsFilteringCommentOwnerIdEnabled;
                set => SetProperty(ref _IsFilteringCommentOwnerIdEnabled, value);
            }


            bool _IsFilteringCommentTextEnabled;
            public bool IsFilteringCommentTextEnabled
            {
                get => _IsFilteringCommentTextEnabled;
                set => SetProperty(ref _IsFilteringCommentTextEnabled, value);
            }


            bool _IsFilteringCommandEnabled;
            public bool IsFilteringCommandEnabled
            {
                get => _IsFilteringCommandEnabled;
                set => SetProperty(ref _IsFilteringCommandEnabled, value);
            }

            List<string> _IgnoreCommands;
            public List<string> IgnoreCommands
            {
                get => _IgnoreCommands;
                set => SetProperty(ref _IgnoreCommands, value);
            }

            int _NGShareScore;
            public int NGShareScore
            {
                get => _NGShareScore;
                set => SetProperty(ref _NGShareScore, value);
            }

        }

        public CommentFliteringRepository(
            FilteringCommentOwnerIdDBService filteringCommentOwnerIdDBService,
            FilteringCommentTextDBService filteringCommentTextDBService,
            CommentTextTransformConditionDBService commentTextTransformConditionDBService
            )
        {
            _filteringCommentOwnerIdDBService = filteringCommentOwnerIdDBService;
            _filteringCommentTextDBService = filteringCommentTextDBService;
            _commentTextTransformConditionDBService = commentTextTransformConditionDBService;
            
            _commentFilteringSettings = new CommentFilteringSettings();
            _filteredCommands = _commentFilteringSettings.IgnoreCommands?.ToHashSet() ?? new HashSet<string>();
            _shareNGScore = _commentFilteringSettings.NGShareScore;
        }

        private readonly FilteringCommentOwnerIdDBService _filteringCommentOwnerIdDBService;
        private readonly FilteringCommentTextDBService _filteringCommentTextDBService;
        private readonly CommentTextTransformConditionDBService _commentTextTransformConditionDBService;
        private readonly CommentFilteringSettings _commentFilteringSettings;




        int _shareNGScore;
        public int ShareNGScore
        {
            get => _shareNGScore;
            set => _commentFilteringSettings.NGShareScore = _shareNGScore = value;
        }


        
        #region Comment Text Transform

        public sealed class CommentTextTransformCondition
        {
            [BsonId(autoId:true)]
            public int _id { get; set; }

            [BsonField]
            public bool IsEnabled { get; set; } = true;


            string _regexPattern;
            [BsonField]
            public string RegexPattern 
            {
                get => _regexPattern;
                set
                {
                    _regexPattern = value;
                    _regex = null;
                }
            }

            [BsonField]
            public string ReplaceText { get; set; } = string.Empty;

            [BsonField]
            public string Description { get; set; }

            [BsonIgnore]
            Regex _regex;

            public string TextTransform(string commentText)
            {
                try
                {
                    _regex ??= (!string.IsNullOrWhiteSpace(RegexPattern) ? new Regex(RegexPattern) : null);
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e.ToString());
                }

                if (_regex?.IsMatch(commentText) ?? false)
                {
                    var replaced = _regex.Replace(commentText, ReplaceText ?? string.Empty);
                    System.Diagnostics.Debug.WriteLine($"[CommentTextTransform] {commentText} -> {replaced}");
                    return replaced;
                }
                else
                {
                    return commentText;
                }
            }
        }

        public sealed class CommentTextTransformConditionDBService : LocalLiteDBService<CommentTextTransformCondition>
        {
        }

        public List<CommentTextTransformCondition> GetAllCommentTextTransformCondition()
        {
            return _commentTextTransformConditionDBService.ReadAllItems();
        }

        public CommentTextTransformCondition AddCommentTextTransformCondition(string regexPattern, string replaceText, string description)
        {
            var condition = new CommentTextTransformCondition()
            {
                RegexPattern = regexPattern,
                ReplaceText = replaceText,
                Description = description
            };

            return _commentTextTransformConditionDBService.CreateItem(condition);
        }

        public void UpdateCommentTextTransformCondition(CommentTextTransformCondition commentTextTransformCondition)
        {
            _commentTextTransformConditionDBService.UpdateItem(commentTextTransformCondition);
        }

        public bool RemoveCommentTextTransformCondition(CommentTextTransformCondition commentTextTransformCondition)
        {
            return _commentTextTransformConditionDBService.DeleteItem(commentTextTransformCondition);
        }


        #endregion


        #region Filtered Command


        #endregion

        public sealed class FilteringCommentOwnerId
        {
            [BsonId]
            public string UserId { get; set; }

            [BsonField]
            public string SoureComment { get; set; } = string.Empty;
        }

        public sealed class FilteringCommentOwnerIdDBService : LocalLiteDBService<FilteringCommentOwnerId>
        {

        }


        #region Filtering Owner Id


        public bool IsFilteringCommentOwnerIdEnabled
        {
            get => _commentFilteringSettings.IsFilteringCommentOwnerIdEnabled;
            set => _commentFilteringSettings.IsFilteringCommentOwnerIdEnabled = value;
        }



        public List<FilteringCommentOwnerId> GetAllFilteringCommenOwnerId()
        {
            return _filteringCommentOwnerIdDBService.ReadAllItems();
        }

        public void AddFilteringCommenOwnerId(string userId, string comment)
        {
            RemoveFilteringCommenOwnerId(userId);

            FilteringCommentOwnerId filteringCommentOwnerId = new FilteringCommentOwnerId() 
            {
                UserId = userId,
                SoureComment = comment
            };

            _filteringCommentOwnerIdDBService.CreateItem(filteringCommentOwnerId);
        }

        public bool RemoveFilteringCommenOwnerId(string userId)
        {
            return _filteringCommentOwnerIdDBService.DeleteItem(userId);
        }

        public void ClearFilteringCommenOwnerId()
        {
            var items = GetAllFilteringCommenOwnerId();
            foreach (var item in items)
            {
                _filteringCommentOwnerIdDBService.DeleteItem(item);
            }
        }


        #endregion

        #region Filtering Comment Text

        public sealed class FilteringCommentTextKeyword
        {
            [BsonId(autoId: true)]
            public int _id { get; set; }


            private string _condition;
            [BsonField]
            public string Condition
            {
                get => _condition;
                set
                {
                    _condition = value;
                    _regex = null;
                }
            }

            [BsonIgnore]
            public Regex _regex;

            public bool IsMatch(string commentText)
            {
                if (_regex == null && !string.IsNullOrWhiteSpace(Condition))
                {
                    try
                    {
                        _regex = new Regex(Condition);
                    }
                    catch { }
                }

                if (_regex == null) { return false; }

                return _regex.IsMatch(commentText);
            }
        }

        public sealed class FilteringCommentTextDBService : LocalLiteDBService<FilteringCommentTextKeyword>
        {

        }


        public bool IsFilteringCommentTextEnabled
        {
            get => _commentFilteringSettings.IsFilteringCommentTextEnabled;
            set => _commentFilteringSettings.IsFilteringCommentTextEnabled = value;
        }

        public List<FilteringCommentTextKeyword> GetAllFilteringCommentTextConditions()
        {
            return _filteringCommentTextDBService.ReadAllItems();
        }


        public FilteringCommentTextKeyword AddFilteringCommentText(string condition)
        {
            FilteringCommentTextKeyword filteringCommentText = new FilteringCommentTextKeyword() 
            {
                Condition = condition,
            };

            return _filteringCommentTextDBService.CreateItem(filteringCommentText);
        }

        public void UpdateFilteringCommentText(FilteringCommentTextKeyword filteringCommentText)
        {
            _filteringCommentTextDBService.UpdateItem(filteringCommentText);
        }

        public bool RemoveFilteringCommentText(FilteringCommentTextKeyword filteringCommentText)
        {
            return _filteringCommentTextDBService.DeleteItem(filteringCommentText._id);
        }


        #endregion



        #region Filtered Command

        HashSet<string> _filteredCommands;

        public bool IsIgnoreCommand(string command)
        {
            return _filteredCommands.Contains(command);
        }

        public List<string> GetFilteredCommands()
        {
            return _filteredCommands?.ToList() ?? new List<string>();
        }

        public void AddFilteredCommand(string command)
        {
            _filteredCommands.Add(command);
            _commentFilteringSettings.IgnoreCommands = _filteredCommands.ToList();
        }

        public void RemoveFilteredCommand(string command)
        {
            _filteredCommands.Remove(command);
            _commentFilteringSettings.IgnoreCommands = _filteredCommands.ToList();
        }

        public void ClearFilteredCommand()
        {
            _filteredCommands.Clear();
            _commentFilteringSettings.IgnoreCommands = _filteredCommands.ToList();
        }

        #endregion


    }

}

using I18NPortable;
using NicoPlayerHohoema.Interfaces;
using NicoPlayerHohoema.Models;
using NicoPlayerHohoema.Models.Niconico;
using NicoPlayerHohoema.Repository;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.UseCase.NicoVideoPlayer
{

    

    public sealed class CommentFiltering : FixPrism.BindableBase, ICommentFilter
    {
        public class CommentOwnerIdFilteredEventArgs
        {
            public string UserId { get; set; }
        }


        public event EventHandler<CommentOwnerIdFilteredEventArgs> FilteringCommentOwnerIdAdded;
        public event EventHandler<CommentOwnerIdFilteredEventArgs> FilteringCommentOwnerIdRemoved;


        private readonly CommentFliteringRepository _commentFliteringRepository;
        private readonly AppFlagsRepository _appFlagsRepository;

        public CommentFiltering(
            Repository.CommentFliteringRepository commentFliteringRepository,
            AppFlagsRepository appFlagsRepository
            )
        {
            _commentFliteringRepository = commentFliteringRepository;
            _appFlagsRepository = appFlagsRepository;
            _commentTextTransformConditions = _commentFliteringRepository.GetAllCommentTextTransformCondition();
            _filteredCommentOwnerIds = _commentFliteringRepository.GetAllFilteringCommenOwnerId().Select(x => x.UserId).ToHashSet();
            _filteringCommentTextKeywords = new ObservableCollection<CommentFliteringRepository.FilteringCommentTextKeyword>(
                _commentFliteringRepository.GetAllFilteringCommentTextConditions()
                );
            _ignoreCommands = _commentFliteringRepository.GetFilteredCommands().ToHashSet();

            Initialize();
        }

        void Initialize()
        {
            if (!_appFlagsRepository.IsCreatedGlassMowerTextTransformCondition_V_0_21_5)
            {
                AddGlassMowerCommentTextTransformCondition();
                _appFlagsRepository.IsCreatedGlassMowerTextTransformCondition_V_0_21_5 = true;
            }
        }

        public void AddGlassMowerCommentTextTransformCondition()
        {
            _commentTextTransformConditions.Add(new CommentFliteringRepository.CommentTextTransformCondition()
            {
                RegexPattern = "([wWｗＷ]){2,}",
                ReplaceText = "ｗ",
                Description = "AutoShortingKUSAWords".Translate(),
            });
        }

        public void AddCenterCommandFiltering()
        {
            
        }



        #region ICommentFilter

        public bool IsFilterdComment(Comment comment)
        {
            return IsShareNGScoreFilterd(comment.NGScore)
                || IsCommentOwnerUserIdFiltered(comment.UserId)
                || _filteringCommentTextKeywords.IsMatchAny(comment.CommentText)
                ;
        }


        public string TransformCommentText(string commentText)
        {
            return TranformCommentText(commentText);
        }

        HashSet<string> _ignoreCommands;

        public bool IsIgnoreCommand(string command)
        {
            return _ignoreCommands.Contains(command);
        }

        #endregion



        DelegateCommand<string> _AddFilteredCommentCommandCommand;
        public DelegateCommand<string> AddFilteredCommentCommandCommand => _AddFilteredCommentCommandCommand
            ?? (_AddFilteredCommentCommandCommand = new DelegateCommand<string>((commandText) =>
            {
                AddFilteredCommentCommand(commandText);
            }));

        public void AddFilteredCommentCommand(string commandText)
        {
            if (_ignoreCommands.Contains(commandText))
            {
                return;
            }

            _ignoreCommands.Add(commandText);
            _commentFliteringRepository.AddFilteredCommand(commandText);
        }


        #region Comment Text Transform

        List<CommentFliteringRepository.CommentTextTransformCondition> _commentTextTransformConditions;

        public string TranformCommentText(string commentText)
        {
            return _commentTextTransformConditions.TransformCommentText(commentText);
        }

        DelegateCommand<CommentFliteringRepository.CommentTextTransformCondition> _AddTextTransformConditionsCommand;
        public DelegateCommand<CommentFliteringRepository.CommentTextTransformCondition> AddTextTransformConditionsCommand => _AddTextTransformConditionsCommand
            ?? (_AddTextTransformConditionsCommand = new DelegateCommand<CommentFliteringRepository.CommentTextTransformCondition>((condition) =>
            {
                AddTextTransformConditions(condition);
            }));

        public void AddTextTransformConditions(CommentFliteringRepository.CommentTextTransformCondition condition)
        {
            _commentFliteringRepository.AddCommentTextTransformCondition(condition.RegexPattern, condition.ReplaceText, condition.Description);
        }

        DelegateCommand<CommentFliteringRepository.CommentTextTransformCondition> _UpdateTextTransformConditionsCommand;
        public DelegateCommand<CommentFliteringRepository.CommentTextTransformCondition> UpdateTextTransformConditionsCommand => _UpdateTextTransformConditionsCommand
            ?? (_UpdateTextTransformConditionsCommand = new DelegateCommand<CommentFliteringRepository.CommentTextTransformCondition>((condition) =>
            {
                UpdateTextTransformConditions(condition);
            }));

        public void UpdateTextTransformConditions(CommentFliteringRepository.CommentTextTransformCondition condition)
        {
            _commentFliteringRepository.UpdateCommentTextTransformCondition(condition);
        }


        DelegateCommand<CommentFliteringRepository.CommentTextTransformCondition> _RemoveTextTransformConditionsCommand;
        public DelegateCommand<CommentFliteringRepository.CommentTextTransformCondition> RemoveTextTransformConditionsCommand => _RemoveTextTransformConditionsCommand
            ?? (_RemoveTextTransformConditionsCommand = new DelegateCommand<CommentFliteringRepository.CommentTextTransformCondition>((condition) =>
            {
                RemoveTextTransformConditions(condition);
            }));

        public void RemoveTextTransformConditions(CommentFliteringRepository.CommentTextTransformCondition condition)
        {
            _commentFliteringRepository.RemoveCommentTextTransformCondition(condition);
        }

        #endregion


        #region Share NG Score 

        public bool IsShareNGScoreFilterd(int score)
        {
            return _commentFliteringRepository.ShareNGScore < score;
        }

        #endregion


        #region Filtered Comment Owner Id

        HashSet<string> _filteredCommentOwnerIds = new HashSet<string>();

        public bool IsCommentOwnerUserIdFiltered(string userId)
        {
            if (!_commentFliteringRepository.IsFilteringCommentOwnerIdEnabled) { return false; }

            return _filteredCommentOwnerIds.Contains(userId);
        }

        private bool _IsEnableFilteringCommentOwnerId;
        public bool IsEnableFilteringCommentOwnerId
        {
            get => _commentFliteringRepository.IsFilteringCommentOwnerIdEnabled;
            set
            {
                if (SetProperty(ref _IsEnableFilteringCommentOwnerId, value))
                {
                    _commentFliteringRepository.IsFilteringCommentOwnerIdEnabled = value;
                }
            }
        }


       

        DelegateCommand<Comment> _AddFilteringCommentOwnerIdCommand;
        public DelegateCommand<Comment> AddFilteringCommentOwnerIdCommand => _AddFilteringCommentOwnerIdCommand
            ?? (_AddFilteringCommentOwnerIdCommand = new DelegateCommand<Comment>((comment) => 
            {
                AddFilteringCommentOwnerId(comment.UserId, comment.CommentText);
            }));

        DelegateCommand<Comment> _RemoveFilteringCommentOwnerIdCommand;
        public DelegateCommand<Comment> RemoveFilteringCommentOwnerIdCommand => _RemoveFilteringCommentOwnerIdCommand
            ?? (_RemoveFilteringCommentOwnerIdCommand = new DelegateCommand<Comment>((comment) =>
            {
                RemoveFilteringCommentOwnerId(comment.UserId);
            }));

        public bool AddFilteringCommentOwnerId(string userId, string commentText)
        {
            if (string.IsNullOrEmpty(userId)) { return false; }

            if (_filteredCommentOwnerIds.Add(userId))
            {
                _commentFliteringRepository.AddFilteringCommenOwnerId(userId, commentText);

                FilteringCommentOwnerIdAdded?.Invoke(this, new CommentOwnerIdFilteredEventArgs() { UserId = userId });

                return true;
            }
            else { return false; }
        }

        public bool RemoveFilteringCommentOwnerId(string userId)
        {
            if (string.IsNullOrEmpty(userId)) { return false; }

            if (_filteredCommentOwnerIds.Remove(userId))
            {
                var result = _commentFliteringRepository.RemoveFilteringCommenOwnerId(userId);

                FilteringCommentOwnerIdRemoved?.Invoke(this, new CommentOwnerIdFilteredEventArgs() { UserId = userId });

                return true;
            }
            else { return false; }
        }

        #endregion


        #region Filtering Comment Text

        public sealed class FilteringCommentTextKeywordEventArgs
        {
            public CommentFliteringRepository.FilteringCommentTextKeyword FilterKeyword { get; set; }
        }


        public event EventHandler<FilteringCommentTextKeywordEventArgs> FilterKeywordAdded;
        public event EventHandler<FilteringCommentTextKeywordEventArgs> FilterKeywordUpdated;
        public event EventHandler<FilteringCommentTextKeywordEventArgs> FilterKeywordRemoved;



        private bool _IsEnableFilteringCommentText;
        public bool IsEnableFilteringCommentText
        {
            get => _commentFliteringRepository.IsFilteringCommentTextEnabled;
            set
            {
                if (SetProperty(ref _IsEnableFilteringCommentText, value))
                {
                    _commentFliteringRepository.IsFilteringCommentTextEnabled = value;
                }
            }
        }


        ObservableCollection<CommentFliteringRepository.FilteringCommentTextKeyword> _filteringCommentTextKeywords;
        public IEnumerable<CommentFliteringRepository.FilteringCommentTextKeyword> GetAllFilteringCommentTextCondition()
        {
            return _filteringCommentTextKeywords;
        }



        DelegateCommand<string> _AddFilteringCommentTextConditionCommand;
        public DelegateCommand<string> AddFilteringCommentTextConditionCommand => _AddFilteringCommentTextConditionCommand
            ?? (_AddFilteringCommentTextConditionCommand = new DelegateCommand<string>((keyword) =>
            {
                AddFilteringCommentTextKeyword(keyword);
            }));

        public void AddFilteringCommentTextKeyword(string keyword)
        {
            var added =_commentFliteringRepository.AddFilteringCommentText(keyword);
            _filteringCommentTextKeywords.Insert(0, added);
            FilterKeywordAdded?.Invoke(this, new FilteringCommentTextKeywordEventArgs() { FilterKeyword = added });
        }

        DelegateCommand<CommentFliteringRepository.FilteringCommentTextKeyword> _UpdateFilteringCommentTextConditionCommand;
        public DelegateCommand<CommentFliteringRepository.FilteringCommentTextKeyword> UpdateFilteringCommentTextConditionCommand => _UpdateFilteringCommentTextConditionCommand
            ?? (_UpdateFilteringCommentTextConditionCommand = new DelegateCommand<CommentFliteringRepository.FilteringCommentTextKeyword>((keyword) =>
            {
                UpdateFilteringCommentTextKeyword(keyword);
            }));

        public void UpdateFilteringCommentTextKeyword(CommentFliteringRepository.FilteringCommentTextKeyword keyword)
        {
            _commentFliteringRepository.UpdateFilteringCommentText(keyword);
            
            FilterKeywordUpdated?.Invoke(this, new FilteringCommentTextKeywordEventArgs() { FilterKeyword = keyword });
        }


        DelegateCommand<CommentFliteringRepository.FilteringCommentTextKeyword> _RemoveFilteringCommentTextConditionCommand;
        public DelegateCommand<CommentFliteringRepository.FilteringCommentTextKeyword> RemoveFilteringCommentTextConditionCommand => _RemoveFilteringCommentTextConditionCommand
            ?? (_RemoveFilteringCommentTextConditionCommand = new DelegateCommand<CommentFliteringRepository.FilteringCommentTextKeyword>((keyword) =>
            {
                RemoveFilteringCommentTextCondition(keyword);
            }));

        public void RemoveFilteringCommentTextCondition(CommentFliteringRepository.FilteringCommentTextKeyword keyword)
        {
            if (_commentFliteringRepository.RemoveFilteringCommentText(keyword))
            {
                _filteringCommentTextKeywords.Remove(keyword);
                FilterKeywordRemoved?.Invoke(this, new FilteringCommentTextKeywordEventArgs() { FilterKeyword = keyword });
            }
        }

        #endregion
    }


    public static class CommentTextTransformConditionExtensions
    {
        public static string TransformCommentText(this IEnumerable<CommentFliteringRepository.CommentTextTransformCondition> conditions, string commentText)
        {
            return conditions.Aggregate(commentText, (text, condition) => condition.IsEnabled ? condition.TextTransform(text) : text);
        }
    }

    public static class FilteringCommentTextKeywordExtensions
    {
        public static bool IsMatchAny(this IEnumerable<CommentFliteringRepository.FilteringCommentTextKeyword> keywords, string commentText)
        {
            return keywords.Any(x => x.IsMatch(commentText));
        }
    }
}
